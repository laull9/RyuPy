"""产品门草稿的严格比较器；只读冻结CP原件，不执行Python源码或重建oracle。"""
import hashlib
import json
from pathlib import Path
import re
import struct

MANIFEST_SHA = 'ccce5de8d2344934cca51e04df28c6f584d45ee610ec358ae1515be097e213c1'
B_SHA = '3cc2200aae4adf22b3a8156767f1a162c351e2dd5fa43f25f3eba3849b2283f4'
CASES = ('a_all_miss', 'b_target_failure', 'c_nested_same_object', 'd_same_object_escape')
REJECTED = frozenset(CASES[2:])
HOT = 'hot_same_getter'
HOT_SOURCE_SHA = 'ba3cfd9a969dd8e88bad260b3e24b3d44c9d6c8c631abe7fff67e0c4b337c0fa'
FRAME = re.compile(r'  File "([^"\n]+)", line ([0-9]+), in ([^\n]+)\n\Z')
PREFIX = '[RyuPy.Runtime]RyuPy.Runtime.PyTraceProduct::'
SIGNATURES = {'Enter': '[0;0;1](Int64)->Void', 'Explicit': '[0;0;1](Object)->Object',
              'Park': '[0;0;2](Object,Int64)->Object', 'Match': '[0;0;1](Object)->Void',
              'Abort': '[0;0;1](Object)->Void', 'AllMiss': '[0;0;1](Object)->Void',
              'Handle': '[0;0;2](Object,Int64)->Void', 'Forward': '[0;0;2](Object,Int64)->Void',
              'GeneratorCatch': '[0;0;1](Object)->Void', 'GeneratorDepth': '[0;0;0]()->Int64'}


def require(value, message):
    # 不使用assert，python -O也不能抹掉验收条件。
    if not value:
        raise ValueError(message)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def load_goldens(directory):
    """只读最小portable参考；stderr仅已声明的File源路径basename映射，其他字节原样。"""
    directory = Path(directory)
    require(sha(directory / 'manifest.json') == MANIFEST_SHA, 'portable CP清单身份不符')
    manifest = json.loads((directory / 'manifest.json').read_text())
    require(manifest['schema'] == 1 and manifest['cases'] == [*CASES, HOT], '固定case集合/顺序改变')
    require(manifest['python']['implementation'] == 'CPython' and manifest['python']['version'] == '3.12.13'
            and manifest['python']['binary_sha256'] == '021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7'
            and manifest['python']['flags'] == ['-B', '-S']
            and manifest['python']['semantic_environment']['PYTHONOPTIMIZE'] == '0', 'CP工具/优化身份不符')
    expected = {name + suffix for name in (*CASES, HOT) for suffix in ('.py', '.stdout', '.stderr', '.exit')}
    require(set(manifest['files']) == expected, 'portable原件集合缺失/增加')
    require({p.name for p in directory.iterdir()} == expected | {'manifest.json'}, 'portable目录混入额外文件')
    for name, record in manifest['files'].items():
        require(sha(directory / name) == record['sha256'], 'portable输入/原流改变：' + name)
    result = {}
    for name in (*CASES, HOT):
        source = directory / (name + '.py')
        prefix = directory / name
        exit_code = 0 if name == HOT else 1
        require(prefix.with_suffix('.exit').read_bytes() == str(exit_code).encode() + b'\n', 'CP原exit改变')
        stdout, stderr = prefix.with_suffix('.stdout').read_bytes(), prefix.with_suffix('.stderr').read_bytes()
        result[name] = {'source': source, 'source_bytes': source.read_bytes(), 'source_sha256': sha(source),
                        'stdout': stdout, 'stderr': stderr, 'exit': exit_code,
                        'normalized': normalize(stderr, source.read_bytes(), {source.name})}
    require(result['b_target_failure']['source_sha256'] == B_SHA
            and result[HOT]['source_sha256'] == HOT_SOURCE_SHA, '原B/热source身份改变')
    return result


def oracle(directory, fixture):
    result = load_goldens(directory)
    require(sha(fixture) == B_SHA == result['b_target_failure']['source_sha256'], '原B被替换')
    return {name: result[name] for name in CASES}


