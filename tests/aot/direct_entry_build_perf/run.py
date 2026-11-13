"""同机热包缓存的完整公开AOT构建反事实对照；独立SDK根，不测运行速度。"""
import argparse
import json
import os
from pathlib import Path
import platform
import shlex
import shutil
import sys

HERE = Path(__file__).resolve().parent
sys.path[:0] = [str(HERE), str(HERE.parent), str(HERE.parent/'standalone_app/benchmark')]
from contracts import APPS, require, schedule, timings, verify_project, summaries, python_command, verify_managed_equivalence
from measure import capture, save, sha, inventory, positive, elf
from aot_layout import reported_aot
from manifest_files import check_artifacts
from manifest_options import BOOL_OPTIONS, OPTION_KEYS, validate_options


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--base', type=Path, required=True)
    args = parser.parse_args(); base = args.base.resolve()
    require(sys.platform == 'linux' and platform.machine() == 'x86_64', '仅Linux x64同机测量')
    require(os.environ.get('GITHUB_ACTIONS') == 'true', '只用于hosted测量')
    out = base/'measurement'; out.mkdir(exist_ok=False)
    setup = json.loads((base/'setup.json').read_text())
    require(setup.get('passed') is True and setup.get('source_restored') is True, 'setup未完成或源码未恢复')
    python = Path(setup['python']).resolve(); dotnet = Path(setup['dotnet']).resolve()
    runtime = base/'tools/RyuPy.Runtime.dll'
    clis = {arm:base/'tools'/arm/'ryupy' for arm in ('direct','thin')}
    projects = {app:base/'projects'/app for app in APPS}
    frozen = {str(p):sha(p) for p in [base/'setup.json',python,dotnet,runtime,*clis.values()]}
    for key,path in {'runtime':runtime,'python':python,'dotnet':dotnet,**clis}.items():
        require(setup['tools'][key]['sha256']==sha(path), '工具不匹配setup：'+key)
    nuget=base/'NuGet.Config';packages=Path(setup['nuget_packages'])
    require(packages.resolve()==(base/'nuget').resolve(), '缓存不属于本次base')
    snapshots = {app:inventory(projects[app]) for app in APPS}
    save(out/'frozen-tools.json', frozen);save(out/'source-before.json',snapshots)
    plan = schedule();save(out/'schedule.json',plan)
    cwd = out/'unrelated-cwd';cwd.mkdir()
    roots = out/'buildroots';roots.mkdir()
    wrapper = out/'dotnet-timing'
    wrapper.write_text('#!/bin/sh\nif [ "$1" = publish ]; then\n  exec '+shlex.quote(str(dotnet))+' "$@" "-clp:PerformanceSummary"\nelse\n  exec '+shlex.quote(str(dotnet))+' "$@"\nfi\n')
    wrapper.chmod(0o700)
    env = dict(os.environ, DOTNET=str(wrapper), RYUPY_RUNTIME_DIR=str(runtime.parent),
               PYTHONDONTWRITEBYTECODE='1',PYTHONNOUSERSITE='1',PYTHONOPTIMIZE='0',
               DOTNET_CLI_UI_LANGUAGE='en-US', DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1',
               MSBUILDDISABLENODEREUSE='1',UseSharedCompilation='false',
               NUGET_PACKAGES=str(packages),NuGetAudit='false')
    result = {'passed':False,'setup':setup,'host':{'platform':platform.platform(),
              'cpu':Path('/proc/cpuinfo').read_text(),'cpu_count':os.cpu_count()},'builds':[],
              'scope':'热系统/包缓存、全新SDK根的完整公开构建；非历史版本基线、非增量缓存、非程序运行速度'}
    def command(prefix, argv, cwd=cwd, environment=env, timeout=1200):
        record = capture(prefix, argv, cwd, environment, timeout)
        require(record['exit']==0 and record['failure'] is None and record['raw_streams_complete'], str(prefix)+'失败；保留原件不重跑')
        return record
    try:
        command(out/'sdk-version',[dotnet,'--version'])
        require((out/'sdk-version.stdout').read_text(encoding='utf-8-sig').strip()=='10.0.403','SDK漂移')
        command(out/'cp-version',[python,'-c','import platform;print(platform.python_version())'])
        require((out/'cp-version.stdout').read_text().strip()=='3.12.13','CP版本漂移')
        cases = {'pyfiglet':{'standard':['-f','standard','hi'],'slant':['-f','slant','hi'],'listfonts':['--list_fonts']},'binaryornot':{}}
        data = out/'inputs';data.mkdir()
        binary_contract = json.loads((HERE.parent/'deployment_binaryornot/contract.json').read_text())
        for case in binary_contract['cases']:
            source = HERE.parent/'deployment_binaryornot'/case['input'];target=data/case['input']
            require(sha(source)==binary_contract['files'][case['input']],'固定分类输入改变')
            shutil.copyfile(source,target);cases['binaryornot'][case['id']]=[str(target)]
        input_snapshot=inventory(data);goldens={}
        for app in APPS:
            config=json.loads((projects[app]/'ryupy.json').read_text())
            require(config.get('aot_optimization','default')=='default','不是默认优化配置')
            entry=projects[app]/('console_entry.py' if app=='pyfiglet' else 'scanner.py')
            cp_env=dict(env,PYTHONPATH=str(projects[app]/('package' if app=='pyfiglet' else 'vendor')))
            for name,argv in cases[app].items():
                prefix=out/'oracle'/f'{app}-{name}'
                command(prefix,python_command(python,app,entry,argv),environment=cp_env,timeout=60)
                require(prefix.with_suffix('.stderr').read_bytes()==b'','CP oracle stderr非空')
                golden=prefix.with_suffix('.stdout').read_bytes()
                if app=='pyfiglet':
                    golden_file={'standard':'stdout.txt','slant':'slant.stdout.txt','listfonts':'fonts.stdout.txt'}[name]
                    require(golden==(HERE.parent/'standalone_app'/golden_file).read_bytes(),'pyfiglet冻结oracle不同')
                else:
                    expected=next(c['stdout'].encode() for c in binary_contract['cases'] if c['id']==name)
                    require(golden==expected,'分类冻结oracle不同')
                goldens[(app,name)]=golden
        identities={};layouts={};effective=None;cache_before=None;semantic_inputs={}
        save(out/'nuget-warm-config.json',{'sha256':sha(nuget),'text':nuget.read_text()})
        for row in plan:
            app,arm,key=row['app'],row['arm'],row['id'];prefix=out/'build-logs'/key
            if not row['warmup'] and cache_before is None:
                # 禁源只发生一次；测量期缺包必须失败，不允许在线填充掩盖缓存状态。
                nuget.write_text('<configuration><packageSources><clear /></packageSources></configuration>\n')
                save(out/'nuget-measured-config.json',{'sha256':sha(nuget),'text':nuget.read_text()})
                cache_before=inventory(packages);save(out/'nuget-cache-before-measured.json',cache_before)
            output=roots/key;require(not output.exists(),'构建根已存在')
            argv=[clis[arm],'build','--project',projects[app]/'ryupy.json','--target',
                  'metadata' if app=='pyfiglet' else 'scan','-o',output,'--diagnostic-stages']
            record=command(prefix,argv)
            stem='console_entry' if app=='pyfiglet' else 'scanner'
            layout=reported_aot(prefix.with_suffix('.stderr').read_bytes(),output,stem,'linux-x64')
            elf(layout.native);layouts[key]=layout
            assets=json.loads((layout.host/'obj/project.assets.json').read_text())
            restore=assets['project']['restore']
            require(str(nuget) in restore['configFilePaths'],'SDK未使用本轮NuGet.Config')
            if not row['warmup']:
                require(not restore.get('sources'), '测量恢复仍含联网源')
            require(str(packages)+os.sep in assets['packageFolders'] or str(packages) in assets['packageFolders'], 'SDK包缓存路径不同')
            evidence=verify_project(layout,arm,sha)
            managed=layout.managed.read_bytes();identities.setdefault(app,managed)
            evidence['managed_equivalence']=verify_managed_equivalence(managed,identities[app])
            require((layout.host/'sdk.version').read_text(encoding='utf-8-sig').strip()=='10.0.403','实际SDK漂移')
            receipt=json.loads((layout.sdk_root/'aot-optimization.json').read_text())
            require(receipt['requested']=='default' and receipt['native_sha256']==sha(layout.native),'优化收据输入不同')
            if effective is None:effective=receipt['effective']
            require(receipt['effective']==effective,'两组SDK有效优化不同')
            manifest=json.loads(layout.manifest.read_text());check_artifacts(layout.dist,manifest['artifacts'],layout.manifest.name)
            require(any(i['relative_path']=='RyuPy.Runtime.dll' and i['sha256']==sha(runtime) for i in manifest['build_inputs']),'runtime输入不同')
            metadata=json.loads((layout.managed.parent/'distribution-metadata.json').read_text())
            options=manifest['compiler']['options']
            require(set(options)==OPTION_KEYS|{'distribution_metadata'}, '编译选项字段不同')
            validate_options({k:options[k] for k in OPTION_KEYS})
            require(all(options[k] is False for k in BOOL_OPTIONS) and options['instance_modules']==[] and
                    options['module_environment_policy']=='explicit-only','非固定默认编译配置')
            require(options['distribution_metadata']==metadata and metadata['mode']=='closed_snapshot','metadata报告不匹配')
            expected_distributions={'pyfiglet':'1.0.2'} if app=='pyfiglet' else {'binaryornot':'0.4.4','chardet':'5.2.0'}
            require({d['metadata_name']:d['metadata_version'] for d in metadata['distributions']}==expected_distributions and
                    len(metadata['distributions'])==len(expected_distributions),'分发metadata版本/集合不同')
            require(len(manifest.get('resources',[]))==(553 if app=='pyfiglet' else 0),'真实资源集合数量不同')
            semantic={'options':options,'resources':manifest.get('resources',[]),'sources':manifest['sources']}
            semantic_inputs.setdefault(app,semantic)
            require(semantic==semantic_inputs[app],'同应用资源/metadata/源码身份不同')
            import re
            front=re.findall(rb'event=end stage=output-write module="-" elapsed_us=(\d+)',prefix.with_suffix('.stderr').read_bytes())
            require(len(front)==1,'前端结束标记不唯一')
            sdktext=(layout.sdk_root/'publish.stdout.log').read_text()
            timing=timings(sdktext)
            result['builds'].append({**row,'wall_ms':record['wall_ms'],'frontend_end_us':int(front[0]),
                'sdk_timing':timing,'entry':evidence,'optimization':receipt,'dist':str(layout.dist),
                'native_sha256':sha(layout.native),'resource_count':len(manifest.get('resources',[]))})
            save(out/'result.json',result)
        require(cache_before is not None and inventory(packages)==cache_before, '测量阶段改变NuGet缓存内容')
        save(out/'nuget-cache-after-measured.json',inventory(packages))
        # 全部计时结束再运行。复制所有产物后隐藏原项目和所有build，杜绝旧源码/中间树回退。
        moved=out/'moved';moved.mkdir();dist_snapshots={}
        for key,layout in layouts.items():
            target=moved/key;shutil.copytree(layout.dist,target)
            dist_snapshots[key]=inventory(layout.dist);require(inventory(target)==dist_snapshots[key],'搬迁字节改变')
        require(all(inventory(projects[a])==snapshots[a] for a in APPS),'构建修改项目输入')
        (base/'projects').rename(base/'projects-hidden-after-build')
        roots.rename(out/'buildroots-hidden-after-build')
        result['relocation']={'projects_from':str(base/'projects'),'projects_to':str(base/'projects-hidden-after-build'),
                              'builds_from':str(roots),'builds_to':str(out/'buildroots-hidden-after-build'),'all_dist_copies':str(moved)}
        clean={'PATH':'/usr/bin:/bin','LC_ALL':'C.UTF-8','TZ':'UTC'}
        result['runs']=[]
        for row in plan:
            key,app=row['id'],row['app'];native=moved/key/layouts[key].native.name
            for name,argv in cases[app].items():
                prefix=out/'runs'/f'{key}-{name}'
                rec=command(prefix,[native,*argv],environment=clean,timeout=60)
                positive(rec,prefix,goldens[(app,name)])
                result['runs'].append({'id':key,'case':name,'exit':rec['exit']})
            require(inventory(moved/key)==dist_snapshots[key],'执行修改搬迁包')
        require(inventory(data)==input_snapshot,'外部分类输入改变')
        require(all(sha(Path(p))==digest for p,digest in frozen.items()),'工具或setup改变')
        require(all(inventory(base/'projects-hidden-after-build'/a)==snapshots[a] for a in APPS),'隐藏源码改变')
        result['summary']=summaries(result['builds']);result['passed']=True
    finally:
        save(out/'result.json',result)


if __name__=='__main__':main()
