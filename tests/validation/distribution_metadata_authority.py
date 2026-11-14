"""显式/自动metadata权威的九个有限JIT域；复用旧工具/采集/PE解码，不扩Native。"""
import json
from pathlib import Path
import distribution_metadata_contract as contract
import distribution_metadata_products as product
import typing_noreturn as old

require, sha, save = old.require, old.sha, old.save
CONFIG = old.ROOT / 'tests/validation/distribution_metadata_authority.json'
DATA = json.loads(CONFIG.read_text())
TARGETS = {'neutral_pkg.target', 'closed_target', 'neutral_pkg.missing', 'math', 're'}
NI = {
    'outside': 'NotImplementedError: module is outside the planned source domain\n',
    'dotted': 'NotImplementedError: optional source activation: closed fallback is outside the top-level domain\n',
    'hooks': 'NotImplementedError: optional source activation: closed fallback requires original sys/path without finder hooks\n',
}


def stage_names():
    return ['authority-csharp-tests'] + ['authority-' + n + '-cp' for n in DATA['oracles']] + [
        'authority-' + n + '-jit' for n in DATA['cases']]


def verdict(record, stdout, stderr, expected_stdout, boundary=None):
    """精确自然exit/双流；不吞启动失败、超时、截断或无关NI。"""
    expected_stderr = NI[boundary].encode() if boundary else b''
    require(record.get('exit') == (1 if boundary else 0) and record.get('failure') is None
            and record.get('raw_streams_complete') is True
            and stdout == expected_stdout and stderr == expected_stderr,
            'authority实际exit/完整双流不等于冻结合同（非CP兼容域分别列账）')


def optional_payload(decoded):
    found = []
    for method in decoded['methods']:
        instructions = (method.get('body') or {}).get('instructions', [])
        for index, ins in enumerate(instructions):
            target = ins.get('target', {})
            if target.get('owner') == 'RyuPy.Runtime.PyModules' and target.get('name') == 'ConfigureOptionalSources':
                require(ins['opcode'] == '0x28' and index > 0
                        and instructions[index - 1]['opcode'] == '0x72', 'optional配置非直接ldstr/call')
                require(contract.signature(target['signature_hex'], {}) == ('void', ('string',)),
                        'optional配置完整签名不符')
                found.append(instructions[index - 1].get('string'))
    require(len(found) == 1 and isinstance(found[0], str), '唯一真实optional payload缺失/重复')
    return contract.parse(found[0])


def validate_plan(payload, report, module_report, row):
    """报告只能与真实PE载荷比对；所有分类按本fixture完整目标集合约束。"""
    report = dict(report)
    diagnostics = report.pop('diagnostics', None)
    unavailable = report.pop('metadata_unavailable', None)
    require(diagnostics == {} and unavailable is None, '新有限正/负域不接受延期编译错误/metadata不可用')
    require(payload == report, 'optional报告与实际PE载荷不等')
    fields = {'schema_version', 'roots', 'dependencies', 'locations', 'deferred', 'outside', 'protected'}
    explicit = row['mode'] == 'explicit'
    require(set(payload) == fields | ({'closed_unplanned'} if explicit else set()), 'optional精确字段/版本模式不符')
    require(payload['schema_version'] == (2 if explicit else 1), 'optional版本不符')
    for key in ['roots', 'outside', 'closed_unplanned']:
        values = payload.get(key, [])
        require(isinstance(values, list) and all(type(x) is str for x in values)
                and values == sorted(set(values)), '分类名称非精确去重有序数组')
    require(payload['deferred'] == {}, '不以延期错误冒充目标缺失')
    modules = module_report['modules']
    compiled = {m['module'] for m in modules if m['compiled']}
    selected = {'neutral_pkg.target'} if row['entry'] == 'selected' else set()
    expected_unplanned = TARGETS - compiled - selected
    require(set(payload.get('closed_unplanned', [])) == (expected_unplanned if explicit else set()),
            '显式未选目标集合不精确')
    require(set(payload['outside']) == (set() if explicit else expected_unplanned), '自动Outside集合不精确')
    require(set(payload['roots']) == selected, '候选根变化/丢失')
    if selected:
        require(any(m['module'] == 'neutral_pkg.target' and m['candidate'] for m in modules),
                'selected目标未作为真实冷候选编译')
        require('neutral_pkg.target' in payload['locations'] and 'neutral_pkg.target' in payload['dependencies'],
                'selected真实位置/依赖缺失')
    return {'mode': row['mode'], 'compiled': sorted(compiled), 'selected': sorted(selected),
            'unplanned': sorted(expected_unplanned), 'payload_report_equal': True}


