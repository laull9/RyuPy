"""同run成对计量的透明小输入包；不传旧CLI/跨run ELF，不信任原runner绝对路径。"""
import json
import os
from pathlib import Path
import shutil
import tarfile
import xml.etree.ElementTree as ET
import runner as w


def event():
    return {k: os.environ[k] for k in ('GITHUB_SHA', 'GITHUB_RUN_ID', 'GITHUB_RUN_ATTEMPT')}


def pack(out, role, files, details):
    root = out / ('paired-' + role)
    root.mkdir(exist_ok=False)
    for name, source in files.items():
        path = root / name
        path.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, path)
    manifest = {'event': event(), 'role': role, 'details': details,
                'files': {name: {'sha256': w.sha(root / name), 'mode': (root / name).stat().st_mode & 511}
                          for name in sorted(files)}}
    w.save(root / 'INPUTS.json', manifest)
    with tarfile.open(out / ('paired-' + role + '.tar'), 'w') as tar:
        for path in sorted(root.rglob('*')):
            if path.is_file():
                tar.add(path, arcname=str(path.relative_to(root)), recursive=False)


def warm(out, layout, runtime, manifest):
    files = {'managed/kernel.dll': layout.managed,
             'managed/kernel.runtimeconfig.json': layout.managed.parent / 'kernel.runtimeconfig.json',
             'managed/module-environment.json': layout.managed.parent / 'module-environment.json',
             'host/Program.cs': layout.host / 'Program.cs', 'host/Host.original.csproj': layout.host / 'Host.csproj',
             'warm-Runtime.dll': runtime, 'manifest.json': out / 'manifest.json',
             'entry.json': out / 'entry-identity.stdout', 'C3.stdout': out / 'jit.stdout',
             'RESULT.json': out / 'RESULT.json'}
    pack(out, 'warm', files, {'build_inputs': manifest['build_inputs']})


def allocation(out):
    files = {label + '/RyuPy.Runtime.dll': out / label / 'RyuPy.Runtime.dll' for label in ('old', 'new')}
    for name in ('source-identity.json', 'old-commit.stdout', 'old-tree.stdout', 'RESULT.json',
                 'before-identity.stdout', 'trx-verified.json', 'sdk.stdout'):
        files[name] = out / name
    pack(out, 'allocation', files, {})


def unpack(archive, destination, role):
    destination.mkdir(exist_ok=False)
    with tarfile.open(archive) as tar:
        names = [m.name for m in tar.getmembers()]
        w.require(len(names) == len(set(names)), '包内重复路径')
        for member in tar.getmembers():
            path = Path(member.name)
            w.require(member.isfile() and not path.is_absolute() and '..' not in path.parts,
                      '包内非普通安全文件')
        tar.extractall(destination, filter='data')
    manifest = json.loads((destination / 'INPUTS.json').read_text())
    w.require(manifest['event'] == event() and manifest['role'] == role, '非同run/attempt/HEAD输入')
    actual = {str(p.relative_to(destination)) for p in destination.rglob('*') if p.is_file()}
    w.require(actual == set(manifest['files']) | {'INPUTS.json'}, '包内集合不符')
    for name, row in manifest['files'].items():
        p = destination / name
        w.require(w.sha(p) == row['sha256'] and p.stat().st_mode & 511 == row['mode'], '包内身份不符')
    return manifest


def project(original):
    """仅迁移两个HintPath及SDK记录路径，保原程序集名/全部严格属性；两臂项目逐字相同。"""
    tree = ET.fromstring(original)
    expected = {'PublishAot': 'true', 'Optimize': 'true', 'TreatWarningsAsErrors': 'true',
                'IlcTreatWarningsAsErrors': 'true', 'ILLinkTreatWarningsAsErrors': 'true',
                'EnableDefaultCompileItems': 'false', 'TrimmerSingleWarn': 'false',
                'OutputType': 'Exe', 'TargetFramework': 'net10.0'}
    props = {e.tag: e.text for g in tree.findall('PropertyGroup') for e in g}
    w.require(set(props) == set(expected) | {'AssemblyName'} and all(props[k] == v for k, v in expected.items()),
              '原AOT属性超出已审模板')
    name = props['AssemblyName']
    w.require(name.startswith('RyuPyAotHost_') and name.replace('_', '').isalnum(), '程序集名不符')
    refs = tree.findall('./ItemGroup/Reference')
    w.require(len(refs) == 2 and {r.attrib['Include'] for r in refs} == {'kernel', 'RyuPy.Runtime'}, '引用不符')
    for ref in refs:
        ref.find('HintPath').text = '../managed/' + ref.attrib['Include'] + '.dll'
    targets = tree.findall('Target')
    w.require(len(targets) == 1 and targets[0].attrib == {'Name': 'RyuPyRecordSdkVersion', 'BeforeTargets': 'Publish'}, 'SDK目标不符')
    writes = targets[0].findall('WriteLinesToFile')
    w.require(len(writes) == 1 and writes[0].attrib['Lines'] == '$(NETCoreSdkVersion)', 'SDK记录不符')
    writes[0].set('File', 'sdk.version')
    return ET.tostring(tree, encoding='unicode') + '\n', name


