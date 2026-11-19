"""既有feature-off无配置负例与pilot自动空快照分账；不新增CLI/runtime模式。"""
import json
from pathlib import Path
import shutil
import distribution_metadata_contract as contract
import distribution_metadata_products as product
import typing_noreturn as old

require, sha, save = old.require, old.sha, old.save
AUTO_GOLDEN = b'0 False\n'


def tool_path(r):
    return r.out / 'tools/explicit-only/ryupy'


def validate_tool_state(r, state):
    """即使未构建也必须显式保存null；成功工具只能绑定本次固定路径与冻结字节。"""
    require('explicit_cli' in state, 'state缺feature-off工具字段')
    value = state['explicit_cli']
    stage = r.result['stages']['explicit-tools']
    if value is None:
        require(stage['passed'] is False and stage['status'] != 'passed'
                and not r.result['stages']['unconfigured-jit']['passed'],
                'feature-off成功阶段缺工具')
        r.explicit_cli = None
        return
    path = Path(value)
    require(path == tool_path(r) and path != r.cli and path.is_file() and not path.is_symlink()
            and str(path) in r.frozen and sha(path) == r.frozen[str(path)], 'feature-off工具路径/hash改变')
    require(stage['passed'] is True and stage['status'] == 'passed', '未成功工具不能绑定为可用')
    r.explicit_cli = path


def build(r):
    # 不复用pilot输出/旧job缓存；额外一次构建，独立target，原pilot frozen始终不覆盖。
    target = old.ROOT / 'target/metadata-explicit-only-build'
    require(not target.exists() and r.explicit_cli is None, '拒绝feature-off残留构建/重复工具')
    r.barrier('explicit-tools-before')
    argv = ['cargo', 'build', '--locked', '--no-default-features', '-p', 'ryupy-cli', '--target-dir', target]
    record = r.command('explicit-compiler-build', argv, 1200, r.env)
    r.barrier('explicit-tools-after-build')
    require(record['exit'] == 0 and record['failure'] is None and record['raw_streams_complete'],
            'feature-off构建失败，旧负例未执行；不补跑')
    path = tool_path(r); path.parent.mkdir()
    shutil.move(target / 'debug/ryupy', path)
    r.preserve(path.parent, 'explicit-tools')
    r.explicit_cli = path
    return {'mode': 'explicit-only', 'extra_cli_builds': 1, 'timeout_seconds': 1200,
            'target': str(target), 'cli': str(path), 'sha256': sha(path)}


def validate_policy(report, automatic):
    require(report.get('compiled_feature') is automatic and report.get('session_policy') ==
            ('ordinary-dynamic-module-pilot' if automatic else 'explicit-only'), '实际module policy/feature不符')


def forbid_metadata_calls(decoded):
    for method in decoded['methods']:
        for ins in (method.get('body') or {}).get('instructions', []):
            target = ins.get('target', {})
            require(not (target.get('owner') == 'RyuPy.Runtime.PyDistributionSnapshot'
                         and target.get('name') in ('Configure', 'ConfigureAutoUnavailable')),
                    'feature-off PE意外配置metadata')
            require(not (target.get('owner') == 'RyuPy.Runtime.PyModules'
                         and target.get('name') == 'ConfigureOptionalSources'), 'feature-off PE意外配置optional来源')


def decode(r, name, pe):
    decoder = old.ROOT / 'tests/validation/source_provenance/ci/read_pe_decoder.py'
    r.checked(name + '-pe-decode', [r.python, '-B', decoder, pe, 'old', 'distribution_metadata'], 90)
    decoded = contract.parse((r.out / (name + '-pe-decode.stdout')).read_text())
    require(decoded['sha256'] == sha(pe), '解码PE身份不符')
    return decoded


