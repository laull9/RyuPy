"""JIT空资源子包与显式Native竖切；复用现工具/记录/隔离，不构建第二套框架。"""
import json
import os
from pathlib import Path
import shutil
import sys

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]
sys.path.insert(0, str(HERE.parent / 'pyfiglet_ci'))
sys.path.insert(1, str(HERE.parent))
from run import Recorder, base_environment, positive
from guards import require, sha, save, reported_managed
from aot_layout import reported_aot
from manifest_verify import verify_manifest, relocate_manifest, read_sdk_version
from native_evidence import preserve_dist, snapshot
from docker_image import IMAGE, INSPECT
from pe_gate import check, bind_native_inputs


def graph(path, explicit, *, aot=False):
    report = json.loads(path.read_text())
    rows = report['modules']
    require({row['module'] for row in rows} == {'__main__', 'pkg', 'pkg.data'} and len(rows) == 3, '图必须恰3模块')
    require(all(row['compiled'] and row['execution'] == 'LegacySingleton' for row in rows), '包/Static环境不能改Instance')
    found = {row['module']: row['resource_discovery'] for row in rows if 'resource_discovery' in row}
    require((not found) if explicit or aot else set(found) == {'pkg.data'}, '发现理由与真实构建臂不符')
    return report


def execute(out, run):
    require(os.environ.get('GITHUB_ACTIONS') == 'true' and os.environ.get('RUNNER_ENVIRONMENT') == 'github-hosted', '仅hosted固定工具链')
    require(sys.version_info[:3] == (3, 12, 13), 'CP必须固定3.12.13')
    cli = REPO / 'target/debug/ryupy'
    runtime = REPO / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
    dotnet = Path(os.environ['DOTNET']).resolve()
    env = base_environment(dotnet, runtime)
    source = REPO / 'tests/fixtures/resource_discovery'
    # 官方CP入口可为链接；只在工具身份入口解析，sha仍只接受普通文件。
    frozen = {str(path): sha(path) for path in [cli, runtime, dotnet, Path(sys.executable).resolve(strict=True)]}
    runtime_size = runtime.stat().st_size
    frozen.update({str(path): sha(path) for path in source.rglob('*') if path.is_file()})
    prior = REPO / 'target/pyfiglet-deployment-evidence'
    require(json.loads((prior / 'result.json').read_text())['status'] == 'explicit-deployment-passed', '先保原pyfiglet完整部署门')
    prior_tools = json.loads((prior / 'input-tools.json').read_text())
    require(prior_tools['source'][0] == os.environ['GITHUB_SHA'] and
            all(prior_tools['files'][str(path)] == frozen[str(path)] for path in (cli, runtime, dotnet)), '不是同HEAD原门同工具')
    save(out / 'inputs.json', frozen)
    decoder = REPO / 'tests/validation/source_provenance/ci/read_pe_decoder.py'
    save(out / 'decoder.json', {'path': str(decoder), 'sha256': sha(decoder)})
    def cwd(label):
        path = out / ('cwd-' + label); path.mkdir(); return path
    def pe(label, dll, fixture):
        require(sha(dll.parent / 'RyuPy.Runtime.dll') == frozen[str(runtime)], 'Runtime不是本轮候选')
        parsed = run.run(label + '-pe', [sys.executable, '-B', decoder, dll, 'new', 'resource_discovery'],
                         cwd=out, env=env, check=True, timeout=30)
        result = check(json.loads(parsed.stdout), fixture)
        require(result['pe_sha256'] == sha(dll), 'PE审核不是同一字节')
        result['pe_bytes'] = dll.stat().st_size
        save(out / (label + '-pe.json'), result)
        return result
    def build(label, root, explicit, fixture, native=False):
        target = out / label
        args = [cli, 'build', root / 'entry.py', '--diagnostic-stages', '--out-dir', target]
        if explicit: args += ['--include-module', 'pkg.data']
        if native: args += ['--aot', '--resources', root / 'resources.json']
        actual = run.run(label, args, cwd=cwd(label), env=env, check=True, timeout=1500 if native else 120)
        if native: return reported_aot(actual.stderr, target, 'entry', 'linux-x64')
        dll = reported_managed(actual.stderr, target)
        graph(dll.parent / 'module-environment.json', explicit)
        pe(label, dll, fixture)
        return dll
    try:
        # 原输入逐字物化，保CP rebound与既有Static差异两种独立oracle。
        original = out / 'original'; original.mkdir()
        shutil.copytree(source / 'pkg', original / 'pkg')
        shutil.copyfile(source / 'static_entry.py', original / 'entry.py')
        frozen.update({str(path): sha(path) for path in original.rglob('*') if path.is_file()})
        cp = run.run('original-cp', [sys.executable, '-B', '-S', original / 'entry.py'], cwd=cwd('original-cp'), env=env, timeout=20)
        positive(cp, (source / 'static.cp.stdout').read_bytes())
        for label, explicit in [('original-default', False), ('original-include', True)]:
            dll = build(label, original, explicit, 'original')
            actual = run.run(label + '-jit', [dotnet, dll], cwd=cwd(label + '-jit'), env=env, timeout=20)
            positive(actual, (source / 'static.legacy.stdout').read_bytes())
        generic = out / 'generic'; shutil.copytree(source / 'native', generic)
        frozen.update({str(path): sha(path) for path in generic.rglob('*') if path.is_file()})
        cp = run.run('resource-cp', [sys.executable, '-B', '-S', generic / 'entry.py'], cwd=cwd('resource-cp'), env=env, timeout=20)
        positive(cp, (generic / 'expected.stdout').read_bytes())
        for label, explicit in [('resource-default', False), ('resource-include', True)]:
            dll = build(label, generic, explicit, 'resource')
            actual = run.run(label + '-jit', [dotnet, dll], cwd=cwd(label + '-jit'), env=env, timeout=20)
            positive(actual, cp.stdout)
        # AOT仍显式include+resources，真实发布后只把dist挂入无源码/SDK容器。
        image_record = json.loads((prior / 'logs/docker-image.json').read_text())
        require(image_record['requested_tag'] == IMAGE, '不是原固定官方镜像')
        image = image_record['id']
        inspected = run.run('docker-inspect', ['docker', 'image', 'inspect', '--format', INSPECT, image],
                            cwd=out, env=env, check=True, timeout=30)
        require(not inspected.stderr and json.loads(inspected.stdout) ==
                {key: value for key, value in image_record.items() if key != 'requested_tag'}, '原不可变镜像身份漂移')
        save(out / 'docker-image.json', image_record)
        layout = build('resource-native', generic, True, 'resource', native=True)
        graph(layout.managed.parent / 'module-environment.json', True, aot=True)
        audited = pe('resource-native', layout.managed, 'resource')
        sdk = read_sdk_version(layout.host / 'sdk.version'); require(sdk == '10.0.403', 'SDK不是固定版本')
        manifest = verify_manifest(layout.dist, layout.native, 'linux-x64', sdk)
        bound = bind_native_inputs(manifest, {
            'compiled_managed_input': {'sha256': audited['pe_sha256'], 'size': audited['pe_bytes']},
            'runtime_managed_input': {'sha256': frozen[str(runtime)], 'size': runtime_size}})
        save(out / 'native-inputs.json', {'managed_path': str(layout.managed), 'runtime_path': str(runtime),
                                       'audited_pe': audited, 'verified_build_inputs': bound})
        expected_sources = {'__main__': generic / 'entry.py', 'pkg': generic / 'pkg/__init__.py', 'pkg.data': generic / 'pkg/data/__init__.py'}
        require({row['module_name']: row['compiled_text_sha256'] for row in manifest['sources']} ==
                {name: sha(path) for name, path in expected_sources.items()}, 'Native实际来源不等于本次完整源码')
        resources = manifest['resources']; require(len(resources) == 1, 'Native只能带一个显式资源')
        resource = resources[0]
        data_id = next(row['id'] for row in manifest['modules'] if row['name'] == 'pkg.data')
        require(resource['consumer_module_ids'] == [data_id] and resource['size'] == 14, '资源consumer/大小不符')
        require(resource['sha256'] == sha(generic / 'pkg/data/sample.txt'), 'Native资源非原输入')
        before = preserve_dist(layout.dist, out)
        positive(run.run('native-original', [layout.native], cwd=cwd('native-original'), env=env, timeout=30), cp.stdout)
        moved = out / 'moved'
        binary, _ = relocate_manifest(layout.dist, layout.native, moved, 'linux-x64', sdk)
        positive(run.run('native-moved', [binary], cwd=cwd('native-moved'), env=env, timeout=30), cp.stdout)
        script = ('for tool in dotnet python python3 python3.12; do '
                  'if command -v "$tool" >/dev/null 2>&1; then exit 97; fi; done; exec "$1"')
        positive(run.run('native-isolated', ['docker', 'run', '--pull=never', '--rm', '--network', 'none', '--read-only',
                 '--mount', f'type=bind,src={moved},dst=/app,readonly', '--workdir', '/app', '--entrypoint', '/bin/sh',
                 image, '-c', script, 'resource-discovery', '/app/' + binary.name], cwd=out, env=env, timeout=30), cp.stdout)
        require(snapshot(layout.dist)[0] == before and snapshot(moved)[0] == before, 'Native交付物搬移改变')
        # 独立副本缺资源必须失败，不能回退源码目录；原dist及原资源不删除。
        negative = out / 'missing'; missing_binary, _ = relocate_manifest(layout.dist, layout.native, negative, 'linux-x64', sdk)
        (negative / resource['relative_path']).unlink()
        missing = run.run('native-missing', [missing_binary], cwd=cwd('native-missing'), env=env, timeout=30)
        require(missing.returncode == 1 and b'resource-data' not in missing.stdout and b'FileNotFoundError' in missing.stderr,
                '缺资源未按真实文件错误失败或回退源目录')
    finally:
        checks = {path: sha(Path(path)) == digest for path, digest in frozen.items()}
        save(out / 'unchanged.json', checks); require(all(checks.values()), '原输入或工具变化')


def main():
    out = REPO / 'target/resource-discovery-evidence'; out.mkdir(parents=True, exist_ok=False)
    run = Recorder(out / 'logs'); report = {'passed': False, 'performance': 'not-measured'}
    try:
        execute(out, run); report['passed'] = True
    except Exception as error:
        report['error'] = repr(error); raise
    finally:
        report['commands'] = list(run.records); save(out / 'result.json', report)


if __name__ == '__main__':
    main()
