"""真实官方console入口的CP整流合同；输入文件为外部用户数据，不是部署资源。"""
import argparse
import json
import os
from pathlib import Path
import subprocess
from prepare import HERE, require, sha

def check(project, python, output):
    project=Path(project).resolve(); output=Path(output).resolve(); output.mkdir(parents=True,exist_ok=False)
    contract=json.loads((HERE/'contract.json').read_text())
    version=subprocess.check_output([str(python),'--version'],text=True).strip()
    require(version=='Python '+contract['cp_version'],'CP版本不同')
    env=dict(os.environ,PYTHONPATH=os.pathsep.join(str(project/'vendor'/p) for p in ('table','width')),
             PYTHONDONTWRITEBYTECODE='1',PYTHONOPTIMIZE='0',PYTHONNOUSERSITE='1')
    rows=[]
    for case in contract['cases']:
        data=output/case['input'];data.write_bytes((HERE/case['input']).read_bytes())
        result_file=output/(case['id']+'.result')
        args=[*case['argv']]
        if 'output' in case:args=['-o',str(result_file),*args]
        cmd=[str(python),'-B','-S',str(project/'tools/tabulate_console.py'),*args,str(data)]
        run=subprocess.run(cmd,cwd=output,env=env,capture_output=True,timeout=60)
        (output/(case['id']+'.stdout')).write_bytes(run.stdout)
        (output/(case['id']+'.stderr')).write_bytes(run.stderr)
        expected=(HERE/case['stdout']).read_bytes() if 'stdout' in case else b''
        # 文件文本换行按真实宿主约定，而非把Linux原流冒作跨平台黄金。
        expected=expected.replace(b'\n',os.linesep.encode())
        require(run.returncode==0 and run.stderr==b'' and run.stdout==expected,'完整CP原流不同')
        if 'output' in case:require(result_file.read_bytes()==(HERE/case['output']).read_bytes().replace(b'\n',os.linesep.encode()),'完整CP文件不同')
        rows.append({'case':case['id'],'argv':cmd,'exit':run.returncode})
    (output/'result.json').write_text(json.dumps({'passed':True,'version':version,'python_sha256':sha(python),'runs':rows},indent=2)+'\n')

def main():
    parser=argparse.ArgumentParser()
    for name in ('project','python','output'):parser.add_argument('--'+name,type=Path,required=True)
    args=parser.parse_args();check(args.project,args.python,args.output)
if __name__=='__main__':main()
