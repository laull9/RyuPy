#!/usr/bin/env python3
"""用户原 ABC 用例：Mac默认输出布局，一次AOT与一次CP/native，无JIT。"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import tempfile
import time
from aot_layout import reported_aot
from manifest_files import plain_stat
from manifest_verify import relocate_manifest, read_sdk_version, verify_manifest
from platform_runner import Runner
from platform_support import host, clean_environment
from cabi_probe.probe import tool

RID = 'osx-arm64'
SDK = '10.0.403'
FIXTURE = 'tests/e2e/phase3/m3_6_abc.py'


def record(path):
    plain_stat(path)
    raw=path.read_bytes()
    return {'size':len(raw),'sha256':hashlib.sha256(raw).hexdigest()}


def publish_command(cli, source):
    return [str(cli),'build','--aot',str(source)]


def same_output(reference, actual):
    assert reference.returncode==actual.returncode==0
    assert reference.stdout==actual.stdout and reference.stderr==actual.stderr


def retain_source(source, work):
    plain_stat(source,directory=True)
    assert source.parent.samefile(work)
    target=work/'build-evidence'
    assert not target.exists()
    source.rename(target)
    assert not source.exists()
    return target


def main(argv=None):
    p=argparse.ArgumentParser(description=__doc__)
    p.add_argument('--out',type=Path,required=True)
    p.add_argument('--python',default='python3.12')
    p.add_argument('--ryupy')
    p.add_argument('--dotnet',default=os.environ.get('DOTNET','dotnet'))
    args=p.parse_args(argv)
    start=time.monotonic(); metadata=host(RID)
    repo=Path(__file__).resolve().parents[2]
    args.out.mkdir(parents=True,exist_ok=True)
    work=Path(tempfile.mkdtemp(prefix='default-layout-',dir=args.out.resolve()))
    logs=work/'logs'; logs.mkdir(); print(work,flush=True)
    runner=Runner(logs,deadline=start+900)
    result={'status':'failed','rid':RID,'host':metadata,'budget_seconds':900,
        'publish_count':0,'reference_count':0,'native_count':0,'jit_count':0,
        'clean_machine_without_runtime_tested':False,'ci_source_commit':os.environ.get('GITHUB_SHA')}
    def run(label, command, timeout=30, **kwargs):
        return runner.run(label,list(map(str,command)),cwd=kwargs.pop('cwd',work),timeout=timeout,**kwargs)
    try:
        python=tool(args.python); cli=tool(args.ryupy or str(repo/'target/debug/ryupy')); dotnet=tool(args.dotnet)
        env={**os.environ,'DOTNET':dotnet,'DOTNET_CLI_UI_LANGUAGE':'en-US',
             'PYTHONUTF8':'1','PYTHONIOENCODING':'utf-8'}
        run('python-version',[python,'-c',"import sys; assert sys.implementation.name=='cpython' and sys.version_info[:3]==(3,12,13); print(sys.version)"],env=env,check=True)
        (work/'global.json').write_text(json.dumps({'sdk':{'version':SDK,'rollForward':'disable'}})+'\n',encoding='utf-8')
        sdk_probe=run('dotnet-version',[dotnet,'--version'],env=env,check=True)
        assert sdk_probe.stdout.decode('utf-8-sig').strip()==SDK
        source=work/'source'; source.mkdir(); original=repo/FIXTURE
        entry=source/original.name; shutil.copy2(original,entry)
        result['source']=record(original); assert record(entry)==result['source']
        result['cli']=record(Path(cli))
        result['reference_count']+=1
        cp=run('python',[python,'-B',entry],env=env,check=False)
        assert cp.returncode==0
        result['publish_count']+=1
        published=run('publish',publish_command(cli,entry),env=env,check=True,timeout=600)
        layout=reported_aot(published.stderr,source,entry.stem,RID)
        assert layout.build.name=='m3_6_abc.build' and layout.dist.name=='m3_6_abc.dist'
        sdk=read_sdk_version(layout.host/'sdk.version'); result['sdk_version']=sdk
        assert sdk==SDK
        for name in ('publish.stdout.log','publish.stderr.log'):
            shutil.copy2(layout.sdk_root/name,logs/('driver-'+name))
        shutil.copy2(layout.host/'sdk.version',logs/'sdk.version')
        assert b'Generating native code' in (logs/'driver-publish.stdout.log').read_bytes()
        manifest=verify_manifest(layout.dist,layout.native,RID,sdk)
        assert manifest['schema_version']==1
        assert {x['module_name']:x['compiled_text_sha256'] for x in manifest['sources']}=={'__main__':result['source']['sha256']}
        inputs={x['relative_path']:{k:x[k] for k in ('size','sha256')} for x in manifest['build_inputs']}
        assert inputs=={'managed-entry.dll':record(layout.managed),'RyuPy.Runtime.dll':record(layout.managed.parent/'RyuPy.Runtime.dll')}
        result['build_inputs']=inputs; result['original_manifest']=record(layout.manifest)
        native,moved=relocate_manifest(layout.dist,layout.native,work/'搬迁 & dist',RID,sdk)
        result['artifacts']=moved['artifacts']; result['native_before']=record(native)
        result['relocated_manifest']=record(native.parent/'ryupy-artifact.json')
        assert result['relocated_manifest']==result['original_manifest']
        retain_source(source,work)
        assert not entry.exists() and not layout.build.exists() and not layout.dist.exists()
        result['source_build_dist_original_paths_absent']=True
        result['build_evidence_retained']=True
        result['native_count']+=1
        actual=run('native',[native],cwd=native.parent,env=clean_environment(work),check=False)
        same_output(cp,actual)
        result['behavior']={kind:{'exit':value.returncode,
            'stdout_sha256':hashlib.sha256(value.stdout).hexdigest(),
            'stderr_sha256':hashlib.sha256(value.stderr).hexdigest()} for kind,value in [('python',cp),('native',actual)]}
        verify_manifest(native.parent,native,RID,sdk)
        assert record(native)==result['native_before']
        assert (result['publish_count'],result['reference_count'],result['native_count'],result['jit_count'])==(1,1,1,0)
        assert time.monotonic()<start+900, '整体900秒预算已耗尽'
        result['status']='passed'
    except BaseException as error:
        result['failure']=str(error)
        raise
    finally:
        result['elapsed_seconds']=time.monotonic()-start
        (work/'result.json').write_text(json.dumps(result,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')


if __name__=='__main__': main()
