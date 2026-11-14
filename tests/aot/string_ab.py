"""同runner两棵产品源码树的字符串holder A/B；编译全部完成才交错采样。"""
import argparse
import json
import os
from pathlib import Path
import platform
import shutil
import sys
import time
from types import SimpleNamespace

from aot_layout import reported_aot
from current_baseline import PYTHON_VERSION, SDK_VERSION
from direct_baseline import measurement_environment
from direct_protocol import diagnostic_stats, validate_output
from perf_runner import Runner, digest, write_json
from string_ab_protocol import ARMS, CASES, ROUNDS, collect, summarize
from benchlib.artifact import verified_native

# 对应5655f497的已发布、已验归因快照；本地/远端commit元数据不同，产品树相同。
BASE_COMMIT = '8eeae125feaff2772ed175b56e0557566de16069'


def runtime_dir(root):
    return root/'runtime/RyuPy.Runtime/bin/Release/net10.0'


def run(root, base, candidate, targets, out, expected_base, expected_candidate):
    out.mkdir(parents=True, exist_ok=False)
    runner = Runner(out/'processes', time.monotonic()+4800)
    env = measurement_environment(os.environ)
    report = dict(status='started', protocol='string-holder-ab-v1', rounds=ROUNDS,
                  cases=CASES, arms=ARMS, statistics_in_wall=False,
                  timed_processes=250, pre_processes=25, diagnostic_processes=25,
                  startup_included=True, clean_machine=False)
    def check(label, args, cwd=root):
        return runner.run(label, args, cwd=cwd, env=env, limit=60)['stdout'].decode().strip()
    try:
        if sys.platform != 'linux' or platform.machine() != 'x86_64':
            raise ValueError('仅Linux x64')
        if platform.python_version() != PYTHON_VERSION:
            raise ValueError('CP版本不匹配')
        dotnet = shutil.which('dotnet', path=env['PATH'])
        if not dotnet or check('sdk', [dotnet, '--version']) != SDK_VERSION:
            raise ValueError('SDK不匹配')
        env['DOTNET'] = dotnet
        roots = {'base': base, 'candidate': candidate}
        commits = {'base': expected_base, 'candidate': expected_candidate}
        for name, source_root in roots.items():
            if check(f'{name}-head', ['git','rev-parse','HEAD'], source_root) != commits[name]:
                raise ValueError('产品提交不匹配')
            if check(f'{name}-clean', ['git','status','--porcelain'], source_root):
                raise ValueError('产品源码树不干净')
            if not (runtime_dir(source_root)/'RyuPy.Runtime.dll').is_file():
                raise ValueError('缺独立Runtime构建')
        if base == candidate or targets['base'] == targets['candidate']:
            raise ValueError('A/B构建不得共用目录')
        sources = root/'tests/aot/string_ab_fixtures'
        semantic_names = ('m3_6_numeric_accumulator', 'm3_10_string_accumulator')
        source_paths = {c:sources/f'{c}.py' for c in CASES}
        source_paths.update({c:root/'tests/e2e/phase3'/f'{c}.py' for c in semantic_names})
        source_digests = {c:digest(p) for c,p in source_paths.items()}
        report.update(commits=commits, python=sys.version, python_executable=sys.executable,
                      sdk=SDK_VERSION, dotnet=dotnet, source_sha256=source_digests,
                      cpuinfo=Path('/proc/cpuinfo').read_text(), rustc=check('rustc',['rustc','--version']))
        products = {c:{'cpython':[sys.executable,'-B',str(p)]} for c,p in source_paths.items()}
        publications = []
        # 两组完整预构建；不将构建、ILC和诊断时间混入wall样本。
        for variant, source_root in roots.items():
            target = targets[variant]
            build = target/'string-ab';build.mkdir(exist_ok=False)
            cli = target/'release/ryupy'
            if not cli.is_file():raise ValueError('缺release CLI')
            build_env = dict(env, CARGO_TARGET_DIR=str(target), RYUPY_RUNTIME_DIR=str(runtime_dir(source_root)))
            for case, source in source_paths.items():
                output=build/case
                runner.run(f'mir-{variant}-{case}',[cli,'dump',source,'--emit','mir'],cwd=source_root,env=build_env,limit=120)
                result=runner.run(f'build-{variant}-{case}',[cli,'build',source,'--aot','-o',output],cwd=source_root,env=build_env,limit=300)
                native=verified_native(output,SimpleNamespace(name=case,path=source),result['stderr'].decode())
                layout=reported_aot(result['stderr'],output,case,'linux-x64')
                data=json.loads(layout.manifest.read_text())
                if data['schema_version']!=1 or data['toolchain']['sdk_version']!=SDK_VERSION:
                    raise ValueError('执行域/SDK不匹配')
                if not layout.managed.with_suffix('.runtimeconfig.json').is_file():
                    raise ValueError('缺同PE JIT runtimeconfig')
                products[case][variant+'_jit']=[dotnet,str(layout.managed)]
                products[case][variant+'_aot']=[str(native)]
                publications.append(dict(variant=variant,case=case,build_ms=result['wall_ms'],
                                         options=data['compiler']['options'],managed=str(layout.managed),native=str(native),
                                         runtime_sha256=digest(runtime_dir(source_root)/'RyuPy.Runtime.dll')))
        # 编译配置必须逐负载一致（输入输出路径由记录显式区分）。
        for case in source_paths:
            values=[x['options'] for x in publications if x['case']==case]
            if values[0]!=values[1]:raise ValueError('A/B编译选项漂移')
        report['publications']=publications;write_json(out/'run.json',report)
        # 固定语义用例先逐字节对照两版本JIT/AOT，任何失败禁止进入性能结论。
        for case in semantic_names:
            oracle=runner.run(f'semantic-{case}-cpython',products[case]['cpython'],cwd=root,env=env,limit=120)
            expected_streams=oracle['stdout'],oracle['stderr']
            for arm in ARMS[1:]:
                result=runner.run(f'semantic-{case}-{arm}',products[case][arm],cwd=root,env=env,limit=120)
                validate_output(result,expected_streams)
        expected, records=collect(runner,products,root,env,lambda rows:write_json(out/'wall-partial.json',rows))
        write_json(out/'wall.json',dict(protocol='string-holder-ab-v1',records=records,summary=summarize(records)))
        diagnostics=[]
        for case in CASES:
            for arm in ARMS:
                label=f'diagnostic-{case}-{arm}'; stats=runner.logs/label/'stats.json'
                command=products[case][arm]
                if arm=='cpython':
                    command=[sys.executable,'-B',str(root/'tests/benchlib/cpython_harness.py'),str(stats),str(sources/f'{case}.py')]
                result=runner.run(label,command,cwd=root,env=env,limit=120,stats=arm!='cpython')
                validate_output(result,expected[case])
                diagnostics.append(dict(workload=case,arm=arm,stats=diagnostic_stats(stats,'cpython' if arm=='cpython' else 'aot'),comparable_wall=False))
                write_json(out/'diagnostics.json',diagnostics)
        for name, source_root in roots.items():
            if check(f'{name}-final-head',['git','rev-parse','HEAD'],source_root)!=commits[name] or check(f'{name}-final-clean',['git','status','--porcelain'],source_root):
                raise ValueError('产品源码变化')
        if source_digests!={c:digest(p) for c,p in source_paths.items()}:raise ValueError('输入变化')
        report['status']='passed'
    except BaseException as error:
        report.update(status='failed',error=f'{type(error).__name__}: {error}')
        raise
    finally:write_json(out/'run.json',report)


if __name__=='__main__':
    parser=argparse.ArgumentParser(description=__doc__)
    for key in ('base','candidate','base-target','candidate-target','out'):
        parser.add_argument('--'+key,type=Path,required=True)
    parser.add_argument('--candidate-commit',required=True)
    args=parser.parse_args()
    run(Path(__file__).resolve().parents[2],args.base.resolve(),args.candidate.resolve(),
        {'base':args.base_target.resolve(),'candidate':args.candidate_target.resolve()},args.out.resolve(),BASE_COMMIT,args.candidate_commit)
