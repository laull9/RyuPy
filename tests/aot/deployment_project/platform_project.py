"""macOS/Windows同宿主项目Native验收；保源码隔离/搬迁/依赖原流，不声明无SDK干净机器。"""
import argparse
import hashlib
import json
import os
import re
from pathlib import Path
import shutil
import sys
import tarfile
import time
import traceback
import urllib.request

HERE=Path(__file__).resolve().parent
REPO=HERE.parents[2]
sys.path.insert(0,str(HERE.parent))
from platform_runner import Runner
from platform_support import host, native_format, audit, clean_environment
from smoke import executable
from aot_layout import reported_aot
from manifest_verify import dependency_binaries
from prepare import prepare, APP
sys.path.insert(0,str(APP))
from validate_artifact import validate_app_artifact, inventory


def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def save(path,value): path.write_text(json.dumps(value,indent=2)+'\n',encoding='utf-8')
def require(value,message):
    if not value: raise ValueError(message)

def platform_golden(name, raw, linesep):
    # 上游渲染入口detach后直接写UTF-8 bytes，只有字体列表走文本print。
    require(name in ('standard','slant','fonts'), '未知黄金模式')
    return raw.replace(b'\n', linesep.encode()) if name=='fonts' else raw


def check_windows_tests(stdout):
    prefix='test distribution_metadata::files::windows_tests::distribution_metadata_windows_'
    expected={prefix+name+' ... ok' for name in ('plain_and_bounded',
        'handle_identity_and_write_delete_denial','final_symlink_and_junction_rejected')}
    actual=[line for line in stdout.splitlines() if line.startswith('test ') and not line.startswith('test result:')]
    require(len(actual)==3 and set(actual)==expected, 'Windows安全测试必须三项实际通过，不接受零匹配/skip')
    require(any(line.startswith('test result: ok. 3 passed; 0 failed; 0 ignored;') for line in stdout.splitlines()), 'Windows安全测试计数不符')


def source_command(python, package, entry, argv):
    # 使用明确源路径启动原入口，不向环境加入PYTHONPATH；Native不执行此wrapper。
    wrapper="import runpy,sys;sys.path.insert(0,sys.argv[1]);sys.argv=sys.argv[2:];runpy.run_path(sys.argv[0],run_name='__main__')"
    return [str(python),'-B','-c',wrapper,str(package),str(entry)]+argv


def check_missing_appdata(result):
    require(result.returncode != 0 and result.stdout == b'' and b"KeyError: 'APPDATA'" in result.stderr,
            '缺APPDATA负控未得到应用声明的KeyError')


