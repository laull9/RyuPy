"""既有hosted门中的来源串行闭包；复用Recorder，不安装工具、不自动重试或扩产品能力。"""
import argparse
import collections
import json
import os
from pathlib import Path
import shutil
import sys
import traceback
import xml.etree.ElementTree as ET

from warning_split import cases, load as load_split

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[3]
INPUT_SOURCE = HERE.parent
sys.path.insert(0, str(REPO / 'tests'))
from run_projects_gate import candidate_identity
sys.path.insert(0, str(REPO / 'tests/aot/pyfiglet_ci'))
sys.path.insert(1, str(REPO / 'tests/aot'))
from run import Recorder, base_environment
from guards import require, sha, save


def identity(path):
    path = Path(path).resolve(strict=True)
    return {'path': str(path), 'sha256': sha(path)}


def files(root):
    result = {}
    for path in sorted(root.rglob('*')):
        require(not path.is_symlink(), '证据/输入不接受未声明链接：' + str(path))
        result[str(path.relative_to(root))] = sha(path) if path.is_file() else {'kind': 'directory'}
    return result


def space(root, initial=None):
    free = shutil.disk_usage(root).free
    require(free >= 2 * 1024**3, '来源stage可用空间低于2GiB，停止不清理证据')
    if initial is not None:
        require(initial - free <= 3 * 1024**3, '来源stage超过3GiB增量预算')
    return free


def materialize(out):
    dest = out / 'INPUT'
    shutil.copytree(INPUT_SOURCE, dest, ignore=shutil.ignore_patterns('__pycache__'))
    contract = json.loads((dest / 'contract.json').read_text())
    for target, source in contract['fixture_materialization'].items():
        path = dest / target; path.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(REPO / source, path)
    for name, value in contract['files'].items():
        require(sha(dest / name) == value, '固定fixture改变：' + name)
    for name, value in json.loads((dest / 'oracles/identity.json').read_text())['files'].items():
        require(sha(dest / 'oracles' / name) == value, '历史旧观察改变：' + name)
    load_split(dest, contract)
    return dest, contract


def tools(mode, before):
    cli = REPO / 'target/debug/ryupy'
    runtime = REPO / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
    if mode == 'jit':
        old = REPO / 'target/source-import-path-evidence'
        require(json.loads((old / 'result.json').read_text())['passed'] is True, '原路径门未通过')
        built = json.loads((old / 'compiled-tools.json').read_text())
        require(built['source_head'] == before['head'] and built['feature'] == 'ryupy-driver/ordinary-dynamic-module-pilot', '原实际工具源码/配置不符')
        for path in [cli, runtime]:
            require(built['files'][str(path)]['sha256'] == sha(path), '原CLI/Runtime已漂移')
    else:
        old = REPO / 'target/wcwidth-aot-evidence'
        require(json.loads((old / 'result.json').read_text())['passed'] is True, '原Legacy门未通过')
        built = json.loads((old / 'compiler-identity.json').read_text())
        require(built['cli_sha256'] == sha(cli) and built['runtime_sha256'] == sha(runtime), 'Legacy实际工具已漂移')
    value = {'role': 'new', 'source_head': before['head'], 'feature': 'ordinary-dynamic-module-pilot',
             'files': {name: identity(path) for name, path in {'cli': cli, 'runtime': runtime,
                'dotnet': os.environ['DOTNET'], 'cp': os.environ['PYTHON'],
                'cargo': shutil.which('cargo'), 'rustc': shutil.which('rustc')}.items()}}
    require(value['files']['cp']['sha256'] == '021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7', 'CP非固定官方安装')
    return value


def verify_trx(path, expected, class_name):
    tree = ET.parse(path)
    definitions = {}
    for row in tree.findall('.//{*}UnitTest'):
        method = row.find('{*}TestMethod')
        require(method is not None and method.attrib['className'] == class_name, 'TRX类归属不符')
        definitions[row.attrib['id']] = method.attrib['name']
    rows = tree.findall('.//{*}UnitTestResult')
    actual = collections.Counter()
    for row in rows:
        require(row.attrib['outcome'] == 'Passed', '来源Runtime存在未通过/跳过')
        actual[definitions[row.attrib['testId']]] += 1
    require(dict(actual) == expected and len(rows) == sum(expected.values()), '来源Runtime具名名单/数量不符，不能zero-test')
    return dict(actual)


