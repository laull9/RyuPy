"""只复制同次发布的可执行文件；不删用户目录、不把工具轻测称为原生运行。"""
import os
from pathlib import Path
import shutil
from manifest_files import digest, files_under, plain_stat


def prepare_single_file(binary, work, removed_inputs, expected_sha256):
    binary, work = Path(binary), Path(work)
    plain_stat(work, directory=True)
    plain_stat(binary)
    if not removed_inputs:
        raise AssertionError('必须明确原源码和IL目录已移除')
    for item in removed_inputs:
        path = Path(item)
        if path.resolve().parent != work.resolve() or os.path.lexists(path):
            raise AssertionError('源码/IL仍存在或不是本轮owned目录')
    size, source_hash = digest(binary)
    if source_hash != expected_sha256:
        raise AssertionError('完整部署中的native与同次清单SHA不一致')
    destination = work/'single-executable'
    # 不使用exist_ok，不复用任何旧目录；失败留下本轮诊断，不进行递归删除。
    destination.mkdir()
    copied = destination/binary.name
    shutil.copy2(binary, copied)
    record = verify_single_file(copied, expected_sha256)
    copied_size, copied_hash = record['bytes'], record['sha256']
    final_size, final_hash = digest(binary)
    if (copied_size, copied_hash) != (size, source_hash) or (final_size, final_hash) != (size, source_hash):
        raise AssertionError('仅exe复制前后字节或源文件发生变化')
    return copied, {**record, 'owned_source_and_il_absent':True}


def verify_single_file(binary, expected_sha256):
    binary = Path(binary)
    entries = files_under(binary.parent)
    immediate = list(binary.parent.iterdir())
    if set(entries) != {binary.name} or len(immediate) != 1 or immediate[0].name != binary.name:
        raise AssertionError('仅exe部署目录不是恰一个普通文件')
    size, actual_hash = digest(binary)
    if actual_hash != expected_sha256:
        raise AssertionError('仅exe字节不匹配同次发布清单')
    return {'sha256':actual_hash,'bytes':size,'files':[binary.name]}