def main():
    parser=argparse.ArgumentParser()
    parser.add_argument('--rid',required=True,choices=['osx-arm64','win-x64'])
    parser.add_argument('--out',required=True,type=Path)
    parser.add_argument('--python',required=True)
    parser.add_argument('--dotnet',required=True)
    args=parser.parse_args();identity=host(args.rid)
    out=args.out.resolve();out.mkdir(parents=True,exist_ok=False);logs=out/'logs';logs.mkdir()
    run=Runner(logs,deadline=time.monotonic()+4200)
    python=Path(executable(args.python));dotnet=Path(executable(args.dotnet))
    env=dict(os.environ,DOTNET=str(dotnet),DOTNET_ROOT=str(dotnet.parent),PYTHONUTF8='1',
             PYTHONIOENCODING='utf-8',PYTHONDONTWRITEBYTECODE='1',PYTHONNOUSERSITE='1',
             PYTHONOPTIMIZE='0',DOTNET_CLI_UI_LANGUAGE='en-US')
    result={'passed':False,'rid':args.rid,'host':identity,'clean_machine_without_runtime_tested':False,
            'performance':'not-measured','arms':{},'failures':{}}
    try:
        commit=run.run('source-head',['git','rev-parse','HEAD'],cwd=REPO,check=True).stdout.decode().strip()
        require(commit==os.environ.get('GITHUB_SHA'),'不是当前CI候选')
        result['source_head']=commit
        if args.rid=='win-x64':
            longpaths=run.run('git-longpaths',['git','config','--global','--get','core.longpaths'],cwd=REPO,check=True).stdout.decode().strip()
            require(longpaths=='true' and Path(os.environ['CARGO_HOME']).resolve()==(Path(os.environ['RUNNER_TEMP'])/'c').resolve(),'Windows构建路径准备未生效')
            result['windows_build_paths']={'git_core_longpaths':longpaths,'cargo_home':os.environ['CARGO_HOME']}
        version=run.run('python-version',[str(python),'-c','import sys; print(sys.version.split()[0])'],cwd=out,env=env,check=True)
        require(version.stdout.decode().strip()=='3.12.13','CP版本不符')
        sdk=run.run('dotnet-version',[str(dotnet),'--version'],cwd=out,env=env,check=True)
        require(sdk.stdout.decode().strip()=='10.0.403','SDK版本不符')
        info=run.run('dotnet-info',[str(dotnet),'--info'],cwd=out,env=env,check=True).stdout.decode('utf-8-sig')
        architectures=re.findall(r'^\s*Architecture:\s*(\S+)\s*$',info,re.M)
        rids=re.findall(r'^\s*RID:\s*(\S+)\s*$',info,re.M)
        require(architectures and all(v==('arm64' if args.rid=='osx-arm64' else 'x64') for v in architectures) and rids==[args.rid],'.NET实际RID/架构不同')
        rust=run.run('rustc-info',['rustc','-vV'],cwd=REPO,env=env,check=True)
        expected='aarch64-apple-darwin' if args.rid=='osx-arm64' else 'x86_64-pc-windows-msvc'
        require(('host: '+expected) in rust.stdout.decode() and 'release: 1.99.1' in rust.stdout.decode(),'Rust宿主/版本不同')
        recipe=json.loads((REPO/'tests/aot/wcwidth_ci/project-source-recovery.json').read_text(encoding='utf-8'))
        item=next(x for x in recipe['packages'] if x['package']=='pyfiglet');archive=out/item['filename']
        with urllib.request.urlopen(item['url'],timeout=120) as response:archive.write_bytes(response.read())
        require(sha(archive)==item['sha256'],'原包hash不符');save(out/'download.json',item)
        store=out/'source-download';store.mkdir()
        with tarfile.open(archive) as source:source.extractall(store,filter='data')
        original=next(store.glob('pyfiglet-*'));project=prepare(out/'project',original)
        source_before=inventory(project);save(out/'project-inputs.json',source_before)
        run.run('runtime-build',[str(dotnet),'build','runtime/RyuPy.Runtime/RyuPy.Runtime.csproj','-c','Release','-nologo'],cwd=REPO,env=env,check=True,timeout=300)
        run.run('cli-build',['cargo','build','--locked','--release','-p','ryupy-cli'],cwd=REPO,env=env,check=True,timeout=900)
        if args.rid=='win-x64':
            safety=run.run('windows-nofollow-tests',['cargo','test','--locked','-p','ryupy-driver','--lib','-j','1','distribution_metadata_windows_','--','--nocapture'],cwd=REPO,env=env,check=True,timeout=600)
            check_windows_tests(safety.stdout.decode('utf-8'))
        tools=out/'tools';tools.mkdir();cli=tools/('ryupy.exe' if os.name=='nt' else 'ryupy')
        shutil.copy2(REPO/'target/release'/cli.name,cli)
        runtime=tools/'RyuPy.Runtime.dll';shutil.copy2(REPO/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll',runtime)
        frozen={str(p.resolve()):sha(p) for p in (cli,runtime,python,dotnet)};save(out/'frozen-tools.json',frozen)
        env['RYUPY_RUNTIME_DIR']=str(tools)
        dependency_tools={'otool':executable('otool')} if args.rid=='osx-arm64' else {'dumpbin':executable('dumpbin')}
        cwd=out/'unrelated-cwd';cwd.mkdir()
        variants=[('standard',['-f','standard','hi'],'stdout.txt'),('slant',['-f','slant','hi'],'slant.stdout.txt'),('fonts',['--list_fonts'],'fonts.stdout.txt')]
        for target,relative in [('cli','package/pyfiglet/__main__.py'),('metadata','console_entry.py')]:
            try:
                entry=project/relative;goldens={}
                # CP与Native使用完全相同的隔离环境；CP通过显式源路径启动原入口。
                clean_root=out/(target+'-clean-env');clean_root.mkdir();clean=clean_environment(clean_root)
                clean_before=inventory(clean_root)
                save(out/(target+'-cp-environment.json'),{'cwd':str(cwd),'env':clean,
                     'source_launch':'explicit sys.path plus runpy.run_path; original entry and argv'})
                if args.rid=='win-x64':
                    missing=dict(clean);missing.pop('APPDATA')
                    negative=run.run(target+'-cp-missing-appdata',source_command(python,project/'package',entry,['-f','standard','hi']),cwd=cwd,env=missing)
                    check_missing_appdata(negative)
                for name,argv,gold in variants:
                    cp=run.run(target+'-cp-'+name,source_command(python,project/'package',entry,argv),cwd=cwd,env=clean,check=True)
                    expected=platform_golden(name,(APP/gold).read_bytes(),os.linesep)
                    require(cp.stdout==expected and cp.stderr==b'','平台CP黄金不一致');goldens[name]=cp.stdout
                require(inventory(clean_root)==clean_before,'CP对照污染隔离profile')
                output=out/(target+'-build')
                built=run.run(target+'-public-build',[str(cli),'build','--project',str(project/'ryupy.json'),'--target',target,'-o',str(output)],cwd=cwd,env=env,check=True,timeout=600)
                layout=reported_aot(built.stderr,output,entry.stem,args.rid);native_format(layout.native,args.rid)
                oracle=out/(target+'-oracle')
                values=dict(ENTRY=entry,ROOT=project/'package',OUTPUT=oracle,RESOURCES=project/'resources.json',METADATA=project/'metadata-request.json',INCLUDE='["pyfiglet","pyfiglet.fonts"]',CLI=cli,RUNTIME=runtime)
                test='default_aot::tests::default_aot_real_graph_emit'
                answer=run.run(target+'-oracle',['cargo','test','--locked','-p','ryupy-driver','--lib','-j','1',test,'--','--exact','--ignored','--nocapture'],cwd=REPO,env=dict(env,**{'RYUPY_DEFAULT_AOT_'+k:str(v) for k,v in values.items()}),check=True,timeout=600)
                require([s for s in answer.stdout.decode().splitlines() if s.startswith('test ') and not s.startswith('test result:')]==['test '+test+' ... ok'],'oracle不是唯一测试')
                receipt=validate_app_artifact(layout,project,frozen,case=target,python=python,dotnet=dotnet,cli=cli,runtime=runtime,oracle_dir=oracle,output=out/(target+'-receipt'),expected_rid=args.rid)
                moved=out/(target+'-relocated');shutil.copytree(layout.dist,moved)
                require(inventory(moved)==inventory(layout.dist),'搬迁字节变化')
                dist_before=inventory(layout.dist)
                hidden=out/'project-hidden';project.rename(hidden)
                try:
                    save(out/(target+'-execution-environment.json'),{'cwd':str(cwd),'env':clean,'source_hidden_at':str(hidden),'clean_machine':False})
                    if args.rid=='win-x64':
                        negative=run.run(target+'-native-missing-appdata',[str(layout.native),'-f','standard','hi'],cwd=cwd,env=missing)
                        check_missing_appdata(negative)
                    for location,dist in [('original',layout.dist),('relocated',moved)]:
                        data=json.loads((dist/'ryupy-artifact.json').read_text(encoding='utf-8'))
                        dependency_logs=out/(target+'-'+location+'-dependency-logs');dependency_logs.mkdir()
                        dependencies=audit(Runner(dependency_logs,deadline=run.deadline),dist,args.rid,dependency_tools,binaries=dependency_binaries(dist,data))
                        save(out/(target+'-'+location+'-dependencies.json'),dependencies)
                        for name,argv,_ in variants:
                            actual=run.run(target+'-'+location+'-'+name,[str(dist/layout.native.name)]+argv,cwd=cwd,env=clean,check=True)
                            require(actual.stdout==goldens[name] and actual.stderr==b'','Native与平台CP原流不符')
                finally:hidden.rename(project)
                require(inventory(clean_root)==clean_before,'Native运行改变隔离profile')
                require(inventory(project)==source_before,'运行改变项目源')
                require(inventory(layout.dist)==dist_before and inventory(moved)==dist_before,'执行改变产物')
                require(all(sha(p)==h for p,h in frozen.items()),'冻结工具变化')
                result['arms'][target]={'passed':True,'receipt_native_sha256':receipt['native_sha256'],'semantic_runs':6}
            except Exception:result['failures'][target]=traceback.format_exc()
            save(out/'result.json',result)
        require(set(result['arms'])=={'cli','metadata'} and not result['failures'],'至少一入口未通过')
        result['passed']=True
    finally:save(out/'result.json',result)


if __name__=='__main__':main()
