"""裸return通用Native及真实pyfiglet空格/段落验收；不含性能计时结论。"""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tarfile
import urllib.request

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
sys.path[:0]=[str(HERE.parent/'deployment_project'),str(HERE.parent/'standalone_app/benchmark'),str(HERE.parent)]
from measure import capture, positive, require, save, sha, elf, inventory
from prepare import prepare
from public_build import project_command, inputs
from audit_native import audit
from aot_layout import reported_aot
from manifest_files import check_artifacts


def cases():
    # 尾空格属于原失败输入，不strip、不改列宽、不换成无空格负载。
    return {'hi_space':['-f','standard','hi hi'],
            'hello_world':['-f','standard','hello world'],
            'paragraph':['-f','standard','The quick brown fox jumps over the lazy dog. '*20]}


def snapshot():
    names=subprocess.check_output(['git','ls-files','-z'],cwd=ROOT).decode().split('\0')
    return {name:sha(ROOT/name) for name in names if name and (ROOT/name).is_file()}


def main():
    require(os.environ.get('GITHUB_ACTIONS')=='true' and os.environ.get('RUNNER_ENVIRONMENT')=='github-hosted','仅hosted原生验收')
    head=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
    require(head==os.environ['GITHUB_SHA'],'不是本轮提交')
    out=Path(os.environ['RUNNER_TEMP'])/'bare-return-none';out.mkdir(exist_ok=False)
    python=Path(os.environ['PYTHON']).resolve();dotnet=Path(os.environ['DOTNET']).resolve()
    env=dict(os.environ,PYTHONDONTWRITEBYTECODE='1',PYTHONOPTIMIZE='0',PYTHONNOUSERSITE='1',
             DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1',MSBUILDDISABLENODEREUSE='1',UseSharedCompilation='false')
    before=snapshot();save(out/'source-before.json',before)
    result={'passed':False,'candidate':head,'rid':'linux-x64','performance':'not-measured',
            'clean_machine':False,'application_runs':{},'generic_runs':{}}
    def command(label,argv,cwd=ROOT,extra=None,timeout=900):
        record=capture(out/label,argv,cwd,dict(env,**(extra or {})),timeout)
        require(record['exit']==0 and record['failure'] is None,label+'失败')
        return record
    try:
        command('dotnet-info',[dotnet,'--info']);command('rustc-version',['rustc','--version'])
        require(subprocess.check_output([dotnet,'--version'],env=env,text=True).strip()=='10.0.403','SDK漂移')
        require(subprocess.check_output([python,'-c','import platform;print(platform.python_version())'],env=env,text=True).strip()=='3.12.13','CP漂移')
        command('runtime-build',[dotnet,'build','runtime/RyuPy.Runtime/RyuPy.Runtime.csproj','-c','Release','-nologo'])
        command('cli-build',['cargo','build','--locked','--release','-p','ryupy-cli'])
        tools=out/'tools';tools.mkdir();cli=tools/'ryupy';runtime=tools/'RyuPy.Runtime.dll'
        shutil.copy2(ROOT/'target/release/ryupy',cli)
        shutil.copy2(ROOT/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll',runtime)
        frozen={str(p):sha(p) for p in [cli,runtime,python,dotnet]};save(out/'tools.json',frozen)
        recipe=json.loads((ROOT/'tests/aot/wcwidth_ci/project-source-recovery.json').read_text())
        item=next(x for x in recipe['packages'] if x['package']=='pyfiglet')
        archive=out/item['filename']
        with urllib.request.urlopen(item['url'],timeout=120) as response:archive.write_bytes(response.read())
        require(sha(archive)==item['sha256'],'包下载hash不符');save(out/'download.json',item)
        unpacked=out/'upstream';unpacked.mkdir()
        with tarfile.open(archive) as bundle:bundle.extractall(unpacked,filter='data')
        project=prepare(out/'project',next(unpacked.iterdir()))
        project_inputs=inputs(project);save(out/'project-inputs.json',project_inputs)
        cwd=out/'unrelated-cwd';cwd.mkdir();output=out/'application-build'
        command('public-build',project_command(cli,project,'cli',str(output)),cwd,
                {'RYUPY_RUNTIME_DIR':str(runtime.parent)},1200)
        layout=reported_aot((out/'public-build.stderr').read_bytes(),output,'__main__','linux-x64');elf(layout.native)
        receipt=audit(layout,project,'cli',out/'application-audit',cli,runtime,python,dotnet,env)
        require(receipt['resource_count']==553,'资源数不是553');result['independent_receipt']=True
        goldens={}
        for name,argv in cases().items():
            prefix=out/(name+'-cp')
            command(name+'-cp',[python,'-B',project/'package/pyfiglet/__main__.py',*argv],cwd,
                    {'PYTHONPATH':str(project/'package')},60)
            require(prefix.with_suffix('.stderr').read_bytes()==b'','CP stderr非空')
            goldens[name]=prefix.with_suffix('.stdout').read_bytes()
        require(inputs(project)==project_inputs,'构建改变项目输入')
        project.rename(out/'project-hidden')
        native_env={'PATH':'/usr/bin:/bin','LC_ALL':'C.UTF-8','TZ':'UTC'}
        application_inventory=inventory(layout.dist)
        moved=out/'relocated/application';moved.parent.mkdir();shutil.copytree(layout.dist,moved)
        for place,dist in [('original',layout.dist),('relocated',moved)]:
            for name,argv in cases().items():
                prefix=out/(place+'-'+name)
                record=capture(prefix,[dist/layout.native.name,*argv],cwd,native_env,60)
                positive(record,prefix,goldens[name]);result['application_runs'][place+'-'+name]=True
            require(inventory(dist)==application_inventory,'执行/搬迁改变产物')
        # 通用最小程序独立公开构建，避免真实应用成功只是包特判。
        generic=out/'generic-source';generic.mkdir();source=generic/'generic.py'
        shutil.copy2(HERE/'generic.py',source);command('generic-cp',[python,'-B',source],cwd,timeout=60)
        expected=(out/'generic-cp.stdout').read_bytes()
        require(expected==b'True\nTrue\nvalue\nvalue\n' and (out/'generic-cp.stderr').read_bytes()==b'','通用CP合同不符')
        generic_output=out/'generic-build'
        command('generic-public-build',[cli,'build',source,'--aot','-o',generic_output],cwd,
                {'RYUPY_RUNTIME_DIR':str(runtime.parent)},1200)
        simple=reported_aot((out/'generic-public-build.stderr').read_bytes(),generic_output,'generic','linux-x64');elf(simple.native)
        manifest=json.loads(simple.manifest.read_text());check_artifacts(simple.dist,manifest['artifacts'],'ryupy-artifact.json')
        generic.rename(out/'generic-source-hidden');simple_snapshot=inventory(simple.dist)
        simple_moved=out/'relocated/generic';shutil.copytree(simple.dist,simple_moved)
        for place,dist in [('original',simple.dist),('relocated',simple_moved)]:
            prefix=out/(place+'-generic');record=capture(prefix,[dist/simple.native.name],cwd,native_env,60)
            positive(record,prefix,expected);require(inventory(dist)==simple_snapshot,'通用产物改变')
            result['generic_runs'][place]=True
        require(all(sha(p)==h for p,h in frozen.items()),'工具改变')
        after=snapshot();save(out/'source-after.json',after);require(before==after,'源码改变')
        result['passed']=True
    finally:
        save(out/'result.json',result)


if __name__=='__main__':main()
