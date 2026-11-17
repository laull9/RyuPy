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
import re
import shutil
import signal
import subprocess
import traceback

from inspect_sources import CACHE, CATALOG, REPO, analyze
import tempfile

HERE = Path(__file__).resolve().parent


def execute(command, cwd, env, evidence, label, timeout=900):
    """保存双流与退出码；超时只回收本次独立进程组，包含上游容量查询子进程。"""
    command = list(map(str, command))
    (evidence / (label + '-command.json')).write_text(json.dumps(command, ensure_ascii=False) + '\n')
    stdout_path, stderr_path = evidence / (label + '-stdout.log'), evidence / (label + '-stderr.log')
    # 实际窗口与原生崩溃诊断需要实时双流，不能等全部业务结束才落盘。
    with stdout_path.open('wb') as stdout_log, stderr_path.open('wb') as stderr_log:
        process = subprocess.Popen(command, cwd=cwd, env=env, stdout=stdout_log,
                                   stderr=stderr_log, start_new_session=True)
        try:
            process.wait(timeout=timeout)
        except BaseException:
            os.killpg(process.pid, signal.SIGTERM)
            try:
                process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                os.killpg(process.pid, signal.SIGKILL)
                process.wait(timeout=5)
            raise
    stdout, stderr = stdout_path.read_bytes(), stderr_path.read_bytes()
    (evidence / (label + '-exit.json')).write_text(json.dumps({'exit': process.returncode}))
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


def product(args, project, evidence):
    source = args.cache.expanduser().resolve() / project['repo'].replace('/', '--')
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
        extra = []
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
            extra = ['--cpython-native-components', native_declaration]
        built = execute([args.ryupy.resolve(), entry, '--mode', args.mode, '-o', output,
                         '-I', tree / 'src', '-I', tree, *extra], work, dict(env, VIRTUAL_ENV=str(venv)),
                        evidence, 'build')
        checked(built, 'CIL probe项目构建')
        generated = [Path(line[len('已生成 '):]) for line in built.stderr.decode().splitlines()
                     if line.startswith('已生成 ')]
        assert len(generated) == 1, built.stderr
        executable = generated[0]
        reports = list(output.rglob('cpython-provider.json'))
        report_path = next((p for p in reports if p.parent.name.endswith('.dist')), reports[0])
        report = json.loads(report_path.read_text())
        assert report['delegated_local_modules'] == [], report['delegated_local_modules']
        assert report['entry_provider'] == 'ryupy_compiled_source', report['entry_provider']
        shutil.copy2(report_path, evidence / 'cpython-provider.json')
        relocated = work / ('搬迁 空格 ' + args.mode)
        if args.mode == 'onefile':
            relocated.mkdir()
            shutil.copy2(executable, relocated / executable.name)
        else:
            shutil.copytree(executable.parent, relocated)
        dotnet = shutil.which('dotnet')
        shutil.rmtree(inputs)
        shutil.rmtree(output)
        empty_path, cwd = work / 'empty-path', work / 'empty-cwd'
        empty_path.mkdir()
        cwd.mkdir()
        polluted = dict(runtime_env('product'), PATH=str(empty_path),
                        PYTHONHOME='/missing-python', PYTHONPATH='/missing-site')
        command = [dotnet, relocated / executable.name] if args.mode == 'jit' else [relocated / executable.name]
        actual = checked(execute([*command, profiles['product'], evidence / 'product'], cwd,
                                 polluted, evidence, 'product', timeout=150), '搬迁Cocoa原业务')
        assert normalize(actual.stdout, profiles['product']) == normalize(oracle.stdout, profiles['oracle'])
        assert normalize(actual.stderr, profiles['product']) == normalize(oracle.stderr, profiles['oracle'])
        return {'mode': args.mode, 'oracle': 'pass', 'product': 'deleted_inputs_relocated_pass',
                'business_delegations': [], 'probe_entry': 'cil', 'display': 'cocoa'}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--cache', type=Path, default=CACHE)
    parser.add_argument('--mode', choices=['jit', 'standalone', 'onefile'], default='jit')
    parser.add_argument('--oracle-only', action='store_true')
    parser.add_argument('--probe-entry', type=Path, default=HERE / 'explorerpro_probe.py', help='同次CP/CIL诊断入口，默认保持完整原业务合同')
    parser.add_argument('--native-components', type=Path)
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