def pe_review(parsed, role, case, expected_pe, *, location_mode='jit_files', expected_manifest_sha256=None):
    require(parsed['role'] == role and parsed['case_id'] == case and parsed['sha256'] == expected_pe, 'PE角色/输入不符')
    # 只能消费独立固定表全部关键链实际通过的结果，finite_checks绝不是自动授权。
    require(parsed.get('admitted') is True and parsed.get('pe_sha256') == expected_pe and
            parsed.get('unit_link_mapping_verified') is True and parsed.get('frame_carriers_verified') is True,
            '固定fixture实际来源/字段/origin关键链未通过')
    keys = ['role', 'case_id', 'pe_sha256', 'admitted', 'unit_link_mapping_verified', 'frame_carriers_verified']
    # 同名native_consumers也有正常JIT臂；位置域必须来自实际调用方，不能猜case名称。
    require(location_mode in ('jit_files', 'native_consumers_aot') and
            parsed.get('location_mode') == location_mode, 'PE位置审核域与调用方不符')
    if location_mode == 'native_consumers_aot':
        require(role == 'new' and case == 'native_consumers' and
                isinstance(expected_manifest_sha256, str) and len(expected_manifest_sha256) == 64 and
                all(c in '0123456789abcdef' for c in expected_manifest_sha256) and
                parsed.get('native_manifest_sha256') == expected_manifest_sha256, 'Native缺固定位置域/同次manifest身份')
        keys += ['location_mode', 'native_manifest_sha256']
    else:
        require(expected_manifest_sha256 is None and 'native_manifest_sha256' not in parsed, 'JIT不能携带Native manifest身份')
    return {key: parsed[key] for key in keys}


def dynamic_none(input_root, cp_root, jit_root):
    def line(raw):
        found = [s for s in raw.decode().splitlines() if s.startswith('fallback none-control ')]
        require(len(found) == 1, 'None观察不是唯一行'); return found[0]
    old = line((input_root / 'oracles/td_dynamic.old.stdout').read_bytes())
    cp = line((cp_root / 'td_dynamic/stdout').read_bytes())
    new = line((jit_root / 'logs/td_dynamic-default-jit.stdout').read_bytes())
    if new != cp:
        require(old != cp, '旧正确Dynamic None路径被新guard拒绝，停止兼容决策')
        require(new == "fallback none-control error NotImplementedError TypedDict metaclass default module filling is not supported ['module-bool']",
                'Dynamic None非预定的一次真值NI边界')
    return {'old': old, 'cp': cp, 'new': new, 'status': 'cp-match' if new == cp else 'explicit-boundary-old-was-incorrect'}


def execute_jit(out, run, input_root, contract, toolset, env):
    dotnet = toolset['files']['dotnet']['path']
    tests = REPO / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj'
    spec = json.loads((input_root / 'runtime-tests.json').read_text())
    require(sha(REPO / 'runtime/RyuPy.Runtime.Tests/PySourceProvenanceTests.cs') == spec['source_sha256'], 'Runtime测试名单不是同字节')
    trx = out / 'trx'
    run.run('runtime-tests', [dotnet, 'test', tests, '-c', 'Release', '--no-build', '--no-restore',
            '--filter', 'FullyQualifiedName~' + spec['class'], '--logger', 'trx;LogFileName=source-provenance.trx',
            '--results-directory', trx], cwd=REPO, env=env, check=True, timeout=300)
    save(out / 'runtime-tests.json', verify_trx(trx / 'source-provenance.trx', spec['cases'], spec['class']))
    cp_tools = dict(toolset, role='cp'); save(out / 'tools-cp.json', cp_tools)
    gate = input_root / 'ci/gate.py'
    common = [sys.executable, '-B', gate, '--repo', REPO, '--input', input_root]
    run.run('cp-stage', common + ['--out', out / 'cp', '--role', 'cp', '--phase', 'cp', '--tools', out / 'tools-cp.json'],
            cwd=out, env=env, check=True, timeout=240)
    space(out)
    build = out / 'build'; oracle = out / 'cp/results'
    run.run('build-stage', common + ['--out', build, '--role', 'new', '--phase', 'build', '--tools', out / 'tools-new.json', '--oracle', oracle, '--binding-version', 'lexical-chain-v1'],
            cwd=out, env=env, check=True, timeout=1500)
    reviews = out / 'pe-reviews'; reviews.mkdir()
    for case in cases(contract):
        for arm in case['arms']:
            label = case['id'] + '-' + arm['id']
            product = json.loads((build / 'results' / (label + '.json')).read_text())
            if arm.get('reject_phase') == 'build':
                require(product['classification'] == 'expected-capability-rejection'
                        and product['phase'] == 'build' and product['exit'] == 1 and 'pe' not in product,
                        'Legacy负例不是实际精确编译拒绝，不得当正例跳过')
                continue
            parsed = json.loads((build / 'logs' / (label + '-pe-read.stdout')).read_text())
            save(reviews / (label + '.json'), pe_review(parsed, 'new', case['id'], product['pe']['sha256'],
                 location_mode='jit_files'))
    run.run('jit-stage', common + ['--out', out / 'jit', '--role', 'new', '--phase', 'jit', '--tools', out / 'tools-new.json',
            '--oracle', oracle, '--build-evidence', build, '--pe-reviews', reviews], cwd=out, env=env, check=True, timeout=300)
    save(out / 'dynamic-none.json', dynamic_none(input_root, oracle, out / 'jit'))
    space(out)
    # 真实旧/新宿主在同一次stage完成；不在源码中伪造来源、元类或Module帧。
    from host_gate import execute
    execute(REPO, out / 'host', run, input_root, toolset, env, oracle / 'host_xy')


