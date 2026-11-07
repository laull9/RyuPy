"""原生运行前保存有界完整dist；拒绝链接、超量和不稳定文件，失败不能冒充已归档。"""
import hashlib
import json
from pathlib import Path
import tarfile

MAX_FILES = 128
MAX_BYTES = 256 * 1024 * 1024


def snapshot(dist):
    files = {}
    total = 0
    for path in sorted(dist.rglob('*')):
        if path.is_symlink():
            raise RuntimeError(f'dist不接受符号链接：{path.name}')
        if path.is_dir():
            continue
        if not path.is_file():
            raise RuntimeError(f'dist不接受特殊文件：{path.name}')
        total += path.stat().st_size
        if len(files) >= MAX_FILES or total > MAX_BYTES:
            raise RuntimeError('dist超过128文件/256MiB证据上限')
        files[path.relative_to(dist).as_posix()] = hashlib.sha256(path.read_bytes()).hexdigest()
    if not files:
        raise RuntimeError('dist为空')
    return files, total


def preserve_dist(dist, output):
    """完整原物先落盘，之后才能执行ELF工具及目标；manifest包含在同一归档中。"""
    files, total = snapshot(dist)
    archive_path = output / 'native-dist.tar.gz'
    with tarfile.open(archive_path, 'w:gz') as archive:
        archive.add(dist, arcname=dist.name)
    after, after_total = snapshot(dist)
    if after != files or after_total != total:
        raise RuntimeError('归档期间dist身份变化')
    record = {'files': files, 'file_count': len(files), 'total_bytes': total,
              'max_files': MAX_FILES, 'max_bytes': MAX_BYTES,
              'archive_sha256': hashlib.sha256(archive_path.read_bytes()).hexdigest(),
              'phase': 'before-native-execution'}
    (output / 'dist-identity.json').write_text(json.dumps(record, indent=2) + '\n')
    return files
