"""旧/新真实Session产物的托管宿主：固定旧tree单独构建，不伪造source/frame，不复用角色obj。"""
import json
import os
import shutil
from pathlib import Path
import sys

from bootstrap import identity, files, pe_review, space
from guards import require, sha, save
from run import base_environment
from run_projects_gate import candidate_identity

BASELINE = '7545eae82270797cbac2da68e996a7a8a5ba98af'
BASE_TREE = '802df207c7b4bd47a188929f70a1077cbbe5cf38'


def old_pe_review(parsed, expected_pe):
    # 旧控制没有新source协议，只消费固定旧词法定位；外层仍硬核源码、实际图和Runtime角色。
    require(parsed.get('role') == 'old' and parsed.get('case_id') == 'host_xy' and
            parsed.get('sha256') == expected_pe and parsed.get('pe_sha256') == expected_pe,
            '旧host PE角色/输入不符')
    require(parsed.get('admitted') is False and parsed.get('old_locator_verified') is True,
            '旧host缺固定locator，或错误冒充新source票')
    return {key: parsed[key] for key in ('role', 'case_id', 'pe_sha256', 'admitted', 'old_locator_verified')}


def compare_host(cp, old, new):
    expected, previous, observed = (raw.decode().splitlines() for raw in (cp, old, new))
    require(len(expected) == len(observed), '新host完整观察行数改变')
    old_none = [s for s in previous if s.startswith('entry-fallback none-control ')]
    require(len(old_none) == 1, '旧host缺唯一None原值/身份/副作用观察')
    boundary = []
    for a, b in zip(expected, observed):
        if a.startswith('entry-fallback none-control ') and a != b:
            require(old_none[0] != a, '旧host入口None本来正确而新guard拒绝，停止兼容决策')
            require(b == "entry-fallback none-control error NotImplementedError TypedDict metaclass default module filling is not supported ['entry-bool']",
                    '新host None非预定一次真值NI边界')
            boundary.append({'old': old_none[0], 'cp': a, 'new': b, 'classification': 'explicit-boundary-old-was-incorrect'})
        else:
            require(a == b, '新host非边界行不符：' + a)
    return {'ordinary_lines_match_cp': True, 'none': boundary, 'old_product_is_control_not_pass': True}


