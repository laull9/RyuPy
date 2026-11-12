"""第二真实应用公开项目Native门：独立oracle、源隔离、搬迁和禁网无Python容器。"""
import argparse
import json
import os
from pathlib import Path
import shutil
import sys

HERE=Path(__file__).resolve().parent
REPO=HERE.parents[2]
sys.path[:0]=[str(HERE.parent),str(HERE.parent/'standalone_app/benchmark')]
from aot_layout import reported_aot
from measure import capture, require, save, sha, inventory, elf
from hosted import ORACLE_TEST, check_oracle_test
from prepare import configuration


def public_command(cli,project,output):
    return [str(cli),'build','--project',str(project/'ryupy.json'),'--target','table','-o',str(output)]


def oracle_environment(env,project,oracle,cli,runtime):
    result={k:v for k,v in env.items() if not k.startswith('RYUPY_DEFAULT_AOT_')}
    roots=[str(project/'vendor/table'),str(project/'vendor/width')]
    values=dict(ENTRY=project/'tools/tabulate_console.py',ROOT=roots[0],ROOTS=json.dumps(roots),
                OUTPUT=oracle,METADATA=project/'distribution.json',INCLUDE='["tabulate","wcwidth"]',CLI=cli,RUNTIME=runtime)
    result.update({'RYUPY_DEFAULT_AOT_'+k:str(v) for k,v in values.items()})
    return result


def verify_task(case,record,prefix,output_file=None):
    require(case in ('grid','html-file'),'未知任务')
    require(record['exit']==0 and record['failure'] is None,'真实入口执行失败')
    require(prefix.with_suffix('.stderr').read_bytes()==b'','stderr不为空')
    expected=(HERE/'grid.stdout.txt').read_bytes() if case=='grid' else b''
    require(prefix.with_suffix('.stdout').read_bytes()==expected,'完整stdout不同')
    if case=='html-file':
        require(output_file.is_file() and output_file.read_bytes()==(HERE/'html.output.txt').read_bytes(),'完整HTML文件不同')


