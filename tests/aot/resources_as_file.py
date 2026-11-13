#!/usr/bin/env python3
"""as_file文件系统竖切：同源CP/JIT及真实NativeAOT发布，整树搬迁后删除源码/IL再换cwd执行。"""
import argparse
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tempfile


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--ryupy', required=True)
    parser.add_argument('--python', default=sys.executable)
    parser.add_argument('--out', type=Path, required=True)
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[2]
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='resources-as-file-', dir=args.out.resolve()))
    logs = work / 'logs'
    logs.mkdir()
    env = {**os.environ, 'PYTHONDONTWRITEBYTECODE': '1', 'PYTHONIOENCODING': 'utf-8'}
    ryupy = str(Path(args.ryupy).resolve())
    python = shutil.which(args.python) or str(Path(args.python).resolve())

    def run(label, command, cwd, timeout=60):
        result = subprocess.run(command, cwd=cwd, env=env, capture_output=True, timeout=timeout)
        (logs / (label + '.stdout')).write_bytes(result.stdout)
        (logs / (label + '.stderr')).write_bytes(result.stderr)
        (logs / (label + '.exit')).write_text(str(result.returncode) + '\n')
        return result

    def succeeded(result):
        assert result.returncode == 0, result.stderr.decode(errors='replace')

    def equal(reference, actual):
        assert (actual.returncode, actual.stdout, actual.stderr) == (
            reference.returncode, reference.stdout, reference.stderr), actual.stderr.decode(errors='replace')

    version = run('python-version', [python, '-B', '-c',
        "import sys; assert sys.implementation.name == 'cpython'; print('.'.join(map(str, sys.version_info[:3])))"], work)
    succeeded(version)
    assert version.stdout.decode().strip() == (root / 'tests/pinned-python.txt').read_text().strip()
    source = work / '源码 & input'
    shutil.copytree(root / 'tests/cases/resources_as_file/package', source)
    # 同一次原生发布覆盖全部Path/绑定协议，再执行注册包files→joinpath→as_file链路。
    shutil.copyfile(root / 'tests/cases/resources_as_file/path/entry.py', source / 'protocol.py')
    entry = source / 'main.py'
    entry.write_text("""# 同源协议与真实包资源入口，另有缺资源直接读取负例。
import sys
if len(sys.argv) > 1:
    import assetpkg
    from importlib.resources import files, as_file
    with as_file(files(assetpkg).joinpath('data.txt')) as path:
        path.read_text(encoding='utf-8')
else:
    import protocol
    import entry
""", encoding='utf-8')
    resources = source / 'resources.json'
    resources.write_text(json.dumps({'schema_version': 1, 'modules': [
        {'module': 'assetpkg', 'files': ['data.txt']}
    ]}), encoding='utf-8')
    outside = work / 'outside-build-cwd'
    outside.mkdir()
    expected = run('cpython', [python, '-B', str(entry)], outside)
    succeeded(expected)
    assert expected.stderr == b''
    jit = work / 'jit'
    compiled = run('jit-build', [ryupy, 'build', str(entry), '--out-dir', str(jit)], outside, 120)
    succeeded(compiled)
    actual = run('jit', ['dotnet', str(jit / 'main.dll')], outside)
    equal(expected, actual)
    output = work / 'publish'
    published = run('native-publish', [ryupy, 'build', str(entry), '--out-dir', str(output),
        '--aot', '--resources', str(resources)], outside, 600)
    succeeded(published)
    reported = [Path(line.removeprefix('已生成 ')) for line in published.stderr.decode().splitlines()
                if line.startswith('已生成 ')]
    native_name = 'main.exe' if os.name == 'nt' else 'main'
    natives = [p for p in reported if p.name == native_name]
    assert len(natives) == 1 and natives[0].parent == output / 'main.dist'
    original_dist = natives[0].parent
    bundle = work / '搬迁 & bundle'
    shutil.copytree(original_dist, bundle)
    # 删除本次拥有的原输入、托管产物和原dist，让搬迁执行无法回退源码或旧发布目录。
    for directory in (source, jit, output):
        shutil.rmtree(directory)
    assert not source.exists() and not output.exists() and not jit.exists()
    assert not list(bundle.rglob('*.py')) and not list(bundle.rglob('*.dll'))
    native_cwd = work / 'unrelated-native-cwd'
    native_cwd.mkdir()
    native = run('native-relocated', [str(bundle / native_name)], native_cwd)
    equal(expected, native)
    # 删除真实资源后的负例锁定真实IO失败，避免缓存数据或回退搜索路径。
    deployed = list((bundle / '_internal/resources/v1').rglob('data.txt'))
    assert len(deployed) == 1
    deployed[0].unlink()
    missing = run('native-missing-resource', [str(bundle / native_name), '--read-resource'], native_cwd)
    assert missing.returncode != 0 and b'FileNotFoundError' in missing.stderr
    result = {'status': 'passed', 'platform': platform.platform(), 'publish_count': 1,
        'python': version.stdout.decode().strip(), 'cpython_jit_native_equal': True,
        'source_and_managed_removed': True, 'relocated_cwd': str(native_cwd),
        'missing_resource_exit': missing.returncode,
        'scope': '已安装SDK的宿主机真实NativeAOT搬迁执行；无SDK目标环境仍待独立验收'}
    (work / 'result.json').write_text(json.dumps(result, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')
    print(work)
    print(json.dumps(result, ensure_ascii=False))


if __name__ == '__main__':
    main()
