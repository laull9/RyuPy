"""保持完整证据字节；按连接器单工件上限分块，另提供原始文本摘要供先审。"""
import argparse
import hashlib
import json
import shutil
import tarfile
from pathlib import Path

LIMIT=400*1024*1024


def digest(path):
    h=hashlib.sha256()
    with path.open('rb') as stream:
        for data in iter(lambda:stream.read(1024*1024),b''):h.update(data)
    return h.hexdigest()


def package(source,destination,limit=LIMIT):
    source=source.resolve();destination=destination.resolve()
    assert source.is_dir() and not destination.exists() and not destination.is_relative_to(source)
    destination.mkdir(parents=True);summary=destination/'summary';summary.mkdir()
    archive=destination/'evidence.tar.gz'
    with tarfile.open(archive,'w:gz') as tar:
        tar.add(source,arcname='evidence',recursive=True)
    parts=[]
    with archive.open('rb') as stream:
        while data:=stream.read(limit):
            name=f'part-{len(parts)+1:02}.bin';path=destination/name;path.write_bytes(data)
            parts.append(dict(name=name,bytes=len(data),sha256=digest(path)))
    assert 0<len(parts)<=12, 'too many parts; preserve raw archive and fail'
    texts=[]
    for path in sorted(source.rglob('*')):
        if path.is_file() and path.suffix.lower() in ('.json','.log','.txt','.stdout','.stderr','.trx','.yml'):
            assert not path.is_symlink()
            relative=path.relative_to(source);target=summary/'raw'/relative
            target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(path,target)
            texts.append(dict(path=str(relative),bytes=path.stat().st_size,sha256=digest(path)))
    assert sum(x['bytes'] for x in texts)<LIMIT, 'text summary exceeds connector bound'
    metadata=dict(schema=1,archive_sha256=digest(archive),archive_bytes=archive.stat().st_size,parts=parts,text_files=texts,
                  reconstruction='Concatenate ordered part-NN.bin bytes; verify archive sha256; extract evidence.tar.gz. Text summary is not binary verification.')
    (summary/'download-parts.json').write_text(json.dumps(metadata,indent=2))
    # 保留归档与原件；上传成功并独立验证前不删除。
    return len(parts)


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('destination',type=Path);p.add_argument('--github-output',type=Path)
    a=p.parse_args();n=package(a.source,a.destination)
    if a.github_output:
        with a.github_output.open('a') as output:output.write(f'count={n}\n')
    print(f'Complete evidence preserved in {n} bounded chunks; summary retains original logs/samples/manifests')