def main():
    parser=argparse.ArgumentParser()
    for key in ('cli','runtime','project','output','python','dotnet'):
        parser.add_argument('--'+key,required=True,type=Path)
    parser.add_argument('--runtime-image',required=True)
    args=parser.parse_args();out=args.output.resolve();out.mkdir(parents=True,exist_ok=False)
    project=args.project.resolve();cli=args.cli.resolve();runtime=args.runtime.resolve()
    require(not out.is_relative_to(project) and not project.is_relative_to(out),'证据/项目需分离')
    require(json.loads((project/'ryupy.json').read_text())==configuration(),'项目配置变化')
    require(args.runtime_image.startswith('sha256:') and len(args.runtime_image)==71,'必须固定容器image ID')
    tools={str(p.resolve()):sha(p) for p in (cli,runtime,args.python,args.dotnet)}
    source_before=inventory(project);save(out/'frozen-tools.json',tools);save(out/'project-before.json',source_before)
    cwd=out/'unrelated-cwd';cwd.mkdir();inputs=out/'user-inputs';inputs.mkdir()
    for name in ('grid.input.txt','html.input.txt'):shutil.copyfile(HERE/name,inputs/name)
    user_inputs_before=inventory(inputs)
    env=dict(os.environ,RYUPY_RUNTIME_DIR=str(runtime.parent),PYTHONDONTWRITEBYTECODE='1',
             DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1',MSBUILDDISABLENODEREUSE='1',UseSharedCompilation='false')
    result={'passed':False,'performance':'not-measured','native_runs':0,'container_runs':0}
    def checked(label,argv,environment=env,timeout=600):
        rec=capture(out/label,argv,cwd,environment,timeout)
        require(rec['exit']==0 and rec['failure'] is None,label+'失败');return rec
    try:
        checked('no-python-dotnet',['docker','run','--rm','--network','none','--read-only',args.runtime_image,'sh','-c',
            'for p in python python3 dotnet; do if command -v "$p" >/dev/null 2>&1; then exit 9; fi; done'],timeout=60)
        build=out/'build';checked('public-build',public_command(cli,project,build))
        layout=reported_aot((out/'public-build.stderr').read_bytes(),build,'tabulate_console','linux-x64');elf(layout.native)
        oracle=out/'oracle'
        oracle_env=oracle_environment(env,project,oracle,cli,runtime)
        save(out/'oracle-request.json',{k:v for k,v in oracle_env.items() if k.startswith('RYUPY_DEFAULT_AOT_')})
        rec=capture(out/'oracle-run',['cargo','test','--locked','-p','ryupy-driver','--lib','-j','1',ORACLE_TEST,
                    '--','--exact','--ignored','--nocapture'],REPO,oracle_env,600)
        require(rec['exit']==0 and rec['failure'] is None,'oracle失败')
        check_oracle_test((out/'oracle-run.stdout').read_text())
        from validate_artifact import validate_tabulate_artifact
        receipt=validate_tabulate_artifact(layout,project,tools,python=args.python.resolve(),dotnet=args.dotnet.resolve(),
                cli=cli,runtime=runtime,oracle_dir=oracle,output=out/'receipt')
        require(receipt['structure_validated'],'独立PE审计未通过')
        moved=out/'relocated-dist';shutil.copytree(layout.dist,moved)
        snapshot=inventory(layout.dist);require(inventory(moved)==snapshot,'搬迁改变字节')
        hidden=project.with_name(project.name+'-hidden');require(not hidden.exists(),'隔离路径已存在');project.rename(hidden)
        clean={'PATH':'/usr/bin:/bin','LC_ALL':'C.UTF-8','TZ':'UTC'}
        try:
            for place,dist in [('original',layout.dist),('relocated',moved)]:
                for case,fmt,input_name in [('grid','grid','grid.input.txt'),('html-file','html','html.input.txt')]:
                    output_file=out/(place+'-html.result');argv=['-1','-f',fmt,str(inputs/input_name)]
                    if case=='html-file':argv=['-o',str(output_file)]+argv
                    prefix=out/(place+'-'+case);rec=capture(prefix,[dist/layout.native.name]+argv,cwd,clean,60)
                    verify_task(case,rec,prefix,output_file);result['native_runs']+=1
                    data=out/(place+'-'+case+'-container-data');data.mkdir()
                    container_file=data/'output.html';argv=['-1','-f',fmt,'/inputs/'+input_name]
                    if case=='html-file':argv=['-o','/output/output.html']+argv
                    command=['docker','run','--rm','--network','none','--read-only','--cap-drop=ALL','--security-opt=no-new-privileges',
                        '--tmpfs','/tmp','--mount','type=bind,src='+str(dist)+',dst=/app,readonly',
                        '--mount','type=bind,src='+str(inputs)+',dst=/inputs,readonly',
                        '--mount','type=bind,src='+str(data)+',dst=/output','--workdir','/tmp',
                        '--env','LC_ALL=C.UTF-8','--env','TZ=UTC',args.runtime_image,'/app/'+layout.native.name]+argv
                    prefix=out/(place+'-'+case+'-container');rec=capture(prefix,command,cwd,clean,60)
                    verify_task(case,rec,prefix,container_file);result['container_runs']+=1
        finally:
            require(not project.exists(),'程序重建输入根');hidden.rename(project)
        require(inventory(project)==source_before,'项目输入变化')
        require(inventory(inputs)==user_inputs_before,'运行改变用户输入文件')
        require(inventory(layout.dist)==snapshot and inventory(moved)==snapshot,'运行改变dist')
        require(all(sha(p)==h for p,h in tools.items()),'工具变更')
        require(result['native_runs']==4 and result['container_runs']==4,'执行计数不完整')
        result.update(passed=True,clean_container_validated=True,image=args.runtime_image,native_sha256=receipt['native_sha256'])
    finally:save(out/'result.json',result)


if __name__=='__main__':main()
