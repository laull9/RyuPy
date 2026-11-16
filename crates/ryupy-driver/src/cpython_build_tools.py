"""自动模式的私有构建工具：固定 packaging，与用户依赖分离，完整候选原子发布。"""
from contextlib import contextmanager
import os
from pathlib import Path
import subprocess
import sys
import tempfile
import time
import uuid


@contextmanager
def build_tool_lock(path):
    """仅同一工具缓存互斥；内核在构建器中断时释放占用。"""
    with path.open('a+b') as lock:
        if lock.seek(0, 2) == 0:
            lock.write(b'0')
            lock.flush()
        lock.seek(0)
        if os.name == 'nt':
            import msvcrt
            deadline = time.monotonic() + 360
            while True:
                try:
                    msvcrt.locking(lock.fileno(), msvcrt.LK_NBLCK, 1)
                    break
                except OSError:
                    if time.monotonic() >= deadline:
                        raise ValueError('等待私有构建工具缓存超时')
                    time.sleep(0.1)
        else:
            import fcntl
            fcntl.flock(lock.fileno(), fcntl.LOCK_EX)
        try:
            yield
        finally:
            lock.seek(0)
            if os.name == 'nt':
                msvcrt.locking(lock.fileno(), msvcrt.LK_UNLCK, 1)
            else:
                fcntl.flock(lock.fileno(), fcntl.LOCK_UN)


def build_tool_ready(root):
    """按实际安装元数据和必要模块判断齐备，不给普通工具文件添加摘要。"""
    try:
        metadata = (root / 'packaging-25.0.dist-info/METADATA').read_text(encoding='utf-8')
        return ('\nVersion: 25.0\n' in metadata and all(
            (root / 'packaging' / name).is_file()
            for name in ['__init__.py', 'requirements.py', 'markers.py', 'tags.py']))
    except OSError:
        return False


def prepare_build_tools():
    cache = Path.home() / '.cache/ryupy/build-tools'
    cache.mkdir(parents=True, exist_ok=True)
    target = cache / 'packaging-25.0'
    with build_tool_lock(cache / 'packaging-25.0.lock'):
        if build_tool_ready(target):
            return str(target)
        with tempfile.TemporaryDirectory(prefix='packaging-', dir=cache) as temporary:
            candidate = Path(temporary) / 'ready'
            # 仅使用已核验的基础发行安装构建工具；不改用户venv或应用需求。
            command = [sys.executable, '-I', '-X', 'utf8', '-m', 'pip', '--isolated',
                       '--disable-pip-version-check', 'install', '--only-binary=:all:',
                       '--no-deps', '--no-compile', '--target', str(candidate), 'packaging==25.0']
            result = subprocess.run(command, stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=300)
            if result.returncode or not build_tool_ready(candidate):
                raise ValueError('自动准备构建工具失败：' + result.stderr.decode('utf-8', errors='replace'))
            if target.exists():
                target.rename(cache / ('incomplete-packaging-' + uuid.uuid4().hex))
            candidate.rename(target)
    return str(target)