def normalize(raw, source, allowed_paths):
    """仅替换已知源路径、删除精确源码展示及紧随其后的caret；其余整流必须相等。"""
    lines = raw.decode('utf-8').splitlines(keepends=True)
    code = source.decode('utf-8').splitlines()
    result = []
    i = 0
    while i < len(lines):
        line = lines[i]
        frame = FRAME.fullmatch(line)
        if frame:
            filename, number, function = frame.groups()
            require(filename in allowed_paths, '异常帧来自未批准路径：' + filename)
            number = int(number)
            require(1 <= number <= len(code), '异常行号越界')
            result.append(f'  File "<SOURCE>", line {number}, in {function}\n')
            i += 1
            if i < len(lines) and lines[i] == '    ' + code[number - 1].strip() + '\n':
                i += 1
                if i < len(lines) and re.fullmatch(r' +[~^]*\^[~^]*\n', lines[i]):
                    i += 1
            continue
        # 非规范帧不能作为普通噪声默许。未知文本原样保留并参与全量比较。
        require(not line.lstrip().startswith('File "'), '非法异常帧排版')
        result.append(line)
        i += 1
    return ''.join(result).encode('utf-8')


def compare(reference, record, stdout, stderr, entry):
    require(record.get('failure') is None and record.get('raw_streams_complete') is True,
            '超时/截断/启动失败不能冒普通异常')
    require(type(record.get('exit')) is int and record['exit'] == reference['exit'], 'exit必须精确为' + str(reference['exit']))
    require(stdout == reference['stdout'], '完整stdout不符')
    require(sha(entry) == reference['source_sha256'], '产品运行后原源改变')
    actual = normalize(stderr, reference['source_bytes'], {str(entry), entry.name})
    require(actual == reference['normalized'], '完整stderr不符（仅允许源路径/源码排版归一化）')
    return actual


def rejection(record, stdout, stderr, output):
    require(record.get('failure') is None and record.get('raw_streams_complete') is True
            and type(record.get('exit')) is int and record['exit'] == 1, '拒绝必须是正常编译exit1')
    text = stderr.decode('utf-8')
    require(stdout == b'', '编译拒绝出现stdout；不能接受ready初始化副作用')
    require('[E0310]' in text and 'trace V1 整产物拒绝：' in text, '未命中V1整产物准入诊断')
    require('Traceback (most recent call last):' not in text and '\nready\n' not in text,
            '运行时错误不能当编译拒绝')
    for path in output.rglob('*'):
        require(path.suffix.lower() not in {'.dll', '.exe'} and path.name != 'ryupy-artifact.json',
                '拒绝后发射了托管/交付产物')
        if path.is_file():
            with path.open('rb') as stream:
                require(stream.read(4) != b'\x7fELF', '拒绝后发射了ELF')


def entry_row(raw):
    """从真实PE CLI header取得MethodDef入口，不能只按名字猜Main。"""
    require(raw[:2] == b'MZ', '不是PE')
    pe = struct.unpack_from('<I', raw, 0x3c)[0]
    require(raw[pe:pe + 4] == b'PE\0\0', 'PE签名不符')
    sections = struct.unpack_from('<H', raw, pe + 6)[0]
    size = struct.unpack_from('<H', raw, pe + 20)[0]
    opt = pe + 24
    magic = struct.unpack_from('<H', raw, opt)[0]
    require(magic in (0x10b, 0x20b), '未知PE可选头')
    cli_rva = struct.unpack_from('<I', raw, opt + (96 if magic == 0x10b else 112) + 14 * 8)[0]
    for i in range(sections):
        pos = opt + size + 40 * i
        virtual_size, virtual, disk_size, disk = struct.unpack_from('<IIII', raw, pos + 8)
        if virtual <= cli_rva < virtual + max(virtual_size, disk_size):
            cli = disk + cli_rva - virtual
            flags, token = struct.unpack_from('<II', raw, cli + 16)
            require(not flags & 0x10 and token >> 24 == 6 and token & 0xffffff, '不是托管MethodDef入口')
            return (token & 0xffffff) - 1
    raise ValueError('找不到CLI header')


