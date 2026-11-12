"""锁定binaryornot/chardet的普通consumer项目；无includes/resources，不更改上游。"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil
HERE=Path(__file__).resolve().parent
REPO=HERE.parents[2]
def sha(path):return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def require(value,message):
    if not value:raise ValueError(message)
def configuration():
    return {'schema_version':1,'mode':'aot','search_paths':['vendor'],
            'distribution_metadata':'distribution.json','targets':{'scan':{'entry':'scanner.py'}}}
def metadata_request():
    return {'schema_version':1,'mode':'closed_snapshot','roots':[{'id':'vendor','path':'vendor'}]}
def prepare(output,sources):
    output=Path(output).resolve();require(not output.exists() and not output.is_relative_to(REPO),'项目须为仓库外新目录')
    contract=json.loads((HERE/'contract.json').read_text())
    for name,digest in contract['files'].items():require(sha(HERE/name)==digest,'合同改变:'+name)
    packages=json.loads((HERE/'inputs.json').read_text())['packages']
    for name,row in packages.items():
        root=Path(sources[name]).resolve()
        for relative,digest in row['files'].items():
            source=root/relative
            require(source.is_file() and not source.is_symlink() and sha(source)==digest,'锁定上游不符:'+relative)
    output.mkdir(parents=True)
    for name,row in packages.items():
        for relative in row['files']:
            target=output/'vendor'/relative;target.parent.mkdir(parents=True,exist_ok=True)
            require(not target.exists(),'vendor来源重叠');shutil.copyfile(Path(sources[name])/relative,target)
    shutil.copyfile(HERE/'scanner.py',output/'scanner.py')
    for name,value in [('ryupy.json',configuration()),('distribution.json',metadata_request())]:
        (output/name).write_text(json.dumps(value,indent=2)+'\n')
    return output
if __name__=='__main__':
    parser=argparse.ArgumentParser();parser.add_argument('output',type=Path)
    for name in ('binaryornot','chardet'):parser.add_argument('--'+name,type=Path,required=True)
    args=parser.parse_args();prepare(args.output,{name:getattr(args,name) for name in ('binaryornot','chardet')})
