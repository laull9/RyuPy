"""CPython provider 验收共用版本、环境、进程及分发声明合同。"""
import json
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[2]
VERSION = '3.12.13'
NUMPY_VERSION = '2.3.5'


def execute(command, *, cwd, env, timeout=240):
    """全部进程有界执行，保留退出码和完整输出参与当次比较。"""
    return subprocess.run(list(map(str, command)), cwd=cwd, env=env,
                          capture_output=True, timeout=timeout)


def checked(result, label):
    assert result.returncode == 0, (label, result.returncode,
                                  result.stdout[:1600].decode(errors='replace'),
                                  result.stderr[:5000].decode(errors='replace'))
    return result


def environment():
    """消除Python/加载器开发环境污染，固定可控locale与时区。"""
    env = {key: value for key, value in os.environ.items()
           if not key.startswith(('PYTHON', 'RYUPY_', 'LD_', 'DYLD_'))}
    env.update(PYTHONDONTWRITEBYTECODE='1', PYTHONHASHSEED='0', TZ='UTC',
               LC_ALL='C.UTF-8', DOTNET_CLI_UI_LANGUAGE='en-US',
               NO_PROXY='127.0.0.1,localhost', no_proxy='127.0.0.1,localhost')
    return env


def require_python(python, cwd, env):
    result = checked(execute([python, '-I', '-c',
                             'import platform; print(platform.python_version())'],
                            cwd=cwd, env=env), '固定 CPython')
    assert result.stdout.decode().strip() == VERSION, result.stdout


def external_workspace(prefix, parent=None):
    """拒绝将日志、数据库和分发产物放回源码仓库。"""
    directory = Path(parent or tempfile.gettempdir()).resolve()
    assert not directory.is_relative_to(ROOT), '验收输出必须位于仓库外'
    directory.mkdir(parents=True, exist_ok=True)
    return Path(tempfile.mkdtemp(prefix=prefix, dir=directory))


def rid():
    system = {'darwin': 'osx', 'linux': 'linux', 'win32': 'win'}[sys.platform]
    machine = platform.machine().lower()
    assert machine in ('arm64', 'aarch64', 'x86_64', 'amd64'), machine
    return system + ('-arm64' if machine in ('arm64', 'aarch64') else '-x64')


def declaration(distribution, payloads, modules, container_transfer='synchronized'):
    """开发机根显式提供；上游发行和许可证随声明进入产品清单。"""
    return {'schema': 1, 'python_version': VERSION,
            'distribution_root': str(Path(distribution).resolve()),
            'modules': modules, 'payloads': payloads, 'container_transfer': container_transfer,
            'source': {'project': 'python-build-standalone', 'build_id': '20260623',
                       'source_url': 'https://github.com/astral-sh/python-build-standalone/releases/tag/20260623',
                       'licenses': ['lib/python3.12/LICENSE.txt']}}


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n', encoding='utf-8')


def private_distribution(source, workspace):
    """私有发行输入可在搬迁时隐藏，避免修改其他验证进程共用的uv发行。"""
    target = workspace / 'distribution-input'
    shutil.copytree(source, target, ignore=shutil.ignore_patterns('__pycache__', '*.pyc'))
    return target


def audit_native_paths(dist):
    """macOS逐Mach-O审计加载边，禁止依赖源发行/venv的绝对路径。"""
    if sys.platform != 'darwin':
        return
    magic = {b'\xcf\xfa\xed\xfe', b'\xfe\xed\xfa\xcf', b'\xca\xfe\xba\xbe'}
    for path in dist.rglob('*'):
        if not path.is_file():
            continue
        with path.open('rb') as stream:
            if stream.read(4) not in magic:
                continue
        result = subprocess.run(['/usr/bin/otool', '-L', path], capture_output=True, check=True)
        for index, line in enumerate(result.stdout.decode().splitlines()[1:]):
            dependency = line.strip().split(' (compatibility', 1)[0]
            # otool对dylib的第一行展示自身ID；它不是加载其他映像的边。
            if index == 0 and path.suffix == '.dylib' and dependency == '@rpath/' + path.name:
                continue
            if dependency.startswith(('/usr/lib/', '/System/Library/')):
                continue
            if dependency.startswith('@loader_path/'):
                resolved = (path.parent / dependency[len('@loader_path/'):]).resolve()
                assert resolved.is_relative_to(dist.resolve()) and resolved.exists(), (path, dependency)
            elif dependency.startswith('@rpath/'):
                raise AssertionError(('尚未解析的rpath加载边', path, dependency))
            else:
                raise AssertionError(('分发之外的原生加载边', path, dependency))


def inspect_host_report(path, dist, *, closed=True):
    """宿主报告来自真实加载和关闭状态；任何包文件均须位于分发根。"""
    report = json.loads(path.read_text())
    if closed:
        assert report['state'] == 'closed', report
        assert report['handles'] == report['managed_roots'] == report['pending_releases'] == report['active_leases'] == 0, report
    assert not report['synchronization_failure'], report
    for field in ('python_library', 'shim_library'):
        assert Path(report[field]).resolve().is_relative_to(dist.resolve()), report[field]
    for name, filename in report['module_paths'].items():
        if filename and name != '__main__':
            assert Path(filename).resolve().is_relative_to(dist.resolve()), (name, filename)
    return report