def audit_pe(decoded, pe, enabled):
    require(Path(decoded['Path']).resolve() == pe.resolve() and decoded['Sha256'].lower() == sha(pe),
            'decoder读取了错误/旧PE')
    methods = decoded['Methods']
    require(len({m['Name'] for m in methods}) == len(methods), '方法身份重复')
    product = [(m, i, ins) for m in methods for i, ins in enumerate(m['IL'])
               if PREFIX in str(ins.get('Arg'))]
    if not enabled:
        require(not product and b'PyTraceProduct\0' not in pe.read_bytes(), '默认PE冒用了产品V1')
        return {'enabled': False, 'sha256': sha(pe)}
    require(product, '缺少实际产品ABI调用')
    root = methods[entry_row(pe.read_bytes())]
    enters = [(m, i) for m, i, ins in product if ins['Arg'] == PREFIX + 'Enter' + SIGNATURES['Enter']]
    require(len(enters) == 1 and enters[0][0] is root, 'Enter必须直接位于真实入口且唯一')
    index = enters[0][1]
    il = root['IL']
    require(index >= 2 and il[index - 1]['Op'] == 'ldc.i8' and str(il[index - 1]['Arg']) == '1',
            '入口必须直接加载Int64 ABI1')
    require(il[index - 2]['Op'] == 'call' and '.PySystem::Initialize[' in str(il[index - 2]['Arg']),
            'Enter必须紧接PySystem.Initialize')
    require(all(not ins['Op'].startswith(('br', 'leave')) and ins['Op'] not in ('ret', 'throw', 'switch')
                for ins in il[:index]), 'Enter前出现可跳过的控制流')
    require(not any(ins['Op'] == 'call' and str(ins.get('Arg')).startswith('RyuPy.Generated.')
                    for ins in il[:index]), '源码/函数工厂在Enter之前初始化')
    allowed = {PREFIX + name + signature for name, signature in SIGNATURES.items()}
    require(all(ins['Op'] == 'call' and ins['Arg'] in allowed for _, _, ins in product), 'ABI签名/调用方式不符')
    names = {m['Name']: m for m in methods}
    reached, pending = set(), [root['Name']]
    while pending:
        name = pending.pop()
        if name in reached:
            continue
        reached.add(name)
        pending.extend(ins['Arg'] for ins in names[name]['IL']
                       if ins['Op'] in ('call', 'callvirt', 'newobj', 'ldftn', 'ldvirtftn')
                       and isinstance(ins.get('Arg'), str) and ins['Arg'] in names)
    require(all(m['Name'] in reached for m, _, _ in product), 'V1调用仅存在于不可达方法')
    for name in ('Explicit', 'GeneratorCatch'):
        require(any(ins['Arg'] == PREFIX + name + SIGNATURES[name] for _, _, ins in product), '缺少真实生成器V1接线')
    return {'enabled': True, 'sha256': sha(pe), 'entry': root['Name'], 'abi': 1,
            'product_calls': [{'method': m['Name'], 'offset': ins['Offset'], 'target': ins['Arg']}
                              for m, _, ins in product]}


def hot_oracle(directory):
    """热case取既有真实exit0参考，不落入C/D编译拒绝集合。"""
    return load_goldens(directory)[HOT]


def hot_site(decoded, repo):
    """复用现有PE参数来源分析，核唯一kind属性站点；不冒称产品hit计数。"""
    import importlib.util
    path = repo / 'tests/validation/foreach_pe_audit/audit.py'
    spec = importlib.util.spec_from_file_location('ryupy_existing_pe_audit', path)
    audit = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(audit)
    sites = audit.pic_sites(decoded)
    matches = []
    target = '[RyuPy.Runtime]RyuPy.Runtime.PyPic::GetAttr[0;0;3](Int32,Object,String)->Object'
    for method_index, site_index, site_id, call_index in sites:
        method = decoded['Methods'][method_index]
        if method['IL'][call_index]['Arg'] != target:
            continue
        origins = audit.arguments(method)[call_index]
        require(len(origins) == 3 and origins[2] >= 0, 'GetAttr属性名来源不明')
        require(audit.op(method['IL'][origins[2]]) == ('ldstr', 'kind'), '热输入出现额外属性读取')
        matches.append({'method': method['Name'], 'site_id': site_id,
                        'site_offset': method['IL'][site_index]['Offset'], 'call_offset': method['IL'][call_index]['Offset']})
    require(len(matches) == 1, '热fixture必须唯一kind/GetAttr站点，禁止两个副本冒同站点')
    return {'sites': matches, 'claim': 'same-site repeated execution coverage; PIC cold/warm mechanism requires separate direct C# test; no product hit counts'}
