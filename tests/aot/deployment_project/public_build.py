"""普通项目公开AOT构建探针：不同cwd与整体搬迁重建，原流保留；不冒充独立深验/clean-container门。"""
import argparse
import json
import os
from pathlib import Path
import shutil
import sys
import traceback

HERE = Path(__file__).resolve().parent
sys.path.insert(0, str(HERE.parent/'standalone_app/benchmark'))
sys.path.insert(0, str(HERE.parent))
from measure import capture, elf, inventory, positive, require, save, sha
from aot_layout import reported_aot
from manifest_files import check_artifacts
from prepare import APP, configuration


def project_command(cli, project, target, output=None):
    result = [str(cli), 'build', '--project', str(project/'ryupy.json'), '--target', target]
    if output is not None: result += ['-o', str(output)]
    return result


def inputs(project):
    config = configuration()
    require(json.loads((project/'ryupy.json').read_text()) == config, '配置不同')
    locked = json.loads((APP/'inputs.json').read_text())['files']
    result = {}
    for relative, digest in locked.items():
        path = project/'package'/relative
        require(path.is_file() and not path.is_symlink() and sha(path) == digest, '锁定源码改变')
        result['package/'+relative] = digest
    for name in ('ryupy.json','resources.json','metadata-request.json','console_entry.py'):
        result[name] = sha(project/name)
    return result


def relocate_project(project, destination, evidence):
    """保留旧绝对来源owner产物，不冒认可把旧build缓存随源码搬迁复用。"""
    require(not destination.exists() and not evidence.exists(), '搬迁目的地必须全新')
    build = project/'build'
    if build.exists():
        require(build.is_dir() and not build.is_symlink(), '旧build目录非法')
        build.rename(evidence)
    project.rename(destination)
    return destination


def main():
    parser = argparse.ArgumentParser()
    for key in ('cli','runtime','project','output'):
        parser.add_argument('--'+key, type=Path, required=True)
    for key in ('python','dotnet'):
        parser.add_argument('--'+key, type=Path)
    parser.add_argument('--runtime-image')
    args = parser.parse_args()
    require(all((args.python,args.dotnet,args.runtime_image)) or not any((args.python,args.dotnet,args.runtime_image)), 'hosted深验参数必须完整')
    cli=args.cli.resolve(); runtime=args.runtime.resolve(); project=args.project.resolve()
    out=args.output.resolve(); out.mkdir(parents=True,exist_ok=False)
    require(not out.is_relative_to(project) and not project.is_relative_to(out), '证据与项目必须分离')
    before=inputs(project); tools={str(cli):sha(cli),str(runtime):sha(runtime)}
    save(out/'tools.json',tools);save(out/'project-inputs.json',before)
    env=dict(os.environ,RYUPY_RUNTIME_DIR=str(runtime.parent),PYTHONDONTWRITEBYTECODE='1',PYTHONOPTIMIZE='0',
             DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1',MSBUILDDISABLENODEREUSE='1',UseSharedCompilation='false')
    cwd=out/'unrelated-cwd';cwd.mkdir()
    result={'passed':False,'deep_receipt':'not-performed','clean_container':False,'performance':'not-measured','attempts':{},'failures':{}}
    moved=project.with_name(project.name+'-relocated-project');require(not moved.exists(),'搬迁目标已存在')
    try:
        for place in ('original-project','relocated-project'):
            if place=='relocated-project':
                project=relocate_project(project,moved,out/'original-project-build-preserved')
            require(inputs(project)==before,'整体搬迁改变输入')
            for target in ('cli','metadata'):
                label=place+'-'+target
                try:
                    # 不注入旧入口/includes/资源参数；公开配置独立决定整张构建图。
                    argv=project_command(cli,project,target)
                    rec=capture(out/label,argv,cwd,env,600,True)
                    require(rec['exit']==0 and rec['failure'] is None,'公开AOT构建失败')
                    entry=configuration()['targets'][target]['entry'];root=project/'build'/target
                    layout=reported_aot((out/(label+'.stderr')).read_bytes(),root,Path(entry).stem,'linux-x64')
                    manifest=json.loads(layout.manifest.read_text())
                    require(manifest['schema_version']==4,'应生成默认计划schema4')
                    check_artifacts(layout.dist,manifest['artifacts'],'ryupy-artifact.json')
                    elf(layout.native)
                    if args.runtime_image:
                        from audit_native import audit
                        audit(layout,project,target,out/(label+'-audit'),cli,runtime,args.python.resolve(),args.dotnet.resolve(),env)
                    bundle=out/(label+'-dist');shutil.copytree(layout.dist,bundle)
                    snapshot=inventory(bundle);hidden=project.with_name(project.name+'-hidden')
                    require(not hidden.exists(),'隔离目录冲突');project.rename(hidden)
                    try:
                        for variant,cmd,golden in [('standard',['-f','standard','hi'],'stdout.txt'),('slant',['-f','slant','hi'],'slant.stdout.txt'),('fonts',['--list_fonts'],'fonts.stdout.txt')]:
                            prefix=out/(label+'-'+variant)
                            run=capture(prefix,[bundle/layout.native.name]+cmd,cwd,{'PATH':'/usr/bin:/bin','LC_ALL':'C.UTF-8','TZ':'UTC'},60)
                            positive(run,prefix,(APP/golden).read_bytes())
                            if args.runtime_image:
                                args_container=['docker','run','--rm','--network','none','--read-only','--cap-drop=ALL','--security-opt=no-new-privileges', '--tmpfs','/tmp','--mount','type=bind,src='+str(bundle)+',dst=/app,readonly','--workdir','/tmp','--env','LC_ALL=C.UTF-8','--env','TZ=UTC',args.runtime_image,'/app/'+layout.native.name]+cmd
                                container_prefix=out/(label+'-'+variant+'-container')
                                container=capture(container_prefix,args_container,cwd,{'PATH':'/usr/bin:/bin','LC_ALL':'C.UTF-8','TZ':'UTC'},60)
                                positive(container,container_prefix,(APP/golden).read_bytes())
                    finally:
                        require(not project.exists(),'程序重建输入目录');hidden.rename(project)
                    require(inventory(bundle)==snapshot,'执行改变产物')
                    result['attempts'][label]={'passed':True,'dist':snapshot,'schema_version':4}
                except Exception:
                    result['failures'][label]=traceback.format_exc()
                save(out/'result.json',result)
            require(inputs(project)==before,'构建改变项目输入')
        require(all(sha(p)==h for p,h in tools.items()),'工具改变')
        require(len(result['attempts'])==4 and not result['failures'],'至少一项公开AOT验收未过')
        result['passed']=True
        if args.runtime_image:
            result['deep_receipt']='four-independent-receipts'
            result['clean_container']={'validated':True,'runs':12,'image':args.runtime_image}
    finally:
        result['project_final_path']=str(project)
        save(out/'result.json',result)


if __name__=='__main__': main()
