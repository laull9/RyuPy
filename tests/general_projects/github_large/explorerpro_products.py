#!/usr/bin/env python3
"""ExplorerPro隔离原业务产品门：同次Cocoa oracle、CIL probe入口与删除输入搬迁。

复用既有explorerpro_probe.py的路径/云发现/剪贴板防线，原业务源码保持原样；本地模块
全部请求CIL编译，不添加业务委派。默认先验JIT，通过后分别请求standalone/onefile。
"""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import plistlib
import re
import shutil
import signal
import subprocess
import time
import traceback

from inspect_sources import CACHE, CATALOG, REPO, analyze
from explorerpro_data import DATA, bundle_manifest
import tempfile
import sys

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parents[2] / 'tests/cpython'))
from support import audit_native_paths, built_product, provider_data



def execute(command, cwd, env, evidence, label, timeout=900, sample_startup=0):
    """保存双流与退出码；超时只回收本次独立进程组，包含上游容量查询子进程。"""
    command = list(map(str, command))
    (evidence / (label + '-command.json')).write_text(json.dumps(command, ensure_ascii=False) + '\n')
    stdout_path, stderr_path = evidence / (label + '-stdout.log'), evidence / (label + '-stderr.log')
    # 实际窗口与原生崩溃诊断需要实时双流，不能等全部业务结束才落盘。
    with stdout_path.open('wb') as stdout_log, stderr_path.open('wb') as stderr_log:
        started = time.monotonic_ns()
        process = subprocess.Popen(command, cwd=cwd, env=env, stdout=stdout_log,
                                   stderr=stderr_log, start_new_session=True)
        sampler = None
        try:
            if sample_startup:
                sampler = subprocess.Popen(['/usr/bin/sample', str(process.pid), str(sample_startup),
                                            '5', '-file', evidence / 'startup-sample.log'],
                                           stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
            process.wait(timeout=timeout)
        except BaseException as error:
            try:
                if label == 'product' and isinstance(error, subprocess.TimeoutExpired):
                    with (evidence / 'product-sample.log').open('wb') as diagnostic:
                        try:
                            subprocess.run(['/usr/bin/sample', str(process.pid), '2'], timeout=10,
                                           stdout=diagnostic, stderr=subprocess.STDOUT, check=False)
                        except (OSError, subprocess.TimeoutExpired) as sample_error:
                            diagnostic.write(f'\n采样未完成：{sample_error}\n'.encode())
            finally:
                os.killpg(process.pid, signal.SIGTERM)
                try:
                    process.wait(timeout=5)
                except subprocess.TimeoutExpired:
                    os.killpg(process.pid, signal.SIGKILL)
                    process.wait(timeout=5)
            raise
        finally:
            if sampler is not None:
                try:
                    sampler.wait(timeout=8)
                except subprocess.TimeoutExpired:
                    sampler.terminate()
                    sampler.wait(timeout=5)
    stdout, stderr = stdout_path.read_bytes(), stderr_path.read_bytes()
    (evidence / (label + '-exit.json')).write_text(json.dumps(
        {'exit': process.returncode, 'started_monotonic_ns': started}))
    return subprocess.CompletedProcess(command, process.returncode, stdout, stderr)


def checked(result, label):
    if result.returncode:
        raise RuntimeError(f'{label} exit={result.returncode}\n{result.stderr.decode(errors="replace")}')
    return result


def normalize(data, profile):
    """只归一化本臂私有profile和Qt已知字体别名初始化耗时，保留其它全部字节。"""
    data = data.replace(str(profile).encode(), b'<PRIVATE_PROFILE>')
    return re.sub(
        rb'(qt\.qpa\.fonts: Populating font family aliases took )\d+( ms\. Replace uses of missing font family "Consolas" with one that exists to avoid this cost\. )',
        rb'\1<TIMING>\2', data)


def relocated_command(executable, cache, original_venv, evidence, cwd, env):
    """先验证原缓存和依赖确实不可读，再给实际产品进程继承同一沙箱。"""
    sandbox = ['/usr/bin/sandbox-exec',
               '-D', f'UPSTREAM={cache}', '-D', f'VENV={original_venv.resolve()}',
               '-p', '(version 1) (allow default) '
                     '(deny file-read* (subpath (param "UPSTREAM")) (subpath (param "VENV")))']
    for label, path in [('upstream', cache), ('venv', original_venv.resolve())]:
        blocked = execute([*sandbox, '/usr/bin/stat', path], cwd, dict(env, LC_ALL='C'), evidence, f'denied-{label}')
        assert blocked.returncode == 1 and b'Operation not permitted' in blocked.stderr, blocked
    return [*sandbox, executable]


def product(args, project, evidence):
    cache = args.cache.expanduser().resolve()
    source = cache / project['repo'].replace('/', '--')
    analyze(project, source)
    original_python = args.python.expanduser().absolute()
    original_venv = original_python.parent.parent
    assert (original_venv / 'pyvenv.cfg').is_file(), '必须提供按explorerpro-requirements准备的venv'
    env = {k: v for k, v in os.environ.items()
           if not k.startswith(('PYTHON', 'RYUPY_', 'LD_', 'DYLD_', 'VIRTUAL_ENV', 'CONDA', 'QT_'))}
    env.update(PYTHONDONTWRITEBYTECODE='1', PYTHONUNBUFFERED='1', PYTHONHASHSEED='0', TZ='UTC',
               QT_QPA_PLATFORM='cocoa')
    with tempfile.TemporaryDirectory(prefix='ryupy-explorer-product-') as temporary:
        work = Path(temporary).resolve()
        assert not work.is_relative_to(REPO)
        (evidence / 'temporary.json').write_text(json.dumps({'work': str(work)}))
        inputs = work / 'inputs'
        inputs.mkdir()
        tree, venv = inputs / 'source', inputs / 'venv'
        shutil.copytree(source, tree, ignore=shutil.ignore_patterns('.git', '.venv', '__pycache__'))
        shutil.copytree(original_venv, venv, symlinks=True, ignore=shutil.ignore_patterns('__pycache__'))
        python = venv / 'bin/python'
        version = checked(execute([python, '-I', '-c', 'import platform; print(platform.python_version())'],
                                  work, env, evidence, 'version'), '版本')
        assert version.stdout.strip() == b'3.12.13', version.stdout
        checked(execute(['uv', 'pip', 'freeze', '--python', python], work, env, evidence, 'freeze'), '依赖freeze')
        entry = tree / 'explorerpro_contract.py'
        shutil.copy2(args.probe_entry, entry)
        shutil.copy2(entry, evidence / entry.name)
        profiles = {}
        for arm in ('oracle', 'product'):
            profile = work / ('profile-' + arm)
            profile.mkdir()
            (evidence / arm).mkdir()
            profiles[arm] = profile
        def runtime_env(arm):
            profile = profiles[arm]
            return dict(env, HOME=str(profile), XDG_CONFIG_HOME=str(profile / 'config'),
                        XDG_CACHE_HOME=str(profile / 'cache'))
        oracle = checked(execute([python, '-B', entry, profiles['oracle'], evidence / 'oracle'],
                                 tree, runtime_env('oracle'), evidence, 'oracle', timeout=150), 'CPython Cocoa基线')
        if args.oracle_only:
            return {'mode': args.mode, 'oracle': 'pass', 'product': 'not-run'}
        output = work / 'output'
        extra = ['--experimental-stock-module-namespaces'] if args.stock_module_namespaces else []
        full_probe = args.probe_entry.resolve() == (HERE / 'explorerpro_probe.py').resolve()
        if full_probe:
            extra.extend(['--bundle-data', bundle_manifest(tree, inputs / 'bundle-data.json')])
        if args.native_components:
            original = args.native_components.expanduser().resolve()
            declaration = json.loads(original.read_text())
            for index, component in enumerate(declaration['components']):
                root = Path(component['root'])
                if not root.is_absolute():
                    root = original.parent / root
                private = inputs / ('component-' + str(index))
                shutil.copytree(root, private, symlinks=True)
                component['root'] = str(private)
            native_declaration = inputs / 'native-components.json'
            native_declaration.write_text(json.dumps(declaration, ensure_ascii=False))
            extra.extend(['--cpython-native-components', native_declaration])
        if args.mode == 'app':
            extra.extend(['--app-name', 'ExplorerPro'])
        built = execute([args.ryupy.resolve(), entry, '--mode', args.mode, '-o', output,
                         '-I', tree / 'src', '-I', tree, *extra], work, dict(env, VIRTUAL_ENV=str(venv)),
                        evidence, 'build')
        checked(built, 'CIL probe项目构建')
        generated = [Path(line[len('已生成 '):]) for line in built.stderr.decode().splitlines()
                     if line.startswith('已生成 ')]
        assert len(generated) == 1, built.stderr
        executable = generated[0]
        if args.mode == 'app':
            bundle = executable
            assert bundle.name == 'ExplorerPro.app'
            layout = plistlib.loads((bundle / 'Contents/Info.plist').read_bytes())
            assert layout['CFBundleExecutable'] == entry.stem and layout['CFBundlePackageType'] == 'APPL'
            executable = bundle / 'Contents/MacOS' / entry.stem
            report_path = executable.parent / 'internal/cpython-provider.json'
        else:
            report_path = provider_data(output, entry.stem, args.mode) / 'cpython-provider.json'
        if args.mode not in ('app', 'onefile'):
            assert built_product(built.stderr, output, entry.stem, args.mode).entry == executable.resolve()
        report = json.loads(report_path.read_text())
        python_bytes = sum(path.stat().st_size for path in (report_path.parent / 'python').rglob('*')
                           if path.is_file())
        assert report['delegated_local_modules'] == [], report['delegated_local_modules']
        assert report['entry_provider'] == 'ryupy_compiled_source', report['entry_provider']
        if full_probe:
            assert set(report['bundle_data']) == {'ryupy_compiled/' + path for path in DATA}, report['bundle_data']
        if args.stock_module_namespaces:
            decisions = report['dependency_collection']['module_namespaces']['modules']
            assert decisions and all(d['compiled'] and d['provider_namespace'] and d['execution'] == 'InstanceGlobals' for d in decisions), decisions
        shutil.copy2(report_path, evidence / 'cpython-provider.json')
        relocated = work / ('搬迁 空格 ' + args.mode)
        launch_path = Path(executable.name)
        if args.mode == 'onefile':
            relocated.mkdir()
            shutil.copy2(executable, relocated / executable.name)
        elif args.mode == 'app':
            relocated.mkdir()
            shutil.copytree(bundle, relocated / bundle.name)
            launch_path = executable.relative_to(bundle.parent)
        else:
            shutil.copytree(executable.parent, relocated)
        if args.mode != 'onefile':
            audit_native_paths(relocated)
        shutil.rmtree(inputs)
        shutil.rmtree(output)
        empty_path, cwd = work / 'empty-path', work / 'empty-cwd'
        empty_path.mkdir()
        cwd.mkdir()
        polluted = dict(runtime_env('product'), PATH=str(empty_path),
                        PYTHONHOME='/missing-python', PYTHONPATH='/missing-site')
        command = relocated_command(relocated / launch_path, cache, original_venv, evidence, cwd, polluted)
        actual = checked(execute([*command, profiles['product'], evidence / 'product'], cwd,
                                 polluted, evidence, 'product', timeout=150,
                                 sample_startup=args.sample_startup), '搬迁Cocoa原业务')
        assert normalize(actual.stdout, profiles['product']) == normalize(oracle.stdout, profiles['oracle'])
        assert normalize(actual.stderr, profiles['product']) == normalize(oracle.stderr, profiles['oracle'])
        result = {'mode': args.mode, 'oracle': 'pass', 'product': 'deleted_inputs_relocated_pass',
                'business_delegations': [], 'probe_entry': 'cil', 'display': 'cocoa',
                'stock_module_namespaces': args.stock_module_namespaces, 'python_payload_bytes': python_bytes,
                'startup_sampled': bool(args.sample_startup)}
        if full_probe:
            result['startup_milliseconds'] = {}
            for arm in ('oracle', 'product'):
                start = json.loads((evidence / (arm + '-exit.json')).read_text())['started_monotonic_ns']
                phases = [line.split('\t') for line in (evidence / arm / 'startup.tsv').read_text().splitlines()]
                result['startup_milliseconds'][arm] = {
                    label: round((int(stamp) - start) / 1_000_000, 1) for label, stamp in phases
                }
        return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--cache', type=Path, default=CACHE)
    parser.add_argument('--mode', choices=['jit', 'standalone', 'onefile', 'app'], default='jit')
    parser.add_argument('--oracle-only', action='store_true')
    parser.add_argument('--probe-entry', type=Path, default=HERE / 'explorerpro_probe.py', help='同次CP/CIL诊断入口，默认保持完整原业务合同')
    parser.add_argument('--native-components', type=Path)
    parser.add_argument('--stock-module-namespaces', action='store_true', help='显式全图Instance/provider权威globals合同')
    parser.add_argument('--sample-startup', type=int, default=0, choices=range(0, 61),
                        metavar='0..60', help='诊断臂启动时采样秒数；采样结果不得计入正式耗时')
    parser.add_argument('--evidence', type=Path, default=Path.home() / '.cache/ryupy/validation/explorerpro-products')
    args = parser.parse_args()
    evidence = args.evidence.expanduser().resolve() / datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')
    assert not evidence.is_relative_to(REPO) and not args.cache.expanduser().resolve().is_relative_to(REPO)
    evidence.mkdir(parents=True)
    print(evidence, flush=True)
    project = next(p for p in json.loads(CATALOG.read_text())['projects'] if p['id'] == 'explorerpro')
    (evidence / 'source.json').write_text(json.dumps(project, ensure_ascii=False, indent=2) + '\n')
    shutil.copy2(__file__, evidence / 'explorerpro_products.py')
    try:
        result = product(args, project, evidence)
        (evidence / 'results.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n')
        print(json.dumps(result, ensure_ascii=False), flush=True)
    except BaseException:
        (evidence / 'failure.txt').write_text(traceback.format_exc())
        raise
    finally:
        temporary = evidence / 'temporary.json'
        if temporary.exists():
            removed = not Path(json.loads(temporary.read_text())['work']).exists()
            (evidence / 'cleanup.json').write_text(json.dumps({'temporary_tree_removed': removed}))


if __name__ == '__main__':
    main()
