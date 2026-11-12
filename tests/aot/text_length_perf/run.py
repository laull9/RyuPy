"""同宿主真实pyfiglet原生进程A/B；源码、独立receipt和逐次原流硬门，不代表稳态或全库速度。"""
import json
import os
from pathlib import Path
import shutil
import statistics
import subprocess
import sys
import tarfile
import urllib.request

HERE=Path(__file__).resolve().parent
ROOT=HERE.parents[2]
BASE='f1765bb40964ce6b251f40d291f0da95bcf44fde'
sys.path[:0]=[str(HERE.parent/'deployment_project'),str(HERE.parent/'standalone_app/benchmark'),str(HERE.parent)]
from measure import capture, positive, require, save, sha, elf, inventory
from prepare import prepare
from public_build import project_command, inputs
from audit_native import audit
from aot_layout import reported_aot


def distribution(values):
    require(len(values)>=2,'缺少重复样本')
    ordered=sorted(values)
    def percentile(p):
        index=(len(ordered)-1)*p; lo=int(index)
        return ordered[lo]+(ordered[min(lo+1,len(ordered)-1)]-ordered[lo])*(index-lo)
    return {'n':len(values),'min_ms':ordered[0],'p25_ms':percentile(.25),
            'median_ms':statistics.median(values),'p75_ms':percentile(.75),
            'p95_ms':percentile(.95),'max_ms':ordered[-1]}


def snapshot(repo):
    names=subprocess.check_output(['git','ls-files','-z'],cwd=repo).decode().split('\0')
    return {name:sha(repo/name) for name in names if name and (repo/name).is_file()}