def unconfigured(r):
    require(r.explicit_cli == tool_path(r), '无独立feature-off工具，禁止用pilot替代')
    entry = r.fixture / 'unconfigured.py'; target = r.out / 'products/unconfigured'
    require(not target.exists(), '拒绝残留unconfigured产品')
    args = [r.explicit_cli, 'build', entry, '--diagnostic-stages', '--out-dir', target, '-I', r.fixture]
    r.barrier('unconfigured-before-compile')
    record = r.command('unconfigured-compile', args, 180, r.env)
    r.barrier('unconfigured-after-compile')
    require(record['exit'] == 0 and record['failure'] is None and record['raw_streams_complete'], '无配置旧负例编译失败')
    def check():
        pe = product._checkpoint_pe(r, 'unconfigured', target, entry, None)
        validate_policy(contract.parse((target / 'module-environment.json').read_text()), False)
        forbid_metadata_calls(decode(r, 'unconfigured', pe))
        return pe
    pe = r.audit(check)
    r.run_positive('unconfigured-jit', [r.dotnet, pe], r.data['goldens']['unconfigured'].encode())
    return {'mode': 'explicit-only', 'original_source_and_golden': True, 'cli': str(r.explicit_cli)}


def validate_empty(payload, report, roots):
    data = contract.parse(payload)
    require(data == {'schema_version': 1, 'mode': 'closed_snapshot',
                     'roots': ['search-' + str(i) for i in range(len(roots))], 'distributions': []},
            '自动空载荷非实际空完整域/有序根错误')
    require(report == dict(data, payload_sha256=contract.digest(payload.encode())), '自动空报告/PE载荷不等')
    for root in roots:
        require(root.is_dir() and not root.is_symlink()
                and not any(p.name.endswith(('.dist-info', '.egg-info')) for p in root.iterdir()),
                '自动根存在分发条目，不能自称空')


def autoempty(r):
    name = 'automatic-empty'; entry = r.fixture / 'automatic_empty/main.py'
    target = r.out / 'products' / name
    require(not target.exists(), '拒绝残留自动空产品')
    # 唯一实际自动根是entry父目录；不传显式metadata，也不从report推导根。
    roots = [entry.parent]
    r.barrier(name + '-before-compile')
    record = r.command(name + '-compile', [r.cli, 'build', entry, '--diagnostic-stages', '--out-dir', target], 180, r.env)
    r.barrier(name + '-after-compile')
    require(record['exit'] == 0 and record['failure'] is None and record['raw_streams_complete'], '自动空编译失败')
    def check():
        pe = target / 'main.dll'
        require(pe.is_file() and sha(target / 'RyuPy.Runtime.dll') == sha(r.runtime), '自动空PE/Runtime身份错误')
        validate_policy(contract.parse((target / 'module-environment.json').read_text()), True)
        decoded = decode(r, name, pe)
        payload = contract.pe_payload(decoded, contract.pe_structure(pe))
        report = contract.parse((target / 'distribution-metadata.json').read_text())
        validate_empty(payload, report, roots)
        request = r.out / (name + '-audit-request.json')
        save(request, {'schema_version': 1, 'mode': 'closed_snapshot',
                      'roots': [{'id': 'search-0', 'path': str(roots[0])}]})
        contract.validate_snapshot(payload, report, request)
        r.frozen[str(request)] = sha(request)
        file = r.out / (name + '-payload.json'); file.write_text(payload); r.frozen[str(file)] = sha(file)
        r.preserve(target, name + '-pe')
        save(r.out / (name + '-before-execution.json'), {'pe_sha256': sha(pe), 'runtime_sha256': sha(r.runtime),
             'roots_from_argv': list(map(str, roots)), 'payload_sha256': sha(file), 'mode': 'automatic-empty'})
        return pe
    pe = r.audit(check)
    r.run_positive(name + '-jit', [r.dotnet, pe], AUTO_GOLDEN)


def run(r):
    if r.result['stages']['explicit-tools']['passed']:
        r.phase('unconfigured-jit', lambda: unconfigured(r))
    else: r.block('unconfigured-jit', '独立feature-off工具未成功，原负例未运行')
    entry = r.fixture / 'automatic_empty/main.py'
    r.audit(lambda: require(entry.read_bytes() == (r.fixture / 'empty.py').read_bytes()
            and set(p.name for p in entry.parent.iterdir()) == {'main.py'}, '自动空独立根/原empty字节改变'))
    if r.phase('automatic-empty-cp', lambda: product.oracle(r, 'automatic-empty', entry, AUTO_GOLDEN, [entry.parent])):
        r.phase('automatic-empty-jit', lambda: autoempty(r))
    else: r.block('automatic-empty-jit', '自动空CP完整参考未通过')
