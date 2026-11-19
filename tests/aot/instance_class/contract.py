"""重建草稿：从本轮对话可见代码恢复意图，不是丢失文件或旧通过原流的逐字恢复。"""
import hashlib
import json
from pathlib import Path, PurePosixPath


def require(ok, message):
    if not ok:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def parse(raw):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            require(key not in result, '重复JSON键: ' + key)
            result[key] = value
        return result
    return json.loads(raw, object_pairs_hook=unique)


def anchors(sources):
    require(bool(sources) and '__main__' not in sources, '缺少非入口源码闭包')
    for name, source in sources.items():
        require(name and all(x.isidentifier() for x in name.split('.')), '无效模块名')
        path = PurePosixPath(source['path'])
        require(not path.is_absolute() and '..' not in path.parts, '源码路径越界')
        require(len(source['sha256']) == 64 and all(c in '0123456789abcdef' for c in source['sha256']), '源码SHA无效')
        parts = name.split('.')
        for end in range(1, len(parts)):
            parent = '.'.join(parts[:end])
            require(parent in sources and sources[parent]['path'].endswith('/__init__.py'), '缺少真实Package祖先: ' + parent)
    return {'schema_version': 1, 'modules': [{'module': name, 'files': []} for name in sorted(sources)]}


def verify_sources(root, sources):
    root = Path(root).resolve(strict=True)
    for row in sources.values():
        path = root / row['path']
        require(path.is_file() and not path.is_symlink() and path.resolve().is_relative_to(root), '源码缺失/链接越界')
        require(sha(path) == row['sha256'], '源码身份变化: ' + row['path'])


def verify_plan(plan, sources, selected):
    rows = plan['modules']
    names = [r['module'] for r in rows]
    require(len(names) == len(set(names)) and set(names) == set(sources) | {'__main__'}, '实际图与冻结源码不等')
    require(set(selected) <= set(sources) and bool(selected), '空/未知显式集合')
    for row in rows:
        require(row['compiled'] is True, '实际图含未编译候选')
        explicit = row['module'] in selected
        require((row['reason'] == 'explicit-instance') == explicit, '显式选择改变')
        require((row['execution'] == 'InstanceGlobals') == explicit, 'AOT图静默回退或自动扩域')


def verify_manifest(manifest, sources, selected, dist):
    rows = manifest['modules']
    names = [r['name'] for r in rows]
    require(len(names) == len(set(names)) and set(names) == set(sources) | {'__main__'}, 'Native模块全集变化')
    for row in rows:
        explicit = row['name'] in selected
        require((row['environment_reason'] == 'explicit-instance') == explicit, 'Native显式选择变化')
        if explicit:
            require(row['getter'] is None and row['setter'] is None and not row['exports'], 'Instance变Legacy')
    names = [r['module_name'] for r in manifest['module_locations'] if r['kind'] == 'executable_relative']
    require(len(names) == len(set(names)) and set(names) == set(sources), '实际来源不是完整ExecutableRelative闭包')
    require(not manifest['scope']['resources_included'] and not manifest['resources'], '虚拟锚意外部署文件')
    require(not any(p.suffix.lower() in {'.py', '.pyc', '.pyo'} for p in Path(dist).rglob('*')), '意外部署Python源/字节码')


def positive(record, stdout, stderr, golden):
    require(record.get('exit') == 0 and not record.get('failure') and record.get('raw_streams_complete') is True, '进程未正常完成/原流不全')
    require(stdout == golden and stderr == b'', '完整双流不等于冻结CP')


def no_resources(record, stdout, stderr, output):
    require(record.get('exit') == 1 and not record.get('failure') and record.get('raw_streams_complete') is True, '不是正常编译拒绝')
    text = stderr.decode('utf-8', errors='strict')
    require(not stdout and '[E0100]' in text and '显式实例 AOT 需要资源清单且不支持原生绑定' in text, '不是原资源缺失拒绝')
    require(not any(Path(output).rglob('*.dll')) and not any(Path(output).rglob('ryupy-artifact.json')), '前置拒绝却发射产品')


def kind2_rejection(record, stdout, stderr):
    """保留不合法managed宿主的真实负界，不把它当合法Native的失败。"""
    require(record.get('exit') == 1 and not record.get('failure') and record.get('raw_streams_complete') is True,
            'kind2不是正常拒绝')
    lines = stderr.decode('utf-8', errors='strict').splitlines()
    require(not stdout and lines and lines[-1] ==
            'NotImplementedError: executable-relative module locations require NativeAOT', 'kind2拒绝原因不符')


def pe_timestamp_equivalence(left, right):
    """唯一容许差异是合法PE COFF的4字节TimeDateStamp；不清MVID/PDB或其它元数据。"""
    require(len(left) == len(right) and len(left) >= 64, 'PE长度不同/截短')
    require(left[:2] == right[:2] == b'MZ', 'DOS标头无效')
    lpos, rpos = (int.from_bytes(x[60:64], 'little') for x in (left, right))
    require(lpos == rpos and 64 <= lpos <= len(left)-24, 'PE偏移无效/变化')
    require(left[lpos:lpos+4] == right[rpos:rpos+4] == b'PE\0\0', 'PE签名无效')
    start = lpos+8
    require(left[:start] == right[:start] and left[start+4:] == right[start+4:],
            '不止COFF timestamp四字节不同')
    return {'original_sha256': hashlib.sha256(left).hexdigest(),
            'actual_sha256': hashlib.sha256(right).hexdigest(), 'bytes': len(left),
            'allowed_offset': start, 'allowed_bytes': 4,
            'original_timestamp_hex': left[start:start+4].hex(),
            'actual_timestamp_hex': right[start:start+4].hex(),
            'byte_identical': left == right, 'scope': '仅COFF时间戳；不能称两次发射同字节'}
