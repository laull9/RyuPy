"""binaryornot真实consumer的构建/交付/执行基线；首次与重复构建分开，不冒充增量缓存。"""
import json
import os
from pathlib import Path
import re
import shutil
import sys

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path[:0] = [str(HERE.parent), str(HERE.parent/'deployment_binaryornot'),
               str(HERE.parent/'standalone_app/benchmark')]
from aot_layout import reported_aot
from measure import capture, require, save, sha, inventory, positive, elf
from runner import public_command


def main():
    require(os.environ.get('GITHUB_ACTIONS') == 'true', '只用于hosted实际测量')
    base = Path(os.environ['RUNNER_TEMP'])/'deployment-binaryornot'
    out = base/'build-profile'; out.mkdir(exist_ok=False)
    project = base/'project'; cli = base/'tools/ryupy'
    runtime = base/'tools/RyuPy.Runtime.dll'; python = Path(os.environ['PYTHON'])
    frozen = {str(p):sha(p) for p in [cli, runtime, python]}
    source = inventory(project); save(out/'source-before.json', source)
    cwd = out/'unrelated-cwd'; cwd.mkdir()
    data = out/'inputs'; data.mkdir()
    contract = json.loads((HERE.parent/'deployment_binaryornot/contract.json').read_text())
    paths = []
    for i in range(16):
        for case in contract['cases']:
            path = data/(str(i)+'-'+case['input'])
            shutil.copyfile(HERE.parent/'deployment_binaryornot'/case['input'], path)
            paths.append(str(path))
    data_snapshot = inventory(data)
    workloads = {'startup': [], 'mixed64': paths}
    env = dict(os.environ, RYUPY_RUNTIME_DIR=str(runtime.parent), PYTHONDONTWRITEBYTECODE='1',
               DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1', MSBUILDDISABLENODEREUSE='1', UseSharedCompilation='false')
    result = {'passed':False, 'profile':'default', 'builds':{}, 'runs':{},
              'scope':'同一应用输出根两次完整构建；非incremental cache，系统/包缓存未清空',
              'runtime_scope':'新进程+已热文件缓存；首/重建产物不是性能A/B',
              'commit':os.environ['GITHUB_SHA']}
    golden = {}
    try:
        for name, args in workloads.items():
            p = out/(name+'-cp')
            rec = capture(p, [python, '-B', '-S', project/'scanner.py', *args], cwd,
                          dict(env, PYTHONPATH=str(project/'vendor')), 120)
            require(rec['exit']==0 and rec['failure'] is None and rec['raw_streams_complete'], 'CP失败')
            require(p.with_suffix('.stderr').read_bytes()==b'', 'CP stderr非空')
            golden[name] = p.with_suffix('.stdout').read_bytes()
        output = out/'build'
        for label in ['first', 'repeat']:
            prefix = out/(label+'-build')
            rec = capture(prefix, [*public_command(cli, project, output), '--diagnostic-stages'], cwd, env, 1200)
            require(rec['exit']==0 and rec['failure'] is None and rec['raw_streams_complete'], label+'构建失败')
            stderr = prefix.with_suffix('.stderr').read_bytes()
            layout = reported_aot(stderr, output, 'scanner', 'linux-x64'); elf(layout.native)
            stages = re.findall(rb'event=end stage=output-write module="-" elapsed_us=(\d+)', stderr)
            require(len(stages)==1, '缺唯一前端完成标记')
            dist = out/(label+'-dist'); shutil.copytree(layout.dist, dist)
            build_tree = inventory(layout.attempt.parent)
            result['builds'][label] = {'total_ms':rec['wall_ms'], 'managed_pipeline_end_us':int(stages[0]),
                'tail_scope':'总时长减前端标记不是精确SDK计时，含进程/布局开销',
                'build_logical_bytes':build_tree['logical_bytes'], 'dist':inventory(dist),
                'native_sha256':sha(dist/'scanner'), 'attempt':str(layout.attempt)}
            # 所有构建结束再计时，避免编译器负载污染原生运行测量。
        hidden = base/'project-hidden-profile'; require(not hidden.exists(), '隔离目录冲突')
        project.rename(hidden)
        try:
            clean = {'PATH':'/usr/bin:/bin','LC_ALL':'C.UTF-8','TZ':'UTC'}
            for name,args in workloads.items():
                result['runs'][name] = {x:[] for x in ['first','repeat']}
                for index in range(24):
                    for label in (['first','repeat'] if index%2==0 else ['repeat','first']):
                        prefix = out/'samples'/f'{name}-{index:02d}-{label}'
                        rec = capture(prefix, [out/(label+'-dist')/'scanner', *args], cwd, clean, 120)
                        positive(rec,prefix,golden[name])
                        if index>=4:result['runs'][name][label].append(rec['wall_ms'])
        finally:
            hidden.rename(project)
        for label in ['first','repeat']:
            require(inventory(out/(label+'-dist'))==result['builds'][label]['dist'], '执行改变交付目录')
        require(inventory(project)==source and inventory(data)==data_snapshot, '输入改变')
        require(all(sha(p)==h for p,h in frozen.items()), '工具改变')
        save(out/'source-after.json', inventory(project))
        result['passed']=True
    finally:
        save(out/'result.json',result)


if __name__=='__main__': main()