def execute(repo, out, run, input_root, tools, env, oracle_out):
    out.mkdir(); oracle_out.mkdir(parents=True)
    # 可再生Rust/MSBuild缓存不进入证据artifact，也不写INPUT或baseline。
    scratch = Path(os.environ['RUNNER_TEMP']) / 'source-provenance-host-build'
    scratch.mkdir(exist_ok=False)
    contract = json.loads((input_root / 'contract.json').read_text())['host']
    root = (input_root / contract['entry']).parent
    before_input = files(input_root)
    cp = run.run('host-cp', [tools['files']['cp']['path'], '-B', '-S', input_root / contract['cp_entry']],
                 cwd=out, env=env, check=True, timeout=15)
    require(not cp.stderr, 'host参考stderr非空')
    (oracle_out / 'stdout').write_bytes(cp.stdout); (oracle_out / 'stderr').write_bytes(cp.stderr)
    save(oracle_out / 'result.json', {'exit': 0, 'entry_sha256': sha(input_root / contract['cp_entry']), 'source_root': str(root)})
    baseline = repo / 'target/source-provenance-baseline'
    require(baseline.is_dir(), '缺固定公开基线checkout，不从网络猜旧artifact')
    old_before = candidate_identity(baseline)
    require(old_before['head'] == BASELINE and not old_before['status'], '旧host不是固定clean baseline')
    tree = run.run('host-old-tree', ['git', 'rev-parse', 'HEAD^{tree}'], cwd=baseline, env=env, check=True, timeout=15)
    require(tree.stdout.decode().strip() == BASE_TREE, '旧源码tree不等于510参考')
    try:
        dotnet = tools['files']['dotnet']['path']; cargo = tools['files']['cargo']['path']
        runtime_bin = scratch / 'old-runtime-bin'; runtime_obj = scratch / 'old-runtime-obj'
        run.run('host-old-runtime-build', [dotnet, 'build', baseline / 'runtime/RyuPy.Runtime/RyuPy.Runtime.csproj',
                '-c', 'Release', '-nologo', '-p:BaseIntermediateOutputPath=' + str(runtime_obj) + '/',
                '-p:MSBuildProjectExtensionsPath=' + str(runtime_obj) + '/', '-o', runtime_bin],
                cwd=baseline, env=env, check=True, timeout=600)
        old_runtime = runtime_bin / 'RyuPy.Runtime.dll'
        require(old_runtime.is_file(), '旧Runtime构建没有真实DLL')
        saved_runtime = out / 'old-runtime.dll'
        shutil.copyfile(old_runtime, saved_runtime)
        require(sha(saved_runtime) == sha(old_runtime), '旧Runtime证据副本改变')
        save(out / 'old-runtime-identity.json', {'source': old_before, 'tree': BASE_TREE,
             'runtime': identity(old_runtime), 'artifact_copy': identity(saved_runtime),
             'sdk_dotnet': tools['files']['dotnet'],
             'binding': '本次固定同树源码新构建，非声称历史1c9134二进制逐字一致'})
        outputs = {}
        for role, source_repo, runtime in [('old', baseline, old_runtime), ('new', repo, Path(tools['files']['runtime']['path']))]:
            space(out)
            role_root = out / role; role_root.mkdir()
            role_env = base_environment(Path(dotnet), runtime)
            target = scratch / 'old-rust-target' if role == 'old' else repo / 'target'
            role_env['CARGO_TARGET_DIR'] = str(target)
            if role == 'old':
                # 仅去除旧对照的调试/增量缓存；不改变优化级别、语义feature或新工具配置。
                role_env.update(CARGO_PROFILE_DEV_DEBUG='0', CARGO_INCREMENTAL='0', CARGO_BUILD_JOBS='2')
            harness = source_repo / contract['session_harness']
            require(sha(harness) == sha(repo / contract['session_harness']), '旧新Session示例不是同字节')
            run.run('host-' + role + '-session-build', [cargo, 'build', '--locked', '-p', 'ryupy-driver',
                    '--example', 'instance_migration', '--features', 'ordinary-dynamic-module-pilot'],
                    cwd=source_repo, env=role_env, check=True, timeout=900)
            example = target / 'debug/examples/instance_migration'
            graph_out = role_root / 'graph'
            built = run.run('host-' + role + '-graph', [example, input_root / contract['entry'], graph_out,
                            root, *contract['instance_modules']], cwd=role_root, env=role_env, check=True, timeout=120)
            # 既有example成功只打印实际artifact.dll，不能按目录猜入口或借另一角色PE。
            lines = built.stdout.decode().splitlines(); require(len(lines) == 1, 'Session输出不是唯一实际PE')
            pe = Path(lines[0]).resolve(strict=True)
            require(pe.is_relative_to(graph_out.resolve()) and pe.suffix == '.dll', 'Session PE超出本次角色目录')
            graph = json.loads((graph_out / 'module-environment.json').read_text())
            require({m['module'] for m in graph['modules']} == set(contract['graph']), 'X/Y真实图改变')
            for module in graph['modules']:
                expected = 'InstanceGlobals' if module['module'] in contract['instance_modules'] else 'LegacySingleton'
                require(module['execution'] == expected, 'X/Y实际环境分类不符')
            require(sha(pe.parent / 'RyuPy.Runtime.dll') == sha(runtime), 'Session图Runtime角色错配')
            parsed = run.run('host-' + role + '-pe', [sys.executable, '-B', input_root / 'ci/read_pe.py', pe, role, 'host_xy'],
                             cwd=role_root, env=role_env, check=True, timeout=30)
            decoded = json.loads(parsed.stdout)
            review = old_pe_review(decoded, sha(pe)) if role == 'old' else pe_review(decoded, role, 'host_xy', sha(pe))
            save(role_root / 'pe-review.json', review)
            obj, binary = scratch / (role + '-obj'), scratch / (role + '-bin')
            require(obj != binary and not obj.exists() and not binary.exists(), '宿主角色obj/bin不独立')
            run.run('host-' + role + '-build', [dotnet, 'build', input_root / contract['project'], '-c', 'Release', '-nologo',
                    '-p:RuntimePath=' + str(runtime), '-p:BaseIntermediateOutputPath=' + str(obj) + '/',
                    '-p:MSBuildProjectExtensionsPath=' + str(obj) + '/', '-o', binary],
                    cwd=role_root, env=role_env, check=True, timeout=300)
            host = binary / 'Probe.dll'
            shutil.copyfile(host, role_root / 'Probe.dll')
            require(sha(binary / 'RyuPy.Runtime.dll') == sha(runtime), '托管宿主复制了另一角色Runtime')
            save(role_root / 'identities.json', {'example': identity(example), 'runtime': identity(runtime),
                 'host': identity(host), 'product': identity(pe), 'obj': str(obj), 'bin': str(binary)})
            frozen = files(binary) | {'product/' + k: v for k, v in files(pe.parent).items()}
            actual = run.run('host-' + role + '-jit', [dotnet, host, role, pe], cwd=role_root, env=role_env, check=True, timeout=20)
            require(not actual.stderr, role + 'host stderr非空')
            require(frozen == files(binary) | {'product/' + k: v for k, v in files(pe.parent).items()}, '宿主运行改变实际产物')
            outputs[role] = actual.stdout
        save(out / 'comparison.json', compare_host(cp.stdout, outputs['old'], outputs['new']))
    finally:
        old_after = candidate_identity(baseline); save(out / 'old-source-after.json', old_after)
        require(old_before == old_after and before_input == files(input_root), '旧源码或host输入被构建污染')
