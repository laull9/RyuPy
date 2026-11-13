"""普通文件分类consumer的公开Native部署；47源审计、外部输入、搬迁和无运行时禁网容器。"""
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
from measure import capture,require,save,sha,inventory,elf
from hosted import ORACLE_TEST,check_oracle_test
from prepare import configuration


def public_command(cli,project,output):
    return [str(cli),'build','--project',str(project/'ryupy.json'),'--target','scan','-o',str(output)]


def oracle_environment(env,project,oracle,cli,runtime):
    result={k:v for k,v in env.items() if not k.startswith('RYUPY_DEFAULT_AOT_')}
    values=dict(ENTRY=project/'scanner.py',ROOT=project/'vendor',OUTPUT=oracle,
                METADATA=project/'distribution.json',INCLUDE='[]',CLI=cli,RUNTIME=runtime)
    result.update({'RYUPY_DEFAULT_AOT_'+k:str(v) for k,v in values.items()})
    return result


def verify_case(case,record,prefix):
    require(record['exit']==0 and record['failure'] is None,'consumer执行失败')
    require(prefix.with_suffix('.stderr').read_bytes()==b'','stderr不为空')
    require(prefix.with_suffix('.stdout').read_bytes()==case['stdout'].encode(),'完整分类stdout不同')


def container_command(dist,inputs,image,native,filename):
    return ['docker','run','--rm','--user',f'{os.getuid()}:{os.getgid()}','--network','none','--read-only',
        '--cap-drop=ALL','--security-opt=no-new-privileges','--tmpfs','/tmp',
        '--mount','type=bind,src='+str(dist)+',dst=/app,readonly',
        '--mount','type=bind,src='+str(inputs)+',dst=/inputs,readonly','--workdir','/tmp',
        '--env','LC_ALL=C.UTF-8','--env','TZ=UTC',image,'/app/'+native,'/inputs/'+filename]


def main():
    parser=argparse.ArgumentParser()
    for key in ('cli','runtime','project','output','python','dotnet'):parser.add_argument('--'+key,required=True,type=Path)
    parser.add_argument('--runtime-image',required=True);args=parser.parse_args()
    out=args.output.resolve();out.mkdir(parents=True,exist_ok=False)
    project=args.project.resolve();cli=args.cli.resolve();runtime=args.runtime.resolve()
    require(not out.is_relative_to(project) and not project.is_relative_to(out),'证据与项目需分离')
    require(json.loads((project/'ryupy.json').read_text())==configuration(),'项目配置变化')
    require(args.runtime_image.startswith('sha256:') and len(args.runtime_image)==71,'必须固定镜像ID')
    tools={str(p.resolve()):sha(p) for p in (cli,runtime,args.python,args.dotnet)}
    source_before=inventory(project);save(out/'frozen-tools.json',tools);save(out/'project-before.json',source_before)
    cwd=out/'unrelated-cwd';cwd.mkdir();inputs=out/'user-inputs';inputs.mkdir()
    contract=json.loads((HERE/'contract.json').read_text())
    for case in contract['cases']:shutil.copyfile(HERE/case['input'],inputs/case['input'])
    inputs_before=inventory(inputs)
    env=dict(os.environ,RYUPY_RUNTIME_DIR=str(runtime.parent),PYTHONDONTWRITEBYTECODE='1',
             DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1',MSBUILDDISABLENODEREUSE='1',UseSharedCompilation='false')
    result={'passed':False,'consumer':'binaryornot.check.is_binary; not upstream CLI',
            'performance':'not-measured','native_runs':0,'container_runs':0}
    def checked(label,argv,timeout=600):
        record=capture(out/label,argv,cwd,env,timeout)
        require(record['exit']==0 and record['failure'] is None,label+'失败');return record
    try:
        checked('no-python-dotnet',['docker','run','--rm','--network','none','--read-only',args.runtime_image,'sh','-c',
            'for p in python python3 dotnet; do if command -v "$p" >/dev/null 2>&1; then exit 9; fi; done'],60)
        build=out/'build';checked('public-build',public_command(cli,project,build),1200)
        layout=reported_aot((out/'public-build.stderr').read_bytes(),build,'scanner','linux-x64');elf(layout.native)
        oracle=out/'oracle';oracle_env=oracle_environment(env,project,oracle,cli,runtime)
        save(out/'oracle-request.json',{k:v for k,v in oracle_env.items() if k.startswith('RYUPY_DEFAULT_AOT_')})
        record=capture(out/'oracle-run',['cargo','test','--locked','-p','ryupy-driver','--lib','-j','1',ORACLE_TEST,
                '--','--exact','--ignored','--nocapture'],REPO,oracle_env,900)
        require(record['exit']==0 and record['failure'] is None,'独立oracle失败')
        check_oracle_test((out/'oracle-run.stdout').read_text())
        from validate_artifact import validate_binaryornot_artifact
        receipt=validate_binaryornot_artifact(layout,project,tools,python=args.python.resolve(),dotnet=args.dotnet.resolve(),
                cli=cli,runtime=runtime,oracle_dir=oracle,output=out/'receipt')
        require(receipt['structure_validated'],'独立PE审计未通过')
        moved=out/'relocated-dist';shutil.copytree(layout.dist,moved)
        snapshot=inventory(layout.dist);require(inventory(moved)==snapshot,'搬迁改变字节')
        hidden=project.with_name(project.name+'-hidden');require(not hidden.exists(),'隔离目录已存在');project.rename(hidden)
        clean={'PATH':'/usr/bin:/bin','LC_ALL':'C.UTF-8','TZ':'UTC'}
        try:
            for place,dist in [('original',layout.dist),('relocated',moved)]:
                for case in contract['cases']:
                    prefix=out/(place+'-'+case['id'])
                    record=capture(prefix,[dist/layout.native.name,inputs/case['input']],cwd,clean,60)
                    verify_case(case,record,prefix);result['native_runs']+=1
                    prefix=out/(place+'-'+case['id']+'-container')
                    record=capture(prefix,container_command(dist,inputs,args.runtime_image,layout.native.name,case['input']),cwd,clean,60)
                    verify_case(case,record,prefix);result['container_runs']+=1
        finally:
            require(not project.exists(),'程序重建输入目录');hidden.rename(project)
        require(inventory(project)==source_before,'项目输入变化')
        require(inventory(inputs)==inputs_before,'程序改变外部用户输入')
        require(inventory(layout.dist)==snapshot and inventory(moved)==snapshot,'程序改变dist')
        require(all(sha(p)==h for p,h in tools.items()),'工具变化')
        require(result['native_runs']==8 and result['container_runs']==8,'执行计数不完整')
        result.update(passed=True,clean_container_validated=True,image=args.runtime_image,native_sha256=receipt['native_sha256'])
    finally:save(out/'result.json',result)


if __name__=='__main__':main()
