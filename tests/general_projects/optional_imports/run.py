#!/usr/bin/env python3
"""可选依赖真实 CLI 门：当次 oracle、保留已装闭包、必需负例及 JIT/AOT 删除输入搬迁。"""
import argparse
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT))
from tests.cpython.dependencies.run import wheel


def invoke(command, cwd, env, log, success=True):
    result = subprocess.run(list(map(str, command)), cwd=cwd, env=env,
                            capture_output=True, timeout=600)
    log.write_bytes(result.stdout + result.stderr)
    if success and result.returncode:
        raise AssertionError((command, result.returncode, result.stderr.decode()))
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', type=Path, required=True)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--mode', choices=['jit', 'standalone'], action='append')
    parser.add_argument('--evidence', type=Path, default=Path.home() / '.cache/ryupy/validation/optional-imports')
    args = parser.parse_args()
    evidence = args.evidence.expanduser().resolve() / datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')
    assert not evidence.is_relative_to(ROOT), '证据必须放仓库外'
    evidence.mkdir(parents=True)
    print(evidence, flush=True)
    env = {k: v for k, v in os.environ.items()
           if not k.startswith(('PYTHON', 'RYUPY_', 'LD_', 'DYLD_', 'VIRTUAL_ENV', 'CONDA'))}
    env.update(PYTHONDONTWRITEBYTECODE='1', PYTHONHASHSEED='0', TZ='UTC')
    rows = []
    with tempfile.TemporaryDirectory(prefix='ryupy-optional-imports-') as temporary:
        work = Path(temporary).resolve()
        assert not work.is_relative_to(ROOT)
        inputs = work / '构建 输入'
        inputs.mkdir()
        venv = inputs / 'venv'
        invoke(['uv', 'venv', '--python', args.python, venv], work, env, evidence / 'venv.log')
        python = venv / ('Scripts/python.exe' if sys.platform == 'win32' else 'bin/python')
        version = invoke([python, '-I', '-c', 'import platform; print(platform.python_version())'],
                         work, env, evidence / 'version.log')
        assert version.stdout.strip() == b'3.12.13', version.stdout
        leaf = wheel(inputs, 'optional_leaf', [], {'optional_leaf.py': 'VALUE = "已装传递依赖"\n'})
        feature = wheel(inputs, 'optional_feature', ['optional_leaf==1.0'], {
            'optional_feature.py': 'from pathlib import Path\nfrom optional_leaf import VALUE\nPath("导入副作用.txt").write_text(VALUE)\n'})
        invoke(['uv', 'pip', 'install', '--python', python, '--no-deps', leaf, feature],
               work, env, evidence / 'install.log')
        build_env = dict(env, VIRTUAL_ENV=str(venv))
        products = []
        dotnet = shutil.which('dotnet')
        for case, module in [('absent', 'ryupy_absent_optional'), ('present', 'optional_feature')]:
            entry = inputs / (case + '.py')
            entry.write_text('"""手写可选依赖产品合同；不执行安装或修改业务输入。"""\n'
                             'try:\n    from ' + module + ' import VALUE\n'
                             'except ImportError:\n    VALUE = "原异常回退"\nprint(VALUE)\n')
            oracle_cwd = work / ('oracle-' + case)
            oracle_cwd.mkdir()
            oracle = invoke([python, '-B', entry], oracle_cwd, env, evidence / (case + '-oracle.log'))
            for mode in args.mode or ['jit', 'standalone']:
                label = case + '-' + mode
                output = work / ('build-' + label)
                built = invoke([args.ryupy.resolve(), entry, '--mode', mode, '-o', output,
                                '--cpython-entry'], work, build_env, evidence / (label + '-build.log'))
                assert not (work / '导入副作用.txt').exists(), '构建执行了应用导入'
                generated = [Path(line[len('已生成 '):]) for line in built.stderr.decode().splitlines()
                             if line.startswith('已生成 ')]
                assert len(generated) == 1, built.stderr
                executable = generated[0]
                report = json.loads((executable.parent / 'cpython-provider.json').read_text())
                collection = report['dependency_collection']
                audit = next(row for row in collection['optional_imports'] if row['module'] == module)
                assert audit['installed'] == (case == 'present'), audit
                names = {row['name'] for row in collection['distributions']}
                assert names == ({'optional-feature', 'optional-leaf'} if case == 'present' else set()), names
                shutil.copy2(executable.parent / 'cpython-provider.json', evidence / (label + '-provider.json'))
                relocated = work / ('搬迁 空格 ' + label)
                shutil.move(executable.parent, relocated)
                products.append((label, mode, relocated / executable.name, oracle, oracle_cwd))
                if output.exists():
                    shutil.rmtree(output)
        # 同根名任一必需出现或显式声明均覆盖可选分类，构建必须报可操作缺包错误。
        negative = inputs / 'required.py'
        optional = 'try:\n    import ryupy_absent_optional\nexcept ImportError:\n    pass\n'
        for label, text, extra in [
            ('required', optional + 'import ryupy_absent_optional\n', []),
            ('dynamic', optional, ['--cpython-dynamic-module', 'ryupy_absent_optional']),
        ]:
            negative.write_text(text)
            failed = invoke([args.ryupy.resolve(), negative, '--mode', 'standalone',
                             '-o', work / ('negative-' + label), '--cpython-entry', *extra],
                            work, build_env, evidence / (label + '-negative.log'), success=False)
            assert failed.returncode and b'ryupy_absent_optional' in failed.stderr, failed.stderr
            rows.append({'case': label, 'result': 'missing_required_rejected'})
        # 已存在而没有发行登记的模块不能按“缺席可选包”漏掉，否则会改变原环境功能。
        site = venv / ('Lib/site-packages' if sys.platform == 'win32' else 'lib/python3.12/site-packages')
        (site / 'ryupy_unowned_optional.py').write_text('VALUE = "已安装未登记"\n')
        negative.write_text(optional.replace('ryupy_absent_optional', 'ryupy_unowned_optional'))
        failed = invoke([args.ryupy.resolve(), negative, '--mode', 'standalone', '-o', work / 'unowned',
                         '--cpython-entry'], work, build_env, evidence / 'unowned-negative.log', success=False)
        assert failed.returncode and b'ryupy_unowned_optional' in failed.stderr, failed.stderr
        rows.append({'case': 'unowned', 'result': 'installed_without_metadata_rejected'})
        shutil.rmtree(inputs)
        empty_path = work / 'empty-path'
        empty_path.mkdir()
        polluted = dict(env, PATH=str(empty_path), PYTHONHOME='/missing-python', PYTHONPATH='/missing-site')
        for label, mode, executable, oracle, oracle_cwd in products:
            cwd = work / ('run-' + label)
            cwd.mkdir()
            command = [dotnet, executable] if mode == 'jit' else [executable]
            actual = invoke(command, cwd, polluted, evidence / (label + '-run.log'))
            assert (actual.returncode, actual.stdout, actual.stderr) == (oracle.returncode, oracle.stdout, oracle.stderr)
            if label.startswith('present'):
                assert (cwd / '导入副作用.txt').read_bytes() == (oracle_cwd / '导入副作用.txt').read_bytes()
            rows.append({'case': label, 'result': 'deleted_inputs_relocated_pass',
                         'execution': 'explicit_cpython_entry'})
            print(label, '删除输入搬迁通过', flush=True)
    (evidence / 'results.json').write_text(json.dumps(rows, ensure_ascii=False, indent=2) + '\n')
    print('必需导入/显式动态依赖负例通过；临时输入和产品已清理', flush=True)


if __name__ == '__main__':
    main()