def main():
    require(os.environ.get('GITHUB_ACTIONS')=='true' and os.environ.get('RUNNER_ENVIRONMENT')=='github-hosted','仅同hosted runner')
    before=ROOT/'before-source'
    require(subprocess.check_output(['git','rev-parse','HEAD'],cwd=before,text=True).strip()==BASE,'基线不是已验资源候选')
    current=subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
    require(current==os.environ['GITHUB_SHA'],'候选身份不符')
    out=Path(os.environ['RUNNER_TEMP'])/'text-length-perf';out.mkdir(exist_ok=False)
    python=Path(os.environ['PYTHON']).resolve();dotnet=Path(os.environ['DOTNET']).resolve()
    env=dict(os.environ,PYTHONDONTWRITEBYTECODE='1',PYTHONOPTIMIZE='0',PYTHONNOUSERSITE='1',
             DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1',MSBUILDDISABLENODEREUSE='1',UseSharedCompilation='false')
    source={label:snapshot(repo) for label,repo in [('before',before),('after',ROOT)]}
    save(out/'source-before.json',source)
    result={'passed':False,'before':BASE,'after':current,'rid':'linux-x64','samples_per_arm':40,
            'warmups_per_arm':4,'scope':'fresh native processes on warm filesystem cache; includes startup and rendering',
            'nuitka_comparison':'not-remeasured','steady_state':'not-measured','workloads':{}}
    def command(label,argv,cwd=ROOT,extra=None,timeout=900):
        record=capture(out/label,argv,cwd,dict(env,**(extra or {})),timeout)
        require(record['exit']==0 and record['failure'] is None,label+'失败')
        return record
    try:
        command('dotnet-info',[dotnet,'--info']);command('rustc-version',['rustc','--version'])
        require(subprocess.check_output([dotnet,'--version'],env=env,text=True).strip()=='10.0.403','SDK漂移')
        require(subprocess.check_output([python,'-c','import platform;print(platform.python_version())'],env=env,text=True).strip()=='3.12.13','CP漂移')
        command('cli-build',['cargo','build','--locked','--release','-p','ryupy-cli'])
        tools=out/'tools';tools.mkdir();cli=tools/'ryupy';shutil.copy2(ROOT/'target/release/ryupy',cli)
        runtimes={}
        for label,repo in [('before',before),('after',ROOT)]:
            command(label+'-runtime-build',[dotnet,'build',repo/'runtime/RyuPy.Runtime/RyuPy.Runtime.csproj','-c','Release','-nologo'])
            folder=tools/label;folder.mkdir();runtime=folder/'RyuPy.Runtime.dll'
            shutil.copy2(repo/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll',runtime);runtimes[label]=runtime
        frozen={str(p):sha(p) for p in [cli,python,dotnet,*runtimes.values()]};save(out/'tools.json',frozen)
        require(sha(runtimes['before'])!=sha(runtimes['after']),'runtime候选没有变化')
        recipe=json.loads((ROOT/'tests/aot/wcwidth_ci/project-source-recovery.json').read_text())
        item=next(x for x in recipe['packages'] if x['package']=='pyfiglet')
        archive=out/item['filename']
        with urllib.request.urlopen(item['url'],timeout=120) as response:archive.write_bytes(response.read())
        require(sha(archive)==item['sha256'],'包下载hash不符');save(out/'download.json',item)
        unpacked=out/'upstream';unpacked.mkdir()
        with tarfile.open(archive) as bundle:bundle.extractall(unpacked,filter='data')
        package=next(unpacked.iterdir());project=prepare(out/'project',package)
        project_inputs=inputs(project);save(out/'project-inputs.json',project_inputs)
        cwd=out/'unrelated-cwd';cwd.mkdir();bundles={}
        for label,runtime in runtimes.items():
            output=out/(label+'-build')
            command(label+'-public-build',project_command(cli,project,'cli',str(output)),cwd,
                    {'RYUPY_RUNTIME_DIR':str(runtime.parent)},1200)
            layout=reported_aot((out/(label+'-public-build.stderr')).read_bytes(),output,'__main__','linux-x64');elf(layout.native)
            audit(layout,project,'cli',out/(label+'-audit'),cli,runtime,python,dotnet,env)
            dist=out/(label+'-dist');shutil.copytree(layout.dist,dist);bundles[label]=dist/layout.native.name
        require(inputs(project)==project_inputs,'构建改变项目输入')
        snapshots={label:inventory(path.parent) for label,path in bundles.items()}
        workloads={'hi':['-f','standard','hi'],'longhi':['-f','standard','-w','20000','hi'*100]}
        golden={}
        for name,argv in workloads.items():
            prefix=out/(name+'-cp')
            command(name+'-cp',[python,'-B',project/'package/pyfiglet/__main__.py',*argv],cwd,
                    {'PYTHONPATH':str(project/'package')},60)
            require(prefix.with_suffix('.stderr').read_bytes()==b'','CP stderr非空')
            golden[name]=prefix.with_suffix('.stdout').read_bytes()
        hidden=out/'project-hidden';project.rename(hidden)
        # 计时段不运行编译器、oracle、hash扫描或CP；每次都核验完整输出，固定交替消除单向顺序偏差。
        runenv={'PATH':'/usr/bin:/bin','LC_ALL':'C.UTF-8','TZ':'UTC'}
        for name,argv in workloads.items():
            values={'before':[],'after':[]};order=[]
            for index in range(44):
                labels=('before','after') if index%2==0 else ('after','before')
                for label in labels:
                    prefix=out/'samples'/f'{name}-{index:02d}-{label}'
                    record=capture(prefix,[bundles[label],*argv],cwd,runenv,60)
                    positive(record,prefix,golden[name])
                    if index>=4: values[label].append(record['wall_ms']);order.append(label)
            result['workloads'][name]={'argv':argv,'golden_sha256':__import__('hashlib').sha256(golden[name]).hexdigest(),
                'samples_ms':values,'order':order,'distributions':{k:distribution(v) for k,v in values.items()},
                'median_before_over_after':statistics.median(values['before'])/statistics.median(values['after'])}
            save(out/'result.json',result)
        require(all(inventory(path.parent)==snapshots[label] for label,path in bundles.items()),'测量改变产物')
        hidden.rename(project)
        require(inputs(project)==project_inputs,'运行改变项目输入')
        require(all(sha(p)==h for p,h in frozen.items()),'工具改变')
        after={label:snapshot(repo) for label,repo in [('before',before),('after',ROOT)]}
        save(out/'source-after.json',after);require(source==after,'源码改变')
        result['passed']=True
    finally:
        save(out/'result.json',result)


if __name__=='__main__':main()
