"""自动依赖闭包真实运行：固定Python当次oracle、JIT/AOT、隐藏输入搬迁及负例。"""
import argparse
import csv
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import zipfile

ROOT = Path(__file__).resolve().parents[3]


def run(command, cwd, env, success=True):
    result = subprocess.run(list(map(str, command)), cwd=cwd, env=env,
                            capture_output=True, timeout=600)
    if success and result.returncode:
        raise AssertionError((command, result.stdout.decode(), result.stderr.decode()))
    return result


def wheel(work, name, requirements, files):
    """手写wheel输入；RECORD仅保存文件名，不生成普通文件摘要。"""
    info = name + '-1.0.dist-info'
    files = dict(files)
    files[info + '/METADATA'] = ('Metadata-Version: 2.3\nName: ' + name + '\nVersion: 1.0\n'
                                      'License: MIT\nProvides-Extra: feature\n'
                                      + ''.join('Requires-Dist: ' + req + '\n' for req in requirements))
    files[info + '/WHEEL'] = 'Wheel-Version: 1.0\nGenerator: ryupy-test\nRoot-Is-Purelib: true\nTag: py3-none-any\n'
    files[info + '/licenses/LICENSE'] = 'MIT fixture license\n'
    record = io.StringIO()
    writer = csv.writer(record)
    for file in list(files) + [info + '/RECORD']:
        writer.writerow([file, '', ''])
    files[info + '/RECORD'] = record.getvalue()
    path = work / (name + '-1.0-py3-none-any.whl')
    with zipfile.ZipFile(path, 'w') as archive:
        for file, content in files.items():
            archive.writestr(file, content)
    return path


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--python', required=True)
    parser.add_argument('--distribution', required=True)
    parser.add_argument('--metadata', required=True)
    parser.add_argument('--numpy-root', required=True)
    parser.add_argument('--ryupy', default=str(ROOT / 'target/debug/ryupy'))
    parser.add_argument('--aot', action='store_true')
    args = parser.parse_args()
    env = {k: v for k, v in os.environ.items() if not k.startswith(('PYTHON', 'DYLD_', 'LD_', 'RYUPY_'))}
    env.update(PYTHONDONTWRITEBYTECODE='1', TZ='UTC', LC_ALL='C.UTF-8')
    with tempfile.TemporaryDirectory(prefix='ryupy 自动依赖 ') as temp:
        work = Path(temp)
        inputs = work / '构建 输入'
        inputs.mkdir()
        distribution = inputs / 'python'
        shutil.copytree(args.distribution, distribution, symlinks=False)
        for name in ['PYTHON.json', 'licenses']:
            source = Path(args.metadata) / name
            if source.is_dir():
                shutil.copytree(source, distribution / name)
            else:
                shutil.copy2(source, distribution / name)
        base_site = distribution / 'lib/python3.12/site-packages'
        base_site.mkdir(exist_ok=True)
        (base_site / 'unselected_development.py').write_text('raise RuntimeError("不应打包")\n')
        venv = inputs / '虚拟 环境'
        run(['uv', 'venv', '--python', args.python, venv], work, env)
        python = venv / 'bin/python'
        run(['uv', 'pip', 'install', '--python', python, 'packaging==25.0'], work, env)
        site = venv / 'lib/python3.12/site-packages'
        for source in Path(args.numpy_root).iterdir():
            if source.is_dir():
                shutil.copytree(source, site / source.name)
            else:
                shutil.copy2(source, site / source.name)
        gamma = wheel(inputs, 'closure_gamma', [], {'closure_gamma.py': 'VALUE = "传递"\n'})
        beta = wheel(inputs, 'closure_beta', ['closure_gamma==1.0; extra == "feature"'], {
            'closure_beta/__init__.py': 'from importlib.resources import files\nfrom closure_gamma import VALUE\ndef read():\n    return VALUE + files(__package__).joinpath("数据.txt").read_text()\n',
            'closure_beta/数据.txt': '资源'})
        plugin = wheel(inputs, 'closure_plugin', [], {'closure_plugin.py': 'VALUE = "插件"\n'})
        alpha = wheel(inputs, 'closure_alpha', ['closure_beta[feature]==1.0', 'numpy==2.3.5', 'never_installed==1; python_version < "3.0"'], {
            'closure_alpha/__init__.py': 'import importlib\nfrom importlib.metadata import version\nfrom pathlib import Path\nimport closure_beta\nimport numpy\ndef run():\n    text = closure_beta.read() + importlib.import_module("closure_plugin").VALUE + version("closure_beta") + str(int(numpy.arange(4).sum()))\n    Path("业务 副作用.txt").write_text(text)\n    return text\n'})
        run(['uv', 'pip', 'install', '--python', python, '--no-deps', alpha, beta, gamma, plugin], work, env)
        entry = inputs / '项目 入口.py'
        entry.write_text((Path(__file__).parent / 'app.py').read_text())
        config = {'schema': 1, 'python_version': '3.12.13', 'distribution_root': str(distribution),
                  'source': {'project': 'python-build-standalone', 'build_id': '20260623',
                             'source_url': 'https://github.com/astral-sh/python-build-standalone/releases/tag/20260623',
                             'licenses': ['lib/python3.12/LICENSE.txt'] + [str(p.relative_to(distribution)) for p in sorted((distribution / 'licenses').glob('*')) if p.is_file()]},
                  'container_transfer': 'synchronized', 'dependencies': {'environment': str(venv), 'dynamic_modules': ['closure_plugin']}}
        declaration = inputs / 'provider.json'
        declaration.write_text(json.dumps(config))
        oracle_cwd = work / 'oracle'
        oracle_cwd.mkdir()
        assert run([python, '-I', '-c', 'import sys; print(sys.version.split()[0])'], work, env).stdout.strip() == b'3.12.13'
        oracle = run([python, '-I', '-X', 'utf8', entry], oracle_cwd, env)
        # 先确认缺少传递依赖得到构建期可操作错误，之后恢复同一输入。
        hidden = site / 'closure_gamma-1.0.dist-info'
        hidden.rename(inputs / '隐藏 metadata')
        bad = run([args.ryupy, '--cpython-provider', declaration, 'build', entry, '--out-dir', work / 'bad'], work, env, False)
        assert bad.returncode and b'closure_gamma' in bad.stderr, bad.stderr
        (inputs / '隐藏 metadata').rename(hidden)
        def reject(path, text, expected):
            original = path.read_bytes()
            try:
                path.write_text(text)
                result = run([args.ryupy, '--cpython-provider', declaration, 'build', entry,
                              '--out-dir', work / 'bad'], work, env, False)
                assert result.returncode and expected.encode() in result.stderr, result.stderr
            finally:
                path.write_bytes(original)
        gamma_metadata = site / 'closure_gamma-1.0.dist-info/METADATA'
        reject(gamma_metadata, gamma_metadata.read_text().replace('Version: 1.0', 'Version: 9.0'), '依赖版本冲突')
        gamma_record = site / 'closure_gamma-1.0.dist-info/RECORD'
        reject(gamma_record, gamma_record.read_text().rstrip() + '\n../escape.py,,\n', 'RECORD路径越界')
        reject(gamma_record, gamma_record.read_text().rstrip() + '\nmissing_payload.py,,\n', '缺少文件')
        gamma_wheel = site / 'closure_gamma-1.0.dist-info/WHEEL'
        reject(gamma_wheel, gamma_wheel.read_text().replace('py3-none-any', 'cp311-cp311-win_amd64'), 'wheel ABI/平台不匹配')
        # 产品静态收集器必须沿包内相对导入发现底层第三方依赖。
        relative_package = inputs / 'relative_application'
        relative_package.mkdir()
        (relative_package / '__init__.py').write_text('')
        (relative_package / 'entry.py').write_text('from . import helper\n')
        (relative_package / 'helper.py').write_text('import closure_plugin\n')
        request = {'environment': str(venv), 'entry': str(relative_package / 'entry.py'),
                   'requirements': [], 'dynamic_modules': [], 'stdlib': [], 'provided_modules': []}
        result = subprocess.run([distribution / 'bin/python3.12', '-I', '-S', '-B',
                                 ROOT / 'crates/ryupy-driver/src/cpython_dependencies.py'],
                                input=json.dumps(request).encode(), capture_output=True, cwd=work, env=env, timeout=60)
        assert result.returncode == 0, result.stderr
        assert {item['name'] for item in json.loads(result.stdout)['report']['distributions']} == {'closure-plugin'}
        # 未执行site初始化：即使环境中有启动钩子，静态工具也不能执行它。
        hook_marker = work / '禁止启动钩子.txt'
        (site / 'unowned.pth').write_text('import pathlib; pathlib.Path(' + repr(str(hook_marker)) + ').write_text("错误")\n')
        products = []
        for aot in ([False, True] if args.aot else [False]):
            output = work / ('aot' if aot else 'jit')
            command = [args.ryupy, '--cpython-provider', declaration, 'build', entry, '--out-dir', output]
            if aot:
                command.append('--aot')
            run(command, work, env)
            configs = [p for p in output.rglob('ryupy-cpython.json') if not aot or p.parent.name.endswith('.dist')]
            assert len(configs) == 1, configs
            product = configs[0].parent
            report = json.loads((product / 'cpython-provider.json').read_text())
            names = {d['name'] for d in report['dependency_collection']['distributions']}
            assert names == {'closure-alpha', 'closure-beta', 'closure-gamma', 'closure-plugin', 'numpy'}, names
            assert not (product / 'python/lib/python3.12/site-packages/packaging').exists()
            assert 'unselected_development' not in report['modules']
            assert not (product / 'python/lib/python3.12/site-packages/unselected_development.py').exists()
            destination = work / ('搬迁 AOT' if aot else '搬迁 JIT')
            shutil.move(product, destination)
            products.append((aot, destination))
            if output.exists():
                shutil.rmtree(output)
        assert not hook_marker.exists(), '构建执行了site启动钩子'
        # 搬迁前移走项目、venv、wheel、基础发行输入，执行臂不能读取构建环境。
        inputs.rename(work / '不可访问 输入')
        shutil.rmtree(work / '不可访问 输入')
        for aot, destination in products:
            cwd = work / ('run-aot' if aot else 'run-jit')
            cwd.mkdir()
            polluted = dict(env, PYTHONHOME='/不可访问 Python', PYTHONPATH='/不可访问 包')
            command = [destination / '项目 入口'] if aot else [shutil.which('dotnet'), destination / '项目 入口.dll']
            actual = run(command, cwd, polluted)
            assert (actual.returncode, actual.stdout, actual.stderr) == (oracle.returncode, oracle.stdout, oracle.stderr), (actual.stdout, actual.stderr)
            assert (cwd / '业务 副作用.txt').read_bytes() == (oracle_cwd / '业务 副作用.txt').read_bytes()
            # 运行真实应用验证缺资源和缺原生扩展均能失败，随后恢复载荷。
            package_root = destination / 'python/lib/python3.12/site-packages'
            resource = package_root / 'closure_beta/数据.txt'
            native = next((package_root / 'numpy/_core').glob('_multiarray_umath*.so'))
            for missing, expected in [(resource, b'FileNotFoundError'), (native, b'numpy')]:
                saved = missing.read_bytes()
                try:
                    missing.unlink()
                    failed = run(command, cwd, polluted, False)
                    assert failed.returncode and expected in failed.stderr, failed.stderr
                finally:
                    missing.write_bytes(saved)
            print(('NativeAOT' if aot else 'JIT') + '：真实依赖闭包、资源/metadata/NumPy、输入删除搬迁与副作用通过')
        print('缺少依赖、版本冲突、RECORD越界、wheel ABI、缺资源/原生扩展负例通过')


if __name__ == '__main__':
    main()