def execute_native(out, run, input_root, toolset, env):
    oracle = out / 'cp'; target = oracle / 'native_consumers'; target.mkdir(parents=True)
    entry = input_root / 'fixtures/native_consumers/entry.py'
    cp = run.run('native-cp', [toolset['files']['cp']['path'], '-B', '-S', entry], cwd=out, env=env, check=True, timeout=15)
    require(not cp.stderr, 'Native参考stderr非空')
    (target / 'stdout').write_bytes(cp.stdout); (target / 'stderr').write_bytes(cp.stderr)
    save(target / 'result.json', {'exit': cp.returncode, 'entry_sha256': sha(entry), 'source_root': str(entry.parent)})
    common = [sys.executable, '-B', input_root / 'ci/native.py', '--repo', REPO, '--input', input_root,
              '--tools', out / 'tools-new.json', '--oracle', oracle]
    publish = out / 'publish'
    run.run('native-publish', common + ['--out', publish, '--phase', 'publish'], cwd=out, env=env, check=True, timeout=1650)
    parsed = json.loads((publish / 'logs/native-pe-read.stdout').read_text())
    published = json.loads((publish / 'publish.json').read_text())
    review = out / 'native-pe-review.json'
    save(review, pe_review(parsed, 'new', 'native_consumers', published['managed_sha256'],
         location_mode='native_consumers_aot', expected_manifest_sha256=published['manifest_sha256']))
    run.run('native-run', common + ['--out', out / 'run', '--phase', 'run', '--publish-evidence', publish,
            '--pe-review', review], cwd=out, env=env, check=True, timeout=150)


def main():
    parser = argparse.ArgumentParser(description=__doc__); parser.add_argument('mode', choices=['jit', 'native']); args = parser.parse_args()
    require(os.environ.get('GITHUB_ACTIONS') == 'true' and os.environ.get('RUNNER_ENVIRONMENT') == 'github-hosted', '只支持既有hosted路线')
    out = REPO / ('target/source-provenance-evidence' if args.mode == 'jit' else 'target/source-provenance-native')
    out.mkdir(parents=True, exist_ok=False)
    report = {'passed': False, 'mode': args.mode, 'product_acceptance': False, 'scope': '固定来源专项，旧全门结果独立'}
    before = toolset = input_root = original = initial = None
    try:
        initial = space(out); require(initial >= 6 * 1024**3, '开始来源stage前不足6GiB空间')
        run = Recorder(out / 'logs'); before = candidate_identity(REPO)
        save(out / 'source-before.json', before)
        require(before['head'] == os.environ['GITHUB_SHA'] and not before['status'], '候选不是同HEAD clean树')
        toolset = tools(args.mode, before); save(out / 'tools-new.json', toolset)
        env = base_environment(Path(toolset['files']['dotnet']['path']), Path(toolset['files']['runtime']['path']))
        # native.py只消费精确hosted值，不转交凭据或整个环境。
        env.update(GITHUB_ACTIONS='true', RUNNER_ENVIRONMENT='github-hosted', GITHUB_SHA=before['head'])
        input_root, contract = materialize(out); original = files(input_root); save(out / 'input-before.json', original)
        if args.mode == 'jit': execute_jit(out, run, input_root, contract, toolset, env)
        else: execute_native(out, run, input_root, toolset, env)
        report['passed'] = True
    except Exception as error:
        report.update(error=str(error), traceback=traceback.format_exc()); raise
    finally:
        try:
            if before is not None:
                after = candidate_identity(REPO); save(out / 'source-after.json', after)
                require(before == after, '候选源码前后改变')
            if original is not None:
                after_input = files(input_root); save(out / 'input-after.json', after_input)
                require(original == after_input, '固定输入前后改变')
            if toolset is not None:
                unchanged = {k: sha(Path(v['path'])) == v['sha256'] for k, v in toolset['files'].items()}
                save(out / 'tools-after.json', unchanged); require(all(unchanged.values()), '工具前后改变')
            space(out, initial)
        except Exception as error:
            report.update(passed=False, postflight_error=str(error)); raise
        finally:
            save(out / 'result.json', report)


if __name__ == '__main__':
    main()
