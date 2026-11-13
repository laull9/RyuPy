"""固定CP四外部文件分类；明确为普通consumer而不是上游CLI。"""
import argparse
import json
import os
from pathlib import Path
import subprocess
import shutil
from prepare import HERE,require,sha

def check(project,python,output):
    project=Path(project).resolve();output=Path(output).resolve();output.mkdir(parents=True,exist_ok=False)
    contract=json.loads((HERE/'contract.json').read_text())
    version=subprocess.check_output([str(python),'--version'],text=True).strip()
    require(version=='Python '+contract['cp_version'],'CP版本不同')
    env=dict(os.environ,PYTHONPATH=str(project/'vendor'),PYTHONDONTWRITEBYTECODE='1',PYTHONOPTIMIZE='0',PYTHONNOUSERSITE='1')
    rows=[]
    for case in contract['cases']:
        data=output/case['input'];shutil.copyfile(HERE/case['input'],data)
        argv=[str(python),'-B','-S',str(project/'scanner.py'),str(data)]
        run=subprocess.run(argv,cwd=output,env=env,capture_output=True,timeout=60)
        (output/(case['id']+'.stdout')).write_bytes(run.stdout);(output/(case['id']+'.stderr')).write_bytes(run.stderr)
        rows.append({'case':case['id'],'argv':argv,'exit':run.returncode})
        (output/'runs.json').write_text(json.dumps(rows,indent=2))
        require(run.returncode==0 and run.stderr==b'' and run.stdout==case['stdout'].encode(),'完整CP分类原流不符')
    (output/'result.json').write_text(json.dumps({'passed':True,'version':version,'python_sha256':sha(python),'runs':rows},indent=2)+'\n')
if __name__=='__main__':
    parser=argparse.ArgumentParser()
    for name in ('project','python','output'):parser.add_argument('--'+name,type=Path,required=True)
    args=parser.parse_args();check(args.project,args.python,args.output)
