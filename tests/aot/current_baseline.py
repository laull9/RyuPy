"""复用公开bench入口的当前三负载CP/JIT/AOT新进程基线，不推导暖稳态或跨机加速。"""
import argparse
import json
import math
import os
from pathlib import Path
import platform
import shutil
import subprocess
import sys
import time

from platform_runner import Runner

CASES = ('hello', 'calls', 'str_concat')
PYTHON_VERSION = '3.12.13'
SDK_VERSION = '10.0.403'
RUNS = 5
WARMUP = 1


def clean_environment(original):
    # SDK安装位置属于工具选择；JIT/GC/编译器实验旗标不继承。
    env = {k:v for k,v in original.items() if not k.startswith(('RYUPY_', 'COMPlus_', 'DOTNET_', 'PYTHON'))}
    for key in ('DOTNET_ROOT', 'DOTNET_ROOT_X64', 'DOTNET_INSTALL_DIR'):
        if key in original: env[key] = original[key]
    env.update(PYTHONHASHSEED='0', PYTHONDONTWRITEBYTECODE='1',
               MSBUILDDISABLENODEREUSE='1', DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1',
               UseSharedCompilation='false', LANG='C.UTF-8', LC_ALL='C.UTF-8')
    env.pop('PYTHONPATH', None); env.pop('PYTHONHOME', None)
    return env


def validate_result(data, backend):
    """已有runner会把坏stats读成空字典，此处必须拒绝，不能生成貌似成功的基线。"""
    env = data['environment']
    if (env['backend'], env['python'], env['dotnet'], env['runs'], env['warmup']) != (
            backend, PYTHON_VERSION, SDK_VERSION, RUNS, WARMUP):
        raise ValueError('后端/工具版本/样本计划不匹配')
    rows = data['results']
    if len(rows) != len(CASES) or {r['name'] for r in rows} != set(CASES):
        raise ValueError('基准集合漂移或重复')
    for row in rows:
        if row['status'] != 'ok' or row['backend'] != backend or row['variants']:
            raise ValueError('未通过基准或额外实验变体')
        compile_ms = row['compile_ms']
        if type(compile_ms) not in (int,float) or not math.isfinite(compile_ms) or compile_ms <= 0:
            raise ValueError('编译计时无效')
        for arm in ('cpython', 'ryupy'):
            series = row[arm]
            for field in ('wall_ms', 'main_ms', 'maxrss'):
                values = series[field]
                if len(values) != RUNS or any(type(x) not in (int,float) or not math.isfinite(x) or x < 0 for x in values):
                    raise ValueError('样本缺失或无效')
            stats = series['stats']
            required = {'main_ms','gc0','gc1','gc2'}
            if arm == 'ryupy': required |= {'alloc_bytes','heap_bytes','gc_pause_ms','jit_ms','jit_methods'}
            if not required <= stats.keys() or any(type(stats[k]) not in (int,float) or not math.isfinite(stats[k]) or stats[k] < 0 for k in required):
                raise ValueError('统计缺失或无效')


def run(root, target, out):
    out.mkdir(parents=True, exist_ok=False)
    logs=out/'logs';logs.mkdir()
    env=clean_environment(os.environ)
    env.update(CARGO_TARGET_DIR=str(target), PYTHON=sys.executable)
    runner=Runner(logs, deadline=time.monotonic()+2400)
    report={'status':'started','cases':CASES,'runs':RUNS,'warmup':WARMUP,
            'arm_order':['jit','aot'],'sampling':'每次新进程，CP先于产品，不清OS缓存',
            'resource_aggregation':'last_timed_process','performance_claim':False}
    try:
        if sys.platform != 'linux' or platform.machine() != 'x86_64': raise ValueError('仅Linux x64基线')
        if platform.python_version() != PYTHON_VERSION: raise ValueError('CPython版本不匹配')
        def command(label, args):
            return runner.run(label,args,cwd=root,env=env,check=True,timeout=60).stdout.decode().strip()
        revision=command('revision',['git','rev-parse','HEAD'])
        if command('clean-tree',['git','status','--porcelain']): raise ValueError('基线需要干净源码提交')
        if command('sdk-version',['dotnet','--version']) != SDK_VERSION: raise ValueError('实际SDK不匹配')
        report.update(commit=revision, platform=platform.platform(), python=sys.version,
                      python_executable=sys.executable, sdk=SDK_VERSION,
                      dotnet_executable=shutil.which('dotnet',path=env.get('PATH')),
                      cpuinfo=Path('/proc/cpuinfo').read_text(), run_environment={k:env[k] for k in
                      ('PYTHONHASHSEED','MSBUILDDISABLENODEREUSE','DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER','UseSharedCompilation')})
        for backend in ('jit','aot'):
            name='current-baseline-'+backend
            saved=target/'bench/results'/f'{name}.json'
            if saved.exists(): raise ValueError('拒绝覆盖旧基线结果')
            args=[sys.executable,'-B','tests/run_benchmarks.py','--runs',str(RUNS),'--warmup',str(WARMUP),'--save',name,'--detail']
            if backend=='aot':args.append('--aot')
            args.extend(CASES)
            try:
                runner.run(backend,args,cwd=root,env=env,check=True,timeout=1100)
            finally:
                # 原runner非零退出也可能写出失败/部分结果，不能只归档成功样本。
                if saved.is_file(): shutil.copy2(saved,out/f'{backend}.json')
            data=json.loads(saved.read_text());validate_result(data,backend)
            if data['environment']['commit'] != revision[:len(data['environment']['commit'])]:
                raise ValueError('测量源码提交漂移')
        manifests=list((target/'bench/aot').glob('*/*.dist/ryupy-artifact.json'))
        if len(manifests)!=len(CASES):raise ValueError('AOT发布数不符，必须使用独立target')
        published=[]
        for path in manifests:
            data=json.loads(path.read_text())
            if data['toolchain']['sdk_version']!=SDK_VERSION or data['schema_version']!=1:
                raise ValueError('实际AOT SDK或执行域与纯原生计划不符')
            published.append({'name':path.parent.stem,'schema':data['schema_version'],
                              'sdk':data['toolchain']['sdk_version'],'options':data['compiler']['options'],
                              'artifacts':data['artifacts']})
        if {item['name'] for item in published} != set(CASES): raise ValueError('AOT发布负载身份漂移')
        report['published']=published
        if command('final-revision',['git','rev-parse','HEAD'])!=revision or command('final-clean',['git','status','--porcelain']):
            raise ValueError('运行期间源码变化')
        report['status']='passed'
    except BaseException as error:
        report.update(status='failed',error=f'{type(error).__name__}: {error}')
        raise
    finally:
        (out/'run.json').write_text(json.dumps(report,ensure_ascii=False,indent=2,allow_nan=False))


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--target',type=Path,required=True)
    parser.add_argument('--out',type=Path,required=True)
    args=parser.parse_args()
    run(Path(__file__).resolve().parents[2],args.target.resolve(),args.out.resolve())
