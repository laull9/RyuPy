"""先保留有界原始文本摘要，再流式归档分块；失败仍可上传摘要且CI保持红色。"""
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


class Parts:
    def __init__(self,destination,limit,max_parts):
        self.destination=destination;self.limit=limit;self.max_parts=max_parts
        self.parts=[];self.stream=None;self.current=0;self.total=0;self.hash=hashlib.sha256()
    def write(self,data):
        length=len(data);self.hash.update(data);self.total+=length
        while data:
            if self.stream is None:
                assert len(self.parts)<self.max_parts, 'too many parts; raw evidence remains untouched'
                name=f'part-{len(self.parts)+1:02}.bin'
                self.parts.append(dict(name=name));self.stream=(self.destination/name).open('xb');self.current=0
            size=min(self.limit-self.current,len(data));self.stream.write(data[:size]);self.current+=size;data=data[size:]
            if self.current==self.limit:self.close_part()
        return length
    def close_part(self):
        if self.stream:
            self.stream.close();self.stream=None
            item=self.parts[-1];path=self.destination/item['name'];item.update(bytes=path.stat().st_size,sha256=digest(path))
    def flush(self):
        if self.stream:self.stream.flush()


def package(source,destination,limit=LIMIT,max_parts=12,summary_limit=LIMIT):
    source=source.resolve();destination=destination.resolve()
    assert source.is_dir() and not destination.exists() and not destination.is_relative_to(source)
    destination.mkdir(parents=True);summary=destination/'summary';summary.mkdir()
    status=dict(ok=False,stage='summary',text_files=[],omitted=[])
    writer=None
    try:
        total=0
        for path in sorted(source.rglob('*')):
            if path.is_file() and path.suffix.lower() in ('.json','.log','.txt','.stdout','.stderr','.trx','.yml'):
                assert not path.is_symlink()
                relative=path.relative_to(source);size=path.stat().st_size
                if total+size>summary_limit-1024*1024:
                    status['omitted'].append(str(relative));continue
                target=summary/'raw'/relative;target.parent.mkdir(parents=True,exist_ok=True);shutil.copyfile(path,target)
                status['text_files'].append(dict(path=str(relative),bytes=size,sha256=digest(path)));total+=size
        assert not status['omitted'], 'text summary exceeds bound; partial original text preserved, not complete evidence'
        status['stage']='archive'
        # 流式gzip直写分块，避免raw+完整tar+parts三份同时占用磁盘。
        writer=Parts(destination,limit,max_parts)
        with tarfile.open(fileobj=writer,mode='w|gz') as tar:tar.add(source,arcname='evidence',recursive=True)
        writer.close_part()
        metadata=dict(schema=1,archive_sha256=writer.hash.hexdigest(),archive_bytes=writer.total,parts=writer.parts,text_files=status['text_files'],
                      reconstruction='Concatenate ordered part-NN.bin; verify archive sha256; extract evidence.tar.gz. Text summary is not binary verification.')
        (summary/'download-parts.json').write_text(json.dumps(metadata,indent=2))
        status.update(ok=True,stage='complete',part_count=len(writer.parts))
        return len(writer.parts)
    except Exception as error:
        status['error']=f'{type(error).__name__}: {error}'
        raise
    finally:
        if writer:writer.close_part()
        (summary/'package-status.json').write_text(json.dumps(status,indent=2))


if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('source',type=Path);p.add_argument('destination',type=Path);p.add_argument('--github-output',type=Path)
    a=p.parse_args();n=package(a.source,a.destination)
    if a.github_output:
        with a.github_output.open('a') as output:output.write(f'count={n}\n')
    print(f'Complete evidence preserved in {n} bounded chunks; summary retains original logs/samples/manifests')
