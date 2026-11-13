"""离线验包验收共用原件记录；不跟随链接，不运行归档内容之外的隐式工具。"""
import hashlib
import json
import os
from pathlib import Path
import stat
import subprocess


def require(value, message):
    if not value:
        raise ValueError(message)


def save(path, value):
    path = Path(path); path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2)+'\n', encoding='utf-8')


def digest(path):
    with Path(path).open('rb') as stream:
        return hashlib.file_digest(stream, 'sha256').hexdigest()


def tree(root):
    """含根权限、目录和链接目标；不把atime等读取本身会变化的字段当内容。"""
    root = Path(root); rows = []
    def walk(path):
        meta = path.lstat(); relative = path.relative_to(root).as_posix()
        row = {'path': relative, 'mode': stat.S_IMODE(meta.st_mode),
               'windows_attributes': getattr(meta, 'st_file_attributes', None)}
        if stat.S_ISLNK(meta.st_mode):
            row.update(kind='link', target=os.readlink(path))
        elif stat.S_ISDIR(meta.st_mode):
            row['kind'] = 'directory'
            for child in sorted(path.iterdir()): walk(child)
        elif stat.S_ISREG(meta.st_mode):
            row.update(kind='file', bytes=meta.st_size, sha256=digest(path))
        else:
            raise ValueError('非预期文件类型：'+relative)
        rows.append(row)
    walk(root)
    return sorted(rows, key=lambda row: row['path'])


def capture(prefix, command, cwd, env, timeout=120):
    prefix = Path(prefix); prefix.parent.mkdir(parents=True, exist_ok=True)
    command = list(map(str, command)); row = {'argv': command, 'cwd': str(cwd), 'exit': None}
    try:
        actual = subprocess.run(command, cwd=cwd, env=env, capture_output=True, timeout=timeout)
        prefix.with_suffix('.stdout').write_bytes(actual.stdout)
        prefix.with_suffix('.stderr').write_bytes(actual.stderr)
        row['exit'] = actual.returncode
        return actual
    except subprocess.TimeoutExpired as error:
        prefix.with_suffix('.stdout').write_bytes(error.stdout or b'')
        prefix.with_suffix('.stderr').write_bytes(error.stderr or b'')
        row['timeout'] = timeout
        raise
    finally:
        save(prefix.with_suffix('.json'), row)


def sentinels(root, env):
    """绝对失败工具哨兵+空PATH；记录验证阶段没有尝试启动SDK/Python。"""
    root = Path(root); root.mkdir(); empty = root/'empty-path'; empty.mkdir()
    marker = root/'tool-was-started'
    suffix = '.cmd' if os.name == 'nt' else '.sh'
    tool = root/('forbidden-tool'+suffix)
    text = (f'@echo off\r\necho started>"{marker}"\r\nexit /b 93\r\n' if os.name == 'nt'
            else '#!/bin/sh\nprintf started > "'+str(marker)+'"\nexit 93\n')
    tool.write_bytes(text.encode('utf-8')); tool.chmod(0o755)
    clean = {k: v for k, v in env.items() if not k.upper().startswith('PYTHON')}
    clean.update(PATH=str(empty), DOTNET=str(tool), PYTHON=str(tool),
                 RYUPY_FOREACH_PYTHON=str(tool))
    save(root/'environment.json', {'PATH': str(empty), 'DOTNET': str(tool), 'PYTHON': str(tool),
                                  'scope': '空工具PATH与失败工具哨兵，不是系统调用沙箱或断网证明'})
    return clean, marker
