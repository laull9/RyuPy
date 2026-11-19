"""单次公开AOT构建、同managed-input的C3合同，随后同ELF的M6可靠性门。"""
import ast
import hashlib
import json
import math
import os
from pathlib import Path
import shutil
import signal
import statistics
import subprocess
import sys
import time

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(ROOT / 'tests/aot'))
from aot_layout import reported_aot
from manifest_verify import verify_manifest


def require(ok, message):
    if not ok:
        raise RuntimeError(message)


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def save(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n')


def identities(paths):
    return {str(p): {'sha256': sha(p), 'mode': p.stat().st_mode & 0o777}
            for p in sorted(set(paths))}


ENV_KEYS = ('PATH', 'HOME', 'TMPDIR', 'RUNNER_TEMP', 'DOTNET', 'DOTNET_ROOT',
            'DOTNET_CLI_TELEMETRY_OPTOUT', 'DOTNET_NOLOGO', 'CARGO_HOME', 'RUSTUP_HOME',
            'CARGO_TARGET_DIR', 'PYTHON', 'WCWIDTH_SOURCE', 'GITHUB_SHA', 'LC_ALL', 'LANG', 'TZ')


def allowed_env(source):
    return {k: source[k] for k in ENV_KEYS if k in source}


def upload_check(out):
    # 只查本门实际记录的完整执行环境，禁止继承凭据键进入产物。
    allowed = set(ENV_KEYS) | {'RYUPY_RUNTIME_DIR', 'UNICODE_VERSION', 'PYTHONHASHSEED',
                             'PYTHONUTF8', 'PYTHONNOUSERSITE', 'PYTHONDONTWRITEBYTECODE'}
    for path in out.glob('*.json'):
        item = json.loads(path.read_text())
        if isinstance(item, dict) and 'environment' in item:
            require(set(item['environment']) <= allowed, '上传含未允许环境键')


def capture(out, label, argv, env, timeout, cwd, popen=subprocess.Popen):
    require(timeout > 0, 'deadline耗尽')
    record = {'argv': list(map(str, argv)), 'cwd': str(cwd), 'environment': env,
              'timeout_seconds': timeout, 'exit': None, 'timeout': False, 'started': False}
    save(out / (label + '.json'), record)
    with (out / (label + '.stdout')).open('wb') as stdout, (out / (label + '.stderr')).open('wb') as stderr:
        try:
            child = popen(record['argv'], cwd=cwd, env=env, stdout=stdout, stderr=stderr,
                          start_new_session=True)
            record.update(started=True, pid=child.pid)
            try:
                record['exit'] = child.wait(timeout=timeout)
            except subprocess.TimeoutExpired:
                record['timeout'] = True
                try:
                    os.killpg(child.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
                record['exit'] = child.wait()
        except OSError as error:
            record['launch_error'] = str(error)
    record['stdout_sha256'] = sha(out / (label + '.stdout'))
    record['stderr_sha256'] = sha(out / (label + '.stderr'))
    save(out / (label + '.json'), record)
    require(record['started'] and record['exit'] == 0 and not record['timeout'], label + '进程失败')
    return record


def parse_warm(raw):
    lines = raw.decode('utf-8', errors='strict').splitlines()
    require(len(lines) == 17 and lines[-1] == 'complete 16', 'batch数量/末清不完整')
    durations = []
    for i, line in enumerate(lines[:-1]):
        parts = line.split(' ', 3)
        require(len(parts) == 4 and parts[0] == 'batch' and int(parts[1]) == i, 'batch顺序不符')
        elapsed = float(parts[2])
        # 两个明确tuple，而非任意执行输出文本。
        checksum, state = ast.literal_eval('(' + parts[3].replace(') ((', '), ((', 1) + ')')
        require(checksum == (300000, 400000, 200000, 100000), 'checksum不符')
        require(state == (((i + 1) * 800000, 8, 1000, 8), (3, 1, 8, 1), (0, 0, 128, 0)), 'cache不符')
        require(math.isfinite(elapsed), '非有限耗时')
        if i < 4:
            require(elapsed == 0.0, '暖batch不得计时')
        else:
            require(elapsed >= 0.100, '计量区间过短')
            durations.append(elapsed)
    return durations


def reliability(rows):
    require(len(rows) == 6 and all(len(r) == 12 for r in rows), '必须6进程72点')
    require(all(math.isfinite(x) and x >= .1 for r in rows for x in r), '无效耗时')
    labels, order = [], []
    for row in rows:
        require(.95 <= statistics.median(row[6:]) / statistics.median(row[:6]) <= 1.05, '进程内漂移')
        for pair in range(6):
            ratio = row[2 * pair + 1] / row[2 * pair]
            order.append(ratio)
            labels.append(ratio if pair % 2 == 0 else 1 / ratio)
    for ratios in (labels, order):
        require(.95 <= statistics.median(ratios) <= 1.05, 'A/A median')
        require(sum(.9 <= x <= 1.1 for x in ratios) >= 29, 'A/A带内不足')
        require(max(sum(x > 1 for x in ratios), sum(x < 1 for x in ratios)) <= 27, 'A/A同向偏差')
    median = statistics.median(x for row in rows for x in row)
    require(all(abs(statistics.median(r) / median - 1) <= .1 for r in rows), '跨进程漂移')
    return {'reliable': True, 'label_ratios': labels, 'order_ratios': order, 'cross_backend_ratio': None}


def binding(manifest, managed, runtime):
    roles = {x['role']: x for x in manifest['build_inputs']}
    require(roles['compiled_managed_input']['sha256'] == sha(managed), '非同managed-input')
    require(roles['runtime_managed_input']['sha256'] == sha(runtime), 'Runtime混用')


def dist_identity(root):
    result = {}
    for path in [root] + sorted(root.rglob('*')):
        require(not path.is_symlink(), 'dist不接受链接')
        relative = str(path.relative_to(root))
        mode = path.stat().st_mode & 0o777
        if path.is_dir():
            result[relative] = {'kind': 'directory', 'mode': mode}
        else:
            require(path.is_file(), 'dist特殊对象')
            result[relative] = {'kind': 'file', 'mode': mode, 'bytes': path.stat().st_size,
                                'sha256': sha(path)}
    return result


def repository_identity(out, label, env):
    capture(out, label + '-identity', ['git', 'rev-parse', 'HEAD', 'HEAD^{tree}'], env, 30, ROOT)
    capture(out, label + '-clean', ['git', 'status', '--porcelain'], env, 30, ROOT)
    require(not (out / (label + '-clean.stdout')).read_bytes(), '仓库不clean')
    lines = (out / (label + '-identity.stdout')).read_text().splitlines()
    require(len(lines) == 2, 'HEAD/tree身份缺失')
    return lines


def execution_counts(out):
    result = {}
    for group, labels in [('contract', ['jit', 'native', 'moved']),
                          ('measurement', ['warm-' + str(i) for i in range(6)])]:
        records = [json.loads((out / (x + '.json')).read_text()) for x in labels
                   if (out / (x + '.json')).exists()]
        result[group] = {'attempted': len(records), 'started': sum(x['started'] for x in records),
                         'completed': sum(x.get('exit') is not None for x in records),
                         'timed_out': sum(x['timeout'] for x in records)}
    return result


def run(out):
    out.mkdir(parents=True, exist_ok=False)
    outcome = {'passed': False, 'accepted_contracts': 0, 'accepted_samples': 0}
    repo_initial = None
    tool_initial = None
    try:
        build_env = allowed_env(os.environ)
        source = Path(build_env['WCWIDTH_SOURCE']).resolve()
        dotnet = Path(build_env['DOTNET']).resolve()
        require(sys.flags.optimize == 0, '不得-O禁用旧布局检查器')
        expected_sources = json.loads((ROOT / 'tests/aot/wcwidth_ci/source-hashes.json').read_text())
        require({n: sha(source / 'wcwidth' / n) for n in expected_sources} == expected_sources, '锁源不符')
        source_files = [p for p in HERE.rglob('*') if p.is_file() and
                        not {'obj', 'bin', '__pycache__'}.intersection(p.parts)]
        source_files += [source / 'wcwidth' / n for n in expected_sources]
        source_files += [ROOT / 'tests/e2e/stdlib/functools_lru_string_pair.py',
                         ROOT / 'tests/validation/instance_globals/read_entry_identity.py']
        before = identities(source_files)
        save(out / 'source-before.json', before)
        repo_initial = repository_identity(out, 'before', build_env)
        require(repo_initial[0] == build_env['GITHUB_SHA'], 'HEAD不等事件')
        capture(out, 'sdk', [dotnet, '--version'], build_env, 30, ROOT)
        require((out / 'sdk.stdout').read_text().strip() == '10.0.403', 'SDK版本不符')
        tools = {'dotnet': dotnet, 'python': Path(sys.executable).resolve(),
                 'cargo': Path(shutil.which('cargo', path=build_env['PATH'])).resolve(),
                 'rustc': Path(shutil.which('rustc', path=build_env['PATH'])).resolve()}
        require(sha(tools['python']) == '021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7', 'CP工具身份不符')
        for name, version in [('cargo', 'cargo 1.99.1 (5f94df478 2026-08-27)'),
                              ('rustc', 'rustc 1.99.1 (58cf8f9c3 2026-10-13)')]:
            capture(out, name + '-version', [tools[name], '--version'], build_env, 30, ROOT)
            require((out / (name + '-version.stdout')).read_text().strip() == version, 'Rust工具版本不符')
        tool_initial = identities(list(tools.values()))
        save(out / 'tools.json', tool_initial)
        capture(out, 'runtime', ['make', 'runtime'], build_env, 600, ROOT)
        # 不启用pilot feature，专用target防止其它job构建模式混用。
        target = out / 'cargo'
        capture(out, 'compiler', ['cargo', 'build', '--locked', '-p', 'ryupy-cli', '--target-dir', target], build_env, 1200, ROOT)
        cli = target / 'debug/ryupy'
        runtime = ROOT / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
        build_env['RYUPY_RUNTIME_DIR'] = str(runtime.parent)
        entry = out / 'kernel.py'
        shutil.copy2(HERE / 'kernel.py', entry)
        products = out / 'products'
        capture(out, 'publish', [cli, 'build', entry, '--diagnostic-stages', '--out-dir', products,
                                '--aot', '-I', source], build_env, 1200, ROOT)
        layout = reported_aot((out / 'publish.stderr').read_bytes(), products, 'kernel', 'linux-x64')
        manifest = verify_manifest(layout.dist, layout.native, 'linux-x64', '10.0.403')
        binding(manifest, layout.managed, runtime)
        require(sha(layout.managed.parent / 'RyuPy.Runtime.dll') == sha(runtime), '实际JIT Runtime混用')
        require((layout.managed.parent / 'kernel.runtimeconfig.json').is_file(), '缺同PE runtimeconfig')
        capture(out, 'entry-identity', [sys.executable, '-B', ROOT / 'tests/validation/instance_globals/read_entry_identity.py',
                                      layout.managed], build_env, 30, ROOT)
        entry_info = json.loads((out / 'entry-identity.stdout').read_text())
        require(entry_info['sha256'] == sha(layout.managed) and entry_info['method_name'] == 'Main'
                and entry_info['signature_blob'] == '0001081d0e'
                and entry_info['owner']['name'] == 'Module'
                and entry_info['owner']['namespace'] == 'RyuPy.Generated', '实际PE入口不符')
        require((layout.host / 'Program.cs').read_text().strip() ==
                'internal static class Program { private static int Main(string[] args) => global::RyuPy.Generated.Module.Main(args); }',
                'AOT真实Host入口改变')
        plan = json.loads((layout.managed.parent / 'module-environment.json').read_text())
        require(all(x['execution'] == 'LegacySingleton' for x in plan['modules']), '意外Instance')
        require({x['module'] for x in plan['modules']} == {'__main__', 'wcwidth', 'wcwidth.wcwidth',
                'wcwidth.table_zero', 'wcwidth.table_wide', 'wcwidth.table_vs16', 'wcwidth.unicode_versions'}, '源码图改变')
        expected_compiled = {'__main__': sha(entry)}
        for name in expected_sources:
            module = 'wcwidth' if name == '__init__.py' else 'wcwidth.' + name[:-3]
            expected_compiled[module] = sha(source / 'wcwidth' / name)
        require({x['module_name']: x['compiled_text_sha256'] for x in manifest['sources']} == expected_compiled,
                '实际编译源码身份不符')
        shutil.copytree(layout.dist, out / 'relocated')
        moved = out / 'relocated' / layout.native.name
        dist_before = dist_identity(layout.dist)
        require(dist_identity(out / 'relocated') == dist_before, '搬移完整dist内容/size/mode不符')
        save(out / 'dist-identity.json', dist_before)
        require(identities(list(tools.values())) == tool_initial, 'C前工具与起点不同')
        require(repository_identity(out, 'before-c', build_env) == repo_initial, 'C前仓库身份改变')
        files = source_files + [cli, runtime] + list(tools.values())
        files += [p for root in (layout.managed.parent, layout.dist, out / 'relocated')
                  for p in root.rglob('*') if p.is_file() and 'obj' not in p.parts and 'bin' not in p.parts]
        frozen = identities(files)
        save(out / 'identities.json', frozen)
        save(out / 'manifest.json', manifest)
        env = {k: build_env[k] for k in ('PATH', 'DOTNET_ROOT') if k in build_env}
        env.update(LC_ALL='C.UTF-8', LANG='C.UTF-8', TZ='UTC', UNICODE_VERSION='latest',
                   PYTHONHASHSEED='0', PYTHONUTF8='1', PYTHONNOUSERSITE='1', PYTHONDONTWRITEBYTECODE='1')
        def product(label, argv, budget):
            require(identities(files) == frozen, '执行前身份漂移')
            capture(out, label, argv, env, budget, out)
            require(identities(files) == frozen, '执行后身份漂移')
            require(not (out / (label + '.stderr')).read_bytes(), '产品stderr非空')
            return (out / (label + '.stdout')).read_bytes()
        for label, argv in [('jit', [dotnet, layout.managed, 'contract']),
                            ('native', [layout.native, 'contract']), ('moved', [moved, 'contract'])]:
            raw = product(label, argv, 30)
            require(raw == (HERE / 'EXPECTED.txt').read_bytes(), 'CP438B合同不符')
            outcome['accepted_contracts'] += 1
        # 功能先行：C3全过才验pending；不让后续计时噪声挡住真实功能验收。
        from allocation_runner import run_pending
        run_pending(out / 'pending-lru', cli, runtime, dotnet, build_env)
        outcome['pending_jit_passed'] = True
        rows = []
        deadline = time.monotonic() + 600
        for i in range(6):
            remaining = deadline - time.monotonic()
            require(remaining > 0, 'M deadline')
            raw = product('warm-' + str(i), [layout.native, 'warm'], min(60, remaining))
            rows.append(parse_warm(raw))
            outcome['accepted_samples'] += 1
            save(out / 'samples.json', rows)
            require(time.monotonic() <= deadline, 'M结果deadline')
        outcome.update(reliability(rows))
        require(identities(source_files) == before, '源漂移')
        outcome['passed'] = True
    except Exception as error:
        outcome['error'] = str(error)
        raise
    finally:
        final_error = None
        try:
            if repo_initial is not None:
                require(repository_identity(out, 'after', build_env) == repo_initial, '最终仓库身份改变')
            if tool_initial is not None:
                tool_after = identities(list(tools.values()))
                save(out / 'tools-after.json', tool_after)
                require(tool_after == tool_initial, '最终工具身份改变')
        except Exception as error:
            outcome['passed'] = False
            outcome['final_identity_error'] = str(error)
            final_error = error
        outcome['actual_processes'] = execution_counts(out)
        save(out / 'RESULT.json', outcome)
        if outcome['passed']:
            from paired_inputs import warm
            warm(out, layout, runtime, manifest)
        upload_check(out)
        if final_error is not None:
            raise final_error


if __name__ == '__main__':
    if sys.argv[1:] == ['--upload-check']:
        upload_check(ROOT / 'target/wcwidth-warm-evidence')
    else:
        require(sys.argv[1:] == ['--execute'], '须显式--execute')
        run(ROOT / 'target/wcwidth-warm-evidence')