def checkpoint(r, name, target, entry, request, row, roots):
    pe = target / (entry.stem + '.dll')
    require(pe.is_file() and sha(target / 'RyuPy.Runtime.dll') == sha(r.runtime), 'authority PE/runtime身份不符')
    # automatic的审查根由实际argv（入口目录＋两-I）确定，绝不从产品report反推根。
    audit_request = request
    if row['mode'] == 'automatic':
        audit_request = r.out / (name + '-automatic-audit-roots.json')
        save(audit_request, {'schema_version': 1, 'mode': 'closed_snapshot',
             'roots': [{'id': 'search-' + str(i), 'path': str(p)} for i, p in enumerate(roots)]})
        r.frozen[str(audit_request)] = sha(audit_request)
    metadata, payload_file = contract.audit_pe(r, name, pe, target / 'distribution-metadata.json', audit_request)
    require(len(metadata['distributions']) == 1 and len(metadata['distributions'][0]['files']) == 2,
            '真实metadata完整inventory不是固定一分发两文件，拒绝empty退化')
    decoded = contract.parse((r.out / (name + '-pe-decode.stdout')).read_text())
    plan = contract.parse((target / 'optional-sources.json').read_text())
    modules = contract.parse((target / 'module-environment.json').read_text())
    checked = validate_plan(optional_payload(decoded), plan, modules, row)
    for module, location in plan['locations'].items():
        relative = module.replace('.', '/')
        path = roots[1] / (relative + ('/__init__.py' if location['is_package'] else '.py'))
        require(set(location) == {'package', 'file', 'path', 'is_package'}
                and Path(location['path']) == path and location['file'] == path.name
                and path.is_file() and not path.is_symlink(), '候选实际来源不在冻结普通包域')
    r.preserve(target, name + '-pe')
    save(r.out / (name + '-before-execution.json'), dict(checked, pe_sha256=sha(pe),
         runtime_sha256=sha(r.runtime), metadata_payload_sha256=sha(payload_file), module_plan=modules))
    return pe


def jit(r, case, row):
    name = 'authority-' + case
    root = r.fixture / 'authority'
    entry = root / 'entries' / (row['entry'] + '.py')
    target = r.out / 'products' / name
    roots = [entry.parent, root / 'sources', root / 'metadata']
    request = root / 'request.json' if row['mode'] == 'explicit' else None
    args = [r.cli, 'build', entry, '--diagnostic-stages', '--out-dir', target,
            '-I', roots[1], '-I', roots[2]]
    if request is not None: args += ['--distribution-metadata', request]
    r.barrier(name + '-before-compile')
    result = r.command(name + '-compile', args, 180, r.env)
    r.barrier(name + '-after-compile')
    require(result['exit'] == 0 and not result['failure'], 'authority编译失败；JIT未运行')
    pe = r.audit(lambda: checkpoint(r, name, target, entry, request, row, roots))
    label = name + '-jit'
    record = r.run_product(label, [r.dotnet, pe])
    verdict(record, (r.out / (label + '.stdout')).read_bytes(), (r.out / (label + '.stderr')).read_bytes(),
            row['stdout'].encode(), row.get('boundary'))
    return {'scope': 'JIT-only; no new Native', 'cp_equal_contract': not bool(row.get('boundary')),
            'expected_exit': 1 if row.get('boundary') else 0}


def run(r):
    root = r.fixture / 'authority'
    actual = {p.relative_to(root).as_posix(): sha(p) for p in root.rglob('*') if p.is_file()}
    r.audit(lambda: require(actual == DATA['fixture_hashes'], 'authority全部普通包/metadata/入口库存改变'))
    # 不用新snapshot替换旧synthetic payload；旧Native门仍消费旧源、旧oracle。
    for name, row in DATA['oracles'].items():
        paths = [root / 'metadata'] if row['entry'] in ('top', 'hooks') else [root / 'sources', root / 'metadata']
        r.phase('authority-' + name + '-cp', lambda name=name, row=row, paths=paths:
                product.oracle(r, 'authority-' + name, root / 'entries' / (row['entry'] + '.py'),
                               row['stdout'].encode(), paths))
    for case, row in DATA['cases'].items():
        stage = 'authority-' + case + '-jit'
        if r.result['stages']['authority-' + row['oracle'] + '-cp']['passed']:
            r.phase(stage, lambda case=case, row=row: jit(r, case, row))
        else: r.block(stage, '本域固定CP参考未通过，禁止产品补跑')


def managed_tests(r):
    """新增guard两Fact单列；原metadata两Fact合同不放宽。"""
    import collections
    import xml.etree.ElementTree as ET
    target = r.out / 'trx' / 'authority'
    r.checked('authority-managed-tests', [r.dotnet, 'test',
        old.ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj', '-c', 'Release',
        '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~PyClosedSourceFallbackTests',
        '--logger', 'trx;LogFileName=authority.trx', '--results-directory', target], 300)
    rows = ET.parse(target / 'authority.trx').findall('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult')
    expected = ['ClosedPlanValidationRejectsMalformedAndConflictingAuthority',
                'ClosedFallbackPreservesV1AtomicityPriorityAndLateSystemGuards']
    require(collections.Counter(x.attrib['testName'] for x in rows) ==
            collections.Counter('RyuPy.Runtime.Tests.PyClosedSourceFallbackTests.' + n for n in expected)
            and all(x.attrib['outcome'] == 'Passed' for x in rows), 'authority两Fact缺失/重复/失败/跳过')
    r.preserve(target, 'authority-trx')
    return {'total': 2, 'all_passed': True}
