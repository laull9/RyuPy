"""只读普通文件清单核对；不跟随链接，不宣称恶意并发目录替换安全。"""
import hashlib
import json
import os
from pathlib import Path
import re
import stat


class ManifestError(ValueError):
    pass


def relative_path(value):
    if (not isinstance(value, str) or not value or '\\' in value or ':' in value
            or any(ord(c) < 32 or 0x7f <= ord(c) <= 0x9f for c in value) or value.startswith('/')
            or any(part in ('', '.', '..') for part in value.split('/'))):
        raise ManifestError(f'非规范相对路径：{value!r}')
    return value


def plain_stat(path, *, directory=False):
    info = path.lstat()
    if stat.S_ISLNK(info.st_mode) or getattr(info, 'st_file_attributes', 0) & 0x400:
        raise ManifestError(f'拒绝符号链接/reparse：{path.name}')
    predicate = stat.S_ISDIR if directory else stat.S_ISREG
    if not predicate(info.st_mode):
        raise ManifestError(f'非普通{"目录" if directory else "文件"}：{path.name}')
    return info


def files_under(root):
    root = Path(root).absolute()
    plain_stat(root, directory=True)
    result = {}
    def visit(directory, prefix):
        with os.scandir(directory) as entries:
            for entry in entries:
                path = Path(entry.path)
                relative = relative_path(prefix + entry.name)
                info = entry.stat(follow_symlinks=False)
                if stat.S_ISLNK(info.st_mode) or getattr(info, 'st_file_attributes', 0) & 0x400:
                    raise ManifestError(f'拒绝符号链接/reparse：{relative}')
                if stat.S_ISDIR(info.st_mode):
                    visit(path, relative + '/')
                elif stat.S_ISREG(info.st_mode):
                    result[relative] = path
                else:
                    raise ManifestError(f'拒绝非普通发布项：{relative}')
    visit(root, '')
    return result


def stat_snapshot(info):
    fields = ('st_dev', 'st_ino', 'st_size', 'st_mtime_ns', 'st_ctime_ns',
              'st_birthtime_ns', 'st_mode', 'st_file_attributes')
    return {key: getattr(info, key, None) for key in fields}


def check_read_snapshots(before, opened, after_read, after, size, *, windows):
    """Windows 3.12 path ctime 是创建时刻，fd ctime 是 ChangeTime，不能跨 API 比。"""
    snapshots = tuple(stat_snapshot(info) for info in (before, opened, after_read, after))
    cross = ('st_dev', 'st_ino', 'st_size', 'st_mtime_ns')
    if windows:
        cross += ('st_birthtime_ns',)
    else:
        cross += ('st_ctime_ns',)
    stable = all(all(item[key] == snapshots[0][key] for key in cross)
                 for item in snapshots[1:])
    if windows:
        stable = (stable and all(item['st_birthtime_ns'] is not None for item in snapshots)
                  and snapshots[0]['st_ctime_ns'] == snapshots[3]['st_ctime_ns']
                  and snapshots[1]['st_ctime_ns'] == snapshots[2]['st_ctime_ns'])
    if not stable or size != before.st_size:
        names = ('before_path', 'opened_fd', 'after_read_fd', 'after_path')
        detail = dict(zip(names, snapshots))
        detail['bytes_read'] = size
        raise ManifestError('读取期间发布文件变化：' + json.dumps(detail, sort_keys=True))


def digest(path):
    before = plain_stat(path)
    size, hashed = 0, hashlib.sha256()
    with path.open('rb') as stream:
        opened = os.fstat(stream.fileno())
        while block := stream.read(1024 * 1024):
            size += len(block)
            hashed.update(block)
        after_read = os.fstat(stream.fileno())
    after = plain_stat(path)
    try:
        check_read_snapshots(before, opened, after_read, after, size, windows=os.name == 'nt')
    except ManifestError as error:
        raise ManifestError(f'{path.name}: {error}') from error
    return size, hashed.hexdigest()


def check_artifacts(root, entries, manifest_name, *, windows=False):
    actual = files_under(root)
    if manifest_name not in actual:
        raise ManifestError('缺少发布清单普通文件')
    expected = {}
    folded = set()
    for entry in entries:
        path = relative_path(entry['relative_path'])
        key = path.casefold() if windows else path
        reserved = {manifest_name, '.' + manifest_name + '.tmp'}
        if key in ({name.casefold() for name in reserved} if windows else reserved) or key in folded:
            raise ManifestError(f'自hash或重复发布路径：{path}')
        folded.add(key)
        if type(entry['size']) is not int or entry['size'] < 0 or not isinstance(entry['sha256'], str) or not re.fullmatch('[0-9a-f]{64}', entry['sha256']):
            raise ManifestError(f'非法文件大小/摘要：{path}')
        expected[path] = entry
    wanted = set(expected) | {manifest_name}
    if set(actual) != wanted:
        raise ManifestError(f'发布普通文件集合不同：missing={sorted(wanted-set(actual))}, extra={sorted(set(actual)-wanted)}')
    for path, entry in expected.items():
        size, hashed = digest(actual[path])
        if (size, hashed) != (entry['size'], entry['sha256']):
            raise ManifestError(f'发布文件字节不符：{path}')
    return actual
