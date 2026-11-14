"""同源直接CP/JIT/AOT wall基线；所有统计进程在计时结束后独立运行。"""
import argparse
import json
import os
from pathlib import Path
import platform
import shutil
import sys
import time
from types import SimpleNamespace

from current_baseline import CASES, PYTHON_VERSION, SDK_VERSION, clean_environment
from direct_protocol import ARMS, ROUNDS, collect_direct, diagnostic_stats, direct_commands, summary, validate_output
from perf_runner import Runner, write_json
from aot_layout import reported_aot

sys.path.insert(0,str(Path(__file__).resolve().parents[1]))
from benchlib.artifact import verified_native


def measurement_environment(original):
    """除实验旗标外，拒绝继承CLR profiler和动态加载注入。"""
    env=clean_environment(original)
    for key in list(env):
        if key.startswith(('CORECLR_', 'COR_', 'LD_', 'DYLD_')):env.pop(key)
    return env


def run(root,target,out):
    out.mkdir(parents=True,exist_ok=False)
    runner=Runner(out/'processes',deadline=time.monotonic()+2400)
    env=measurement_environment(os.environ);env['CARGO_TARGET_DIR']=str(target)
    report={'status':'started','protocol':'direct-wall-v1','cases':CASES,'rounds':ROUNDS,
            'pre_runs_per_arm':1,'direct_processes_planned':len(CASES)*3*(ROUNDS+1),
            'diagnostic_processes_planned':len(CASES)*3,'statistics_in_wall':False,
            'cold_definition':'新进程，不清OS文件缓存；预运行不表示进程内暖稳态',
            'diagnostic_wall_comparable':False,'universal_speedup_claim':False}
    try:
        if sys.platform!='linux' or platform.machine()!='x86_64':raise ValueError('仅Linux x64')
        if platform.python_version()!=PYTHON_VERSION:raise ValueError('CP版本不匹配')
        def check(label,args):
            return runner.run(label,args,cwd=root,env=env,limit=60)['stdout'].decode().strip()
        revision=check('revision',['git','rev-parse','HEAD'])
        if check('clean',['git','status','--porcelain']):raise ValueError('需要干净源码提交')
        dotnet=shutil.which('dotnet',path=env['PATH'])
        if check('sdk',[dotnet,'--version'])!=SDK_VERSION:raise ValueError('实际SDK不匹配')
        env['DOTNET']=dotnet  # driver与JIT使用同一已核工具，不继承另一DOTNET入口。
        report.update(commit=revision,python=sys.version,python_executable=sys.executable,
                      sdk=SDK_VERSION,dotnet_executable=dotnet,platform=platform.platform(),
                      cpuinfo=Path('/proc/cpuinfo').read_text(),pythonhashseed=env['PYTHONHASHSEED'],
                      rustc=check('rustc',['rustc','--version']))
        build=target/'direct-wall-build';build.mkdir(exist_ok=False)
        cli=target/'release/ryupy'
        if not cli.is_file():raise ValueError('需要预先构建release CLI')
        products={};published=[]
        # 所有编译/ILC先完成，再启动wall采样。JIT复用AOT同次DLL，不重新编译。
        for case in CASES:
            source=root/f'tests/benchmarks/{case}.py';output=build/case
            result=runner.run(f'build-{case}',[cli,'build',source,'--aot','-o',output],cwd=root,env=env,limit=300)
            native=verified_native(output,SimpleNamespace(name=case,path=source),result['stderr'].decode())
            layout=reported_aot(result['stderr'],output,case,'linux-x64')
            data=json.loads(layout.manifest.read_text())
            if data['toolchain']['sdk_version']!=SDK_VERSION or data['schema_version']!=1:
                raise ValueError('AOT SDK或纯原生执行域不匹配')
            if not layout.managed.with_suffix('.runtimeconfig.json').is_file():raise ValueError('同PE JIT缺runtimeconfig')
            products[case]=direct_commands(sys.executable,dotnet,source,layout.managed,native)
            published.append({'name':case,'sdk':SDK_VERSION,'options':data['compiler']['options'],
                              'artifacts':data['artifacts'],'build_wall_ms':result['wall_ms'],
                              'managed_input':str(layout.managed),'native':str(native)})
        report['published']=published;write_json(out/'run.json',report)
        expected,records=collect_direct(runner,products,CASES,root,env)
        write_json(out/'wall.json',{'protocol':'direct-wall-v1','records':records,'summary':summary(records,CASES)})
        # 诊断仅供分配/GC热点归因；不把这些进程的wall/Main混入直接启动对照。
        diagnostics=[]
        for case in CASES:
            for arm in ARMS:
                label=f'diagnostic-{case}-{arm}';stats=runner.logs/label/'stats.json'
                command=products[case][arm]
                if arm=='cpython':
                    command=[sys.executable,'-B',str(root/'tests/benchlib/cpython_harness.py'),str(stats),
                             str(root/f'tests/benchmarks/{case}.py')]
                result=runner.run(label,command,cwd=root,env=env,limit=120,stats=arm!='cpython')
                validate_output(result,expected[case])
                diagnostics.append({'workload':case,'arm':arm,'comparable_wall':False,
                                    'stats':diagnostic_stats(stats,arm),'process_evidence':result['case']})
                write_json(out/'diagnostics.json',diagnostics)
        if check('final-revision',['git','rev-parse','HEAD'])!=revision or check('final-clean',['git','status','--porcelain']):
            raise ValueError('测量期间源码变化')
        report['status']='passed'
    except BaseException as error:
        report.update(status='failed',error=f'{type(error).__name__}: {error}')
        raise
    finally:write_json(out/'run.json',report)


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--target',type=Path,required=True);parser.add_argument('--out',type=Path,required=True)
    args=parser.parse_args();run(Path(__file__).resolve().parents[2],args.target.resolve(),args.out.resolve())
