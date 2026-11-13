"""独立Linux普通文件分类consumer门：固定官方包、默认工具构建与公开项目，不测性能。"""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tarfile
import urllib.request

HERE=Path(__file__).resolve().parent
REPO=HERE.parents[2]
sys.path.insert(0,str(HERE.parent/'standalone_app/benchmark'))
from measure import capture, require, save, sha
from prepare import prepare


def main():
    parser=argparse.ArgumentParser();parser.add_argument('phase',choices=('setup','verify'));args=parser.parse_args()
    require(os.environ.get('GITHUB_ACTIONS')=='true' and os.environ.get('RUNNER_ENVIRONMENT')=='github-hosted','仅hosted')
    head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=REPO,text=True).strip()
    require(head==os.environ['GITHUB_SHA'],'候选不符')
    base=Path(os.environ['RUNNER_TEMP'])/'deployment-binaryornot';base.mkdir(exist_ok=True)
    python=Path(os.environ['PYTHON']).resolve();dotnet=Path(os.environ['DOTNET']).resolve()
    env=dict(os.environ,PYTHONDONTWRITEBYTECODE='1',PYTHONOPTIMIZE='0',PYTHONNOUSERSITE='1')
    def command(label,argv,timeout):
        rec=capture(base/label,argv,REPO,env,timeout)
        require(rec['exit']==0 and rec['failure'] is None,label+'失败')
    if args.phase=='setup':
        save(base/'source.json',{'commit':head,'tree':subprocess.check_output(['git','rev-parse','HEAD^{tree}'],cwd=REPO,text=True).strip(),
             'cli_build':'cargo build --locked --release -p ryupy-cli','policy':'default, no feature flags'})
        packages=json.loads((HERE/'inputs.json').read_text())['packages'];sources={}
        for name,row in packages.items():
            archive=base/row['archive']['filename']
            with urllib.request.urlopen(row['archive']['url'],timeout=120) as response:archive.write_bytes(response.read())
            require(sha(archive)==row['archive']['sha256'],'官方归档hash不符')
            store=base/(name+'-download');store.mkdir()
            with tarfile.open(archive) as tar:tar.extractall(store,filter='data')
            sources[name]=store/Path(row['source_root']).name
        save(base/'downloads.json',packages)
        prepare(base/'project',sources)
        command('cp',[python,'-B',HERE/'check_cp.py','--project',base/'project','--python',python,'--output',base/'cp'],120)
        command('runtime-build',[dotnet,'build','runtime/RyuPy.Runtime/RyuPy.Runtime.csproj','-c','Release','-nologo'],300)
        command('cli-build',['cargo','build','--locked','--release','-p','ryupy-cli'],900)
        tools=base/'tools';tools.mkdir();shutil.copy2(REPO/'target/release/ryupy',tools/'ryupy')
        shutil.copy2(REPO/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll',tools/'RyuPy.Runtime.dll')
        save(base/'frozen-tools.json',{str(p):sha(p) for p in (tools/'ryupy',tools/'RyuPy.Runtime.dll',python,dotnet)})
        command('dependencies',[python,'-B',HERE/'check_dependencies.py','--cli',tools/'ryupy',
            '--runtime',tools/'RyuPy.Runtime.dll','--project',base/'project','--output',base/'dependency-evidence'],600)
        docker=HERE.parent/'standalone_app/benchmark'
        command('image-build',['docker','build','--iidfile',base/'runtime-image.id','-f',docker/'runtime.Dockerfile',docker],600)
        image=(base/'runtime-image.id').read_text().strip()
        command('image-inspect',['docker','image','inspect',image],60)
        command('image-packages',['docker','run','--rm','--network','none',image,'dpkg-query','-W'],60)
    else:
        frozen=json.loads((base/'frozen-tools.json').read_text());require(all(sha(p)==h for p,h in frozen.items()),'工具变化')
        command('native',[python,'-B',HERE/'runner.py','--cli',base/'tools/ryupy','--runtime',base/'tools/RyuPy.Runtime.dll',
            '--project',base/'project','--output',base/'evidence','--python',python,'--dotnet',dotnet,
            '--runtime-image',(base/'runtime-image.id').read_text().strip()],2400)
        require(all(sha(p)==h for p,h in frozen.items()),'验收改变工具')


if __name__=='__main__':main()