def ilc_binding(host_root, expected):
    """固定SDK Native.targets的逐行IlcCompileInput/-r:格式；只认本次唯一响应与实际字节。"""
    responses = list(host_root.rglob('*.ilc.rsp'))
    w.require(len(responses) == 1, '必须唯一实际ILC响应')
    response = responses[0]
    found = {name: [] for name in expected}
    for raw in response.read_text(encoding='utf-8-sig').splitlines():
        value = raw.strip()
        role = 'input'
        if value.startswith('-r:'):
            role = 'reference'
            value = value[3:]
        elif value.startswith('-'):
            continue
        value = value.strip('"')
        path = Path(value)
        if path.name not in found:
            continue
        path = path if path.is_absolute() else host_root / path
        w.require(path.is_file() and not path.is_symlink(), 'ILC所用输入不存在')
        required_role = 'reference' if path.name in ('kernel.dll', 'RyuPy.Runtime.dll') else 'input'
        w.require(role == required_role, 'ILC输入/引用角色不符')
        found[path.name].append(path.resolve())
    actual = {}
    for name, paths in found.items():
        w.require(len(paths) == 1 and w.sha(paths[0]) == w.sha(expected[name]), 'ILC输入缺失/重复/非预期:' + name)
        actual[name] = {'path': str(paths[0]), 'sha256': w.sha(paths[0]), 'expected_sha256': w.sha(expected[name])}
    return {'response_path': str(response), 'response_sha256': w.sha(response), 'inputs': actual}


def host_identity_slots(data):
    """精确定位唯一Module.Mvid与已知非执行调试身份；不屏蔽GUID堆/节/IL。"""
    def number(offset, size=4):
        w.require(0 <= offset <= len(data) - size, 'PE字段越界')
        return int.from_bytes(data[offset:offset + size], 'little')
    pe = number(60)
    w.require(data[pe:pe + 4] == b'PE\0\0', '非PE')
    opt = pe + 24
    sections = opt + number(pe + 20, 2)
    def address(rva):
        matches = []
        for i in range(number(pe + 6, 2)):
            s = sections + i * 40
            base, size = number(s + 12), number(s + 16)
            if base <= rva < base + size:
                matches.append(number(s + 20) + rva - base)
        w.require(len(matches) == 1, 'PE RVA非唯一实物范围')
        return matches[0]
    magic = number(opt, 2)
    w.require(magic in (0x10b, 0x20b), 'PE格式未知')
    dirs = opt + (96 if magic == 0x10b else 112)
    cli = address(number(dirs + 14 * 8))
    metadata = address(number(cli + 8))
    w.require(data[metadata:metadata + 4] == b'BSJB', '非CLR metadata')
    pos = metadata + 16 + number(metadata + 12)
    pos = metadata + ((pos - metadata + 3) & ~3)
    count = number(pos + 2, 2); pos += 4
    streams = {}
    for _ in range(count):
        start, size = number(pos), number(pos + 4)
        end = data.index(0, pos + 8)
        name = data[pos + 8:end].decode('ascii')
        w.require(name not in streams, '重复metadata流')
        streams[name] = (metadata + start, size)
        pos = metadata + ((end - metadata + 4) & ~3)
    w.require('#~' in streams and '#GUID' in streams, '需标准表和GUID流')
    table = streams['#~'][0]
    heap = number(table + 6, 1); valid = number(table + 8, 8)
    w.require(valid & 1, '无Module表')
    pos = table + 24; rows = {}
    for i in range(64):
        if valid >> i & 1:
            rows[i] = number(pos); pos += 4
    w.require(rows[0] == 1, '须唯一Module')
    sw, gw = (4 if heap & 1 else 2), (4 if heap & 2 else 2)
    mvid = number(pos + 2 + sw, gw)
    enc = [number(pos + 2 + sw + gw * i, gw) for i in (1, 2)]
    guid, guid_size = streams['#GUID']
    w.require(mvid == 1 and enc == [0, 0] and guid_size == 16, '额外GUID槽/EncId不允许')
    slots = [('coff_timestamp', pe + 8, 4), ('Module.Mvid', guid, 16)]
    debug, debug_size = address(number(dirs + 6 * 8)), number(dirs + 6 * 8 + 4)
    w.require(debug_size == 84, '未知debug记录数')
    types = set()
    for pos in range(debug, debug + debug_size, 28):
        kind, size, raw = number(pos + 12), number(pos + 16), number(pos + 24)
        w.require(kind not in types and kind in (2, 16, 19), '未知或重复debug类型')
        types.add(kind)
        if kind == 2:
            w.require(size >= 25 and data[raw:raw + 4] == b'RSDS' and number(raw + 20) == 1
                      and data[raw + size - 1] == 0, 'CodeView格式变化')
            slots += [('CodeView.timestamp', pos + 4, 4), ('CodeView.guid', raw + 4, 16)]
        elif kind == 19:
            w.require(size == 39 and data[raw:raw + 7] == b'SHA256\0' and number(pos + 4) == 0,
                      'PDB checksum格式变化')
            slots.append(('PDB.sha256', raw + 7, 32))
        else:
            w.require(size == 0 and raw == 0 and number(pos + 4) == 0, 'reproducible记录变化')
    for _, offset, size in slots:
        w.require(0 <= offset <= len(data) - size, '身份槽越界')
    return slots


def compare_hosts(left, right):
    """保完整两DLL/hash；仅精确非执行身份字段可不同，不声称同DLL/布局无扰动。"""
    a, b = left.read_bytes(), right.read_bytes()
    slots = host_identity_slots(a)
    w.require(len(a) == len(b) and slots == host_identity_slots(b), 'Host结构不同')
    allowed = {i for _, offset, size in slots for i in range(offset, offset + size)}
    differences = [i for i, (x, y) in enumerate(zip(a, b)) if x != y]
    w.require(all(i in allowed for i in differences), 'Host含IL/token/非许可字节变化')
    return {'left_sha256': w.sha(left), 'right_sha256': w.sha(right), 'byte_identical': a == b,
            'permitted_slots': slots, 'difference_offsets': differences,
            'scope': '仅这些非执行身份/调试字段可不同，其余全部PE字节相等；不承诺Native布局无扰动'}
