"""metadata模式的窄审计适配：真实PE载荷、完整目录字节和原AOT schema三方绑定。"""
import copy
import hashlib
import json
from pathlib import Path
import re
import shutil
import runpy
import sys
import typing_noreturn as old
import manifest_verify as manifest

require, sha, save = old.require, old.sha, old.save
FIELDS = 'root path kind metadata_name metadata_version discovery_key shadowed directories files'.split()
FILE_FIELDS = {'path', 'size', 'sha256', 'hex'}
REPORT_FIELDS = {'schema_version', 'mode', 'roots', 'distributions', 'payload_sha256'}


def parse(text):
    return json.loads(text, object_pairs_hook=manifest.no_duplicate_keys)


def digest(raw):
    return hashlib.sha256(raw).hexdigest()


def normalize(name):
    return re.sub('[-_.]+', '_', name).lower()


def validate_snapshot(payload, report, request):
    """payload必须来自实际ldstr，不从report重建后自称产品证据。"""
    data = parse(payload)
    require(set(data) == REPORT_FIELDS - {'payload_sha256'}, 'payload根字段不符')
    require(set(report) == REPORT_FIELDS, 'report缺字段/多字段')
    require(type(data['schema_version']) is int and data['schema_version'] == 1
            and data['mode'] == 'closed_snapshot', 'payload版本/模式不符')
    request = Path(request)
    config = parse(request.read_text())
    roots = {x['id']: request.parent / x['path'] for x in config['roots']}
    require(len(roots) == len(config['roots']) and data['roots'] == list(roots), '完整有序root身份不符')
    inventory = [(key, p.name) for key, root in roots.items() for p in sorted(root.iterdir())
                 if p.name.endswith(('.dist-info', '.egg-info'))]
    require([(d['root'], d['path']) for d in data['distributions']] == inventory, '分发inventory漏项/多项/换序')
    seen = set()
    for dist in data['distributions']:
        require(set(dist) == set(FIELDS), '分发字段不符')
        root = roots[dist['root']] / dist['path']
        require(root.parent == roots[dist['root']] and not root.is_symlink(), '分发路径不符')
        kind = 'dist-info' if root.name.endswith('.dist-info') else 'egg-info'
        require(dist['kind'] == kind, 'kind不符')
        paths = sorted(p for p in root.rglob('*') if p.is_file()) if root.is_dir() else [root]
        expected = {p.relative_to(root).as_posix() if root.is_dir() else 'PKG-INFO': p for p in paths}
        directories = sorted(p.relative_to(root).as_posix() for p in root.rglob('*') if p.is_dir()) if root.is_dir() else []
        require(sorted(dist['directories']) == directories, '空目录/完整目录inventory不符')
        require(len(dist['files']) == len(expected) and {f['path'] for f in dist['files']} == set(expected), '完整files漏项/重复')
        for file in dist['files']:
            require(set(file) == FILE_FIELDS, '文件字段不符')
            source = expected[file['path']]
            require(not source.is_symlink(), '不接受链接证据')
            raw = source.read_bytes()
            require(file['hex'] == raw.hex() and type(file['size']) is int and file['size'] == len(raw)
                    and file['sha256'] == digest(raw), '实际载荷字节/hash不等于真实文件')
        identity = expected['METADATA' if kind == 'dist-info' else 'PKG-INFO'].read_text().split('\n\n', 1)[0]
        for key, field in [('Name', 'metadata_name'), ('Version', 'metadata_version')]:
            values = [line.split(':', 1)[1].strip() for line in identity.splitlines()
                      if ':' in line and line.split(':', 1)[0].lower() == key.lower()]
            require(values == [dist[field]], '原身份Name/Version不符')
        prefix = root.name[:-(len(kind) + 1)].split('-', 1)[0]
        key = normalize(prefix or dist['metadata_name'])
        require(dist['discovery_key'] == key and type(dist['shadowed']) is bool
                and dist['shadowed'] == (key in seen), '路径key/遮蔽错误')
        seen.add(key)
    projected = copy.deepcopy(data)
    for dist in projected['distributions']:
        for file in dist['files']: del file['hex']
    projected['payload_sha256'] = digest(payload.encode())
    require(projected == report, 'report与实际PE payload不等（含完整files和digest）')
    return data


def pe_structure(pe):
    """复用已有只读表解析器，按实际RID解码类型，不把数值索引当固定合同。"""
    base = old.ROOT / 'tests/validation/source_provenance/ci/pe_metadata.py'
    require(sha(base) == 'a43bf4bb9e89ceb169b89a67c8817e8cf2312429cb251a0f36f1b9da8f469bc3', 'PE表解析器身份不符')
    before = sys.argv[:]
    try:
        sys.argv = [str(base), str(pe)]
        meta = runpy.run_path(str(base))
    finally: sys.argv = before
    string = meta['string']
    return {hex((table << 24) | rid): (string(row[2]) + '.' if string(row[2]) else '') + string(row[1])
            for table in (1, 2) for rid, row in enumerate(meta['tables'][table], 1)}


