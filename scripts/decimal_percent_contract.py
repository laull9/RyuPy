"""十进制百分号的薄合同：复用既有原流/恢复校验，只增加本切清单和完整用户栈谓词。"""
import collections
import hashlib
from pathlib import Path
import re
import re_scanner_contract as shared

ROOT, require, sha, parse = shared.ROOT, shared.require, shared.sha, shared.parse
relative, safe_file = shared.relative, shared.safe_file
verify_process, verify_command = shared.verify_process, shared.verify_command
verify_checkpoint, verify_trx = shared.verify_checkpoint, shared.verify_trx
TITLE = 'validation: decimal-percent-001'
MANIFEST = ROOT / 'tests/validation/decimal_percent.json'


def check_context(env, event):
    require(event.get('head_commit', {}).get('message') == TITLE, '百分号精确标题不符')
    shared.check_context(env, event)


def load_manifest(root=ROOT, path=MANIFEST):
    data = parse(path.read_bytes())
    require(data['schema_version'] == 1 and data['title'] == TITLE, '清单标题/版本不符')
    require(data['cp_sha256'] == shared.shared.old.shared.PYTHON_SHA
            and data['cp_version'] == '3.12.13' and data['sdk_version'] == '10.0.403', '固定工具不符')
    require(data['default_project_delta'] == 0 and len(data['graphs']) == 2, '仅两图，不晋升项目')
    require({g['id'] for g in data['graphs']} == {'supported', 'exceptions'}, '固定图ID不符')
    sources = {g['source'] for g in data['graphs']}
    require(sources == set(data['sources']), '两图源集合不符')
    for name, digest in {**data['sources'], **data['protected']}.items():
        require(sha(safe_file(root, name)) == digest, '冻结源改变：' + name)
    require(data['graphs'][0]['id'] == 'supported', '支持图须固定第一项')
    ids = []
    for graph in data['graphs']:
        require(graph['include'] == [] and graph['modules'] == {'__main__': graph['source']}, '仅内建单入口图')
        for row in graph['cases']:
            ids.append(row['id'])
            require(row['kind'] in ('cp', 'ni', 'capacity', 'trace') and type(row['stdout']) is str, '场景分类/完整stdout错误')
            require(type(row['argv']) is list and all(type(x) is str for x in row['argv']), '场景argv非法')
            require(row['cp'] == (row['kind'] in ('cp', 'trace')), 'NI/容量禁止送CP')
            if row['cp']:
                oracle = row['oracle']
                require(oracle['source_sha256'] == data['sources'][graph['source']]
                        and oracle['python_sha256'] == data['cp_sha256']
                        and oracle['role'] == 'local-golden-once'
                        and oracle['streams']['stdout'] == dict(bytes=len(row['stdout'].encode()), sha256=hashlib.sha256(row['stdout'].encode()).hexdigest())
                        and oracle['exit'] == row['exit'], '本地CP原件来源不符')
            if row['kind'] == 'trace':
                require(row['exit'] == 1 and row['frames'] and row['message'], '未捕获异常合同缺失')
    require(len(ids) == len(set(ids)) and set(ids) == {'supported-all', 'exception-ni', 'exception-capacity', 'exception-type', 'exception-value'}, '固定五场景漏项/重复')
    require(b'# ryupy: pending' in (root / data['graphs'][0]['source']).read_bytes(), '尚未验收源丢pending')
    actual = []
    for cls, expected in data['csharp']['classes'].items():
        text = (root / ('runtime/RyuPy.Runtime.Tests/' + cls + '.cs')).read_text()
        methods = re.findall(r'\[Fact\]\s+public void (\w+)\(', text)
        require(collections.Counter(methods) == collections.Counter(expected), '实际Fact改变：' + cls)
        actual += [cls + '.' + method for method in methods]
    require(collections.Counter(actual) == collections.Counter(data['csharp']['facts']), '精确Fact名单不符')
    require(len(data['csharp']['classes']['PyPercentDecimalWidthTests']) == 7
            and len(data['csharp']['classes']['PyPercentCapacityTests']) == 3, '十个新增Fact变更')
    return data


def cases(graph):
    """只适配现Native helper的已有case形状；真假CP等价最终逐row报告。"""
    return [dict(row, source=graph['source'], include=graph['include'], modules=graph['modules'],
                 mode='cp' if row['kind'] == 'cp' else 'ni') for row in graph['cases']]


def trace_result(record, streams, row, source):
    """仅去除真实帧后的精确源码展示/定位箭头；完整帧序列和剩余文本必须逐项相等。"""
    require(record['exit'] == 1 and record['failure'] is None and record['raw_streams_complete'] is True,
            '异常不是自然exit1完整原流')
    require(streams['stdout'] == row['stdout'].encode(), '异常stdout不符')
    lines = streams['stderr'].decode('utf-8').splitlines()
    require(lines and lines[0] == 'Traceback (most recent call last):', '异常缺唯一Traceback头')
    source_lines = source.read_text().splitlines()
    frames, position = [], 1
    while position < len(lines):
        match = re.fullmatch(r'  File "([^"]+)", line ([0-9]+), in (.+)', lines[position])
        if match is None: break
        path, number, function = match.groups(); number = int(number)
        require(Path(path).name == source.name and 1 <= number <= len(source_lines), '用户帧文件/行号不符')
        frames.append([number, function]); position += 1
        if position < len(lines) and lines[position].startswith('    '):
            require(lines[position].strip() == source_lines[number - 1].strip(), '源码展示不符，不吞任意stderr')
            position += 1
            if position < len(lines) and re.fullmatch(r'    [ ~^]*[~^][ ~^]*', lines[position]): position += 1
    require(frames == row['frames'], '完整用户帧序列不符')
    require(lines[position:] == [row['message']], '异常类型/消息/附加stderr不符')
    return dict(passed=True, status='passed', exit=1, user_frames=frames, stderr_comparison='完整语义帧；原stderr不改，不声称逐字相等')


def verify_row(prefix, argv, row, source):
    record, streams = verify_process(prefix, argv)
    if row['kind'] == 'trace': return trace_result(record, streams, row, source)
    require(record['exit'] == 0 and streams == {'stdout': row['stdout'].encode(), 'stderr': b''}, '完整stdout/stderr/exit不符')
    return dict(passed=True, status='passed', kind=row['kind'], cp_equivalent=row['kind'] == 'cp')
