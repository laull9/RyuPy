#!/usr/bin/env python3
"""真实DLL/NativeAOT的独立进程SQLite门：旧flat、新marker和既有resolver优先，全部输出仓外。"""
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile


def main():
    here=Path(__file__).resolve().parent
    root=here.parents[2]
    evidence=Path.home()/'.cache/ryupy/validation/application-layout'/datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')
    evidence.mkdir(parents=True);print('EVIDENCE',evidence,flush=True)
    with tempfile.TemporaryDirectory(prefix='ryupy-layout-probe-',dir='/tmp') as temporary, (evidence/'commands.log').open('w') as log:
        work=Path(temporary);project=work/'project';project.mkdir();inputs=work/'inputs';inputs.mkdir()
        runtime=root/'runtime/RyuPy.Runtime'
        shutil.copy2(runtime/'bin/Release/net10.0/RyuPy.Runtime.dll',inputs/'RyuPy.Runtime.dll')
        for name in ['RuntimeDependencies.props','ThirdPartyNotices.sqlite3.txt']:
            shutil.copy2(runtime/name,inputs/name)
        shutil.copy2(here/'runtime_probe.cs',project/'Program.cs')
        (project/'Probe.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><AssemblyName>LayoutProbe</AssemblyName><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable><TreatWarningsAsErrors>true</TreatWarningsAsErrors></PropertyGroup><Import Project="../inputs/RuntimeDependencies.props"/><ItemGroup><Reference Include="RyuPy.Runtime"><HintPath>../inputs/RyuPy.Runtime.dll</HintPath></Reference></ItemGroup></Project>''')
        def run(command,env=None):
            log.write('COMMAND '+repr(list(map(str,command)))+'\n');log.flush()
            result=subprocess.run(list(map(str,command)),cwd=work,env=env,text=True,stdout=subprocess.PIPE,stderr=subprocess.PIPE,timeout=300)
            log.write(f'EXIT {result.returncode}\nSTDOUT\n{result.stdout}\nSTDERR\n{result.stderr}\n');log.flush()
            assert result.returncode==0,(result.returncode,result.stdout,result.stderr)
            return result
        results=[]
        for aot in (False,True):
            mode='aot' if aot else 'jit';output=work/(mode+'-publish')
            command=['dotnet','publish',project/'Probe.csproj','--disable-build-servers','-c','Release','-r','osx-arm64','-o',output]
            command+=['-p:PublishAot=true','-p:IlcTreatWarningsAsErrors=true','-p:ILLinkTreatWarningsAsErrors=true'] if aot else ['--self-contained','false','-p:UseAppHost=false']
            run(command)
            for canonical in (False,True):
                for custom in (False,True):
                    name=mode+('-canonical' if canonical else '-legacy')+('-custom' if custom else '-default')
                    target=work/name
                    if canonical:
                        dependencies=target/'internal';shutil.copytree(output,dependencies);entry='LayoutProbe'
                        if aot:shutil.move(dependencies/entry,target/entry)
                        else:(target/entry).write_text('CLR-hosted probe marker entry')
                        marker=dict(schema_version=1,kind='ryupy-application-layout',payload_mode='canonical-v1',entry=entry,entry_kind='native_aot' if aot else 'apphost')
                        (dependencies/'.ryupy-layout.json').write_text(json.dumps(marker))
                    else:shutil.copytree(output,target);dependencies=target
                    args=(['custom'] if custom else [])+(['canonical'] if canonical else [])
                    command=[target/'LayoutProbe'] if aot else [shutil.which('dotnet'),dependencies/'LayoutProbe.dll']
                    env={key:value for key,value in os.environ.items() if not key.startswith(('RYUPY_','LD_','DYLD_'))}
                    env['PATH']=''
                    env['DOTNET_ROLL_FORWARD']='Major'
                    actual=run([*command,*args],env)
                    assert not actual.stderr
                    results.append(dict(case=name,stdout=actual.stdout.strip(),status='passed'))
                    print('PASS',name,actual.stdout.strip(),flush=True)
        (evidence/'results.json').write_text(json.dumps(results,indent=2))


if __name__=='__main__':main()