def signature(blob, types):
    raw = bytes.fromhex(blob); offset = 0
    def number():
        nonlocal offset
        require(offset < len(raw), '截短调用签名')
        first = raw[offset]; offset += 1
        if first < 0x80: return first
        size, value = (1, first & 0x3f) if first < 0xc0 else (3, first & 0x1f)
        require(first < 0xe0 and offset + size <= len(raw), '无效压缩签名')
        for byte in raw[offset:offset + size]: value = (value << 8) | byte
        offset += size
        return value
    def value_type():
        tag = number()
        if tag in (1, 14): return 'void' if tag == 1 else 'string'
        require(tag in (17, 18), '本调用签名出现非预期类型')
        coded = number(); table = {0: 2, 1: 1}.get(coded & 3)
        require(table is not None and coded >> 2 > 0, '签名类型token无效')
        name = types.get(hex((table << 24) | (coded >> 2)))
        require(name is not None, '签名类型不存在')
        return ('value ' if tag == 17 else 'class ') + name
    require(number() == 0, '要求非泛型static默认调用约定')
    count = number(); result = value_type(); args = tuple(value_type() for _ in range(count))
    require(offset == len(raw), '调用签名尾部多字节')
    return result, args


def pe_payload(decoded, types):
    methods = [m for m in decoded['methods'] if m['method']['name'] == '__init_functions']
    require(len(methods) == 1 and methods[0]['body'], '唯一注册方法缺失')
    ins = methods[0]['body']['instructions']
    require(len(ins) >= 4 and ins[0]['opcode'] == '0xd0'
            and int.from_bytes(bytes.fromhex(ins[0]['operand_hex']), 'little') >> 24 == 2,
            'owner必须为生成TypeDef token')
    token = int.from_bytes(bytes.fromhex(ins[0]['operand_hex']), 'little')
    require(types.get(hex(token)) == methods[0]['method'].get('owner') == 'RyuPy.Generated.Module',
            'ldtoken实际RID不是注册方法的生成owner')
    require(ins[1]['opcode'] == '0x28' and ins[1].get('target', {}).get('owner') == 'System.Type'
            and ins[1]['target']['name'] == 'GetTypeFromHandle', '真实Type owner调用缺失')
    require(ins[2]['opcode'] == '0x72' and isinstance(ins[2].get('string'), str), '无实际静态载荷')
    require(ins[3]['opcode'] == '0x28' and ins[3].get('target', {}).get('owner') == 'RyuPy.Runtime.PyDistributionSnapshot'
            and ins[3]['target']['name'] == 'Configure', '载荷未进入生产Configure')
    require(signature(ins[1]['target']['signature_hex'], types) ==
            ('class System.Type', ('value System.RuntimeTypeHandle',)), 'GetTypeFromHandle完整签名不符')
    require(signature(ins[3]['target']['signature_hex'], types) ==
            ('void', ('class System.Type', 'string')), 'Configure完整签名不符')
    return ins[2]['string']


def audit_pe(runner, name, pe, report_path, request):
    decoder = old.ROOT / 'tests/validation/source_provenance/ci/read_pe_decoder.py'
    runner.checked(name + '-pe-decode', [runner.python, '-B', decoder, pe, 'old', 'distribution_metadata'], 90)
    decoded = parse((runner.out / (name + '-pe-decode.stdout')).read_text())
    require(decoded['sha256'] == sha(pe), '解码PE身份不符')
    payload = pe_payload(decoded, pe_structure(pe))
    report = parse(report_path.read_text())
    validate_snapshot(payload, report, request)
    saved = runner.out / (name + '-payload.json'); saved.write_text(payload)
    runner.frozen[str(saved)] = sha(saved)
    return report, saved


def schema_projection(data, report):
    """仅剥离已严格核过的新字段的内存副本，原manifest及旧validator均不修改。"""
    options = data['compiler']['options']
    require('distribution_metadata' in options and options['distribution_metadata'] == report,
            'AOT metadata字段缺失/与PE和JIT审计不同')
    projected = copy.deepcopy(data)
    del projected['compiler']['options']['distribution_metadata']
    return projected


def verify_native(dist, native, sdk, report, runtime, pe, source_map, *, decoded=None):
    dist, native = Path(dist).absolute(), Path(native).absolute()
    manifest.plain_stat(dist, directory=True)
    manifest.plain_stat(dist / 'ryupy-artifact.json')
    data = parse((dist / 'ryupy-artifact.json').read_text())
    if data.get('schema_version') == 4:
        require(decoded is not None, 'schema4缺本轮实际PE探针，不能跳过深检')
        from distribution_metadata_schema4 import validate_synthetic_schema4
        validate_synthetic_schema4(data, sdk, report, pe, source_map, decoded)
    else:
        manifest.validate_schema(schema_projection(data, report), 'linux-x64', sdk)
    actual = manifest.check_artifacts(dist, data['artifacts'], 'ryupy-artifact.json', windows=False)
    entry = next(x for x in data['artifacts'] if x['role'] == 'native_executable')
    expected = actual[entry['relative_path']]
    require(native.is_file() and native.samefile(expected) and native.parent.samefile(expected.parent)
            and native.name == expected.name, 'native不是清单唯一产物/父目录/名字')
    manifest.image_header(expected, 'linux-x64')
    inputs = {x['role']: x for x in data['build_inputs']}
    require(inputs['runtime_managed_input']['sha256'] == sha(runtime)
            and inputs['compiled_managed_input']['sha256'] == sha(pe), 'AOT PE/runtime输入身份不符')
    require(len(data['sources']) == len(source_map), '编译源码集合不完整')
    for row in data['sources']:
        require(row['module_name'] in source_map and row['compiled_text_sha256'] == source_map[row['module_name']],
                'AOT源码hash不符')
    return data


def relocate(dist, native, moved):
    shutil.copytree(dist, moved)
    require(old.snapshot(dist) == old.snapshot(moved), '搬移实际全部文件改变')
    return moved / native.relative_to(dist)
