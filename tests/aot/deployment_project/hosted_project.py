"""独立Linux项目门：复用固定工具与运行镜像，不安装Nuitka、不执行旧性能链。"""
import argparse
import json
import os
from pathlib import Path
import subprocess
import sys
import tarfile
import urllib.request

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
sys.path.insert(0,str(HERE.parent/'standalone_app/benchmark'))
from measure import capture, require, save, sha
from prepare import prepare


def main():
    parser=argparse.ArgumentParser();parser.add_argument('phase',choices=('setup','verify'));args=parser.parse_args()
    require(os.environ.get('GITHUB_ACTIONS')=='true' and os.environ.get('RUNNER_ENVIRONMENT')=='github-hosted','仅hosted')
    require(subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()==os.environ['GITHUB_SHA'],'候选身份不同')
    base=Path(os.environ['RUNNER_TEMP'])/'deployment-project';base.mkdir(exist_ok=True)
    reused=Path(os.environ['RUNNER_TEMP'])/'standalone-app-benchmark'
    python=Path(os.environ['PYTHON']).resolve();dotnet=Path(os.environ['DOTNET']).resolve()
    def command(label,argv,timeout):
        record=capture(base/label,argv,ROOT,dict(os.environ),timeout)
        require(record['exit']==0 and record['failure'] is None,label+'失败')
    if args.phase=='setup':
        recipe=json.loads((ROOT/'tests/aot/wcwidth_ci/project-source-recovery.json').read_text())
        item=next(x for x in recipe['packages'] if x['package']=='pyfiglet')
        archive=base/item['filename']
        with urllib.request.urlopen(item['url'],timeout=120) as response:archive.write_bytes(response.read())
        require(sha(archive)==item['sha256'],'原包hash不符');save(base/'download.json',item)
        store=ROOT/'third_party/projects'/item['store_directory'];store.mkdir(parents=True,exist_ok=False)
        with tarfile.open(archive) as source:source.extractall(store,filter='data')
        # 仅复用工具/正常CP/镜像准备；不调用该benchmark的oracles或benchmark阶段。
        command('tools',[python,'-B',HERE.parent/'standalone_app/benchmark/hosted.py','tools'],1500)
        prepare(base/'project',reused/'prepared/package')
        save(base/'project-tools.json',json.loads((reused/'frozen-tools.json').read_text()))
    else:
        tools=json.loads((base/'project-tools.json').read_text());require(all(sha(p)==h for p,h in tools.items()),'工具变更')
        image=(reused/'runtime-image.id').read_text().strip()
        require(image.startswith('sha256:') and len(image)==71,'必须固定镜像ID')
        command('no-python-dotnet',['docker','run','--rm','--network','none','--read-only',image,'sh','-c',
            'for p in python python3 dotnet; do if command -v "$p" >/dev/null 2>&1; then exit 9; fi; done'],60)
        command('public-project',[python,'-B',HERE/'public_build.py','--cli',reused/'tools/ryupy',
            '--runtime',reused/'tools/RyuPy.Runtime.dll','--project',base/'project','--output',base/'evidence',
            '--python',python,'--dotnet',dotnet,'--runtime-image',image],3300)
        require(all(sha(p)==h for p,h in tools.items()),'公开验证改变工具')


if __name__=='__main__':main()
