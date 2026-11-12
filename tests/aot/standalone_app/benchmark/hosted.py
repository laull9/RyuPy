"""独立bounded hosted编排；setup、工具构建、独立oracle和真应用测量分别留证。"""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
from measure import capture, require, save, sha

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[3]


def main():
    phase = argparse.ArgumentParser(); phase.add_argument('phase', choices=('tools', 'oracles', 'benchmark'))
    args = phase.parse_args()
    require(os.environ.get('GITHUB_ACTIONS') == 'true' and os.environ.get('RUNNER_ENVIRONMENT') == 'github-hosted', '仅hosted')
    head = subprocess.check_output(['git','rev-parse','HEAD'],cwd=ROOT,text=True).strip()
    require(head == os.environ['GITHUB_SHA'], '非实际同提交')
    base = Path(os.environ['RUNNER_TEMP'])/'standalone-app-benchmark'; base.mkdir(exist_ok=True)
    env = dict(os.environ, PYTHONDONTWRITEBYTECODE='1', PYTHONOPTIMIZE='0', PYTHONNOUSERSITE='1')
    python = Path(os.environ['PYTHON']).resolve(); dotnet = Path(os.environ['DOTNET']).resolve()
    tools = base/'tools'; prepared = base/'prepared'; oracle = base/'oracle'; evidence = base/'evidence'
    def command(label, argv, timeout, extra=None):
        record = capture(base/label, argv, ROOT, dict(env, **(extra or {})), timeout)
        require(record['exit'] == 0 and record['failure'] is None, label+'失败')
    if args.phase == 'tools':
        tools.mkdir()
        command('runtime-build', [dotnet,'build','runtime/RyuPy.Runtime/RyuPy.Runtime.csproj','-c','Release','-nologo'],300)
        command('cli-build', ['cargo','build','--locked','--release','-p','ryupy-cli'],900)
        shutil.copy2(ROOT/'target/release/ryupy', tools/'ryupy')
        shutil.copy2(ROOT/'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll', tools/'RyuPy.Runtime.dll')
        save(base/'build-profile.json', {'cli': 'cargo release', 'runtime': 'dotnet Release', 'oracle': 'test/debug bound to frozen release CLI/runtime', 'tool_build_cost': 'setup, excluded from app compilation wall'})
        save(base/'frozen-tools.json',{str(p):sha(p) for p in (tools/'ryupy',tools/'RyuPy.Runtime.dll',python,dotnet)})
        command('prepare-cp', [python,'-B',HERE.parent/'prepare_cp.py',prepared,python],120)
        command('runtime-image-build', ['docker','build','--iidfile',str(base/'runtime-image.id'),'-f',str(HERE/'runtime.Dockerfile'),str(HERE)],600)
        image=(base/'runtime-image.id').read_text().strip()
        command('runtime-image-inspect', ['docker','image','inspect',image],60)
        command('runtime-os-packages', ['docker','run','--rm','--network','none',image,'dpkg-query','-W'],60)
    elif args.phase == 'oracles':
        frozen=json.loads((base/'frozen-tools.json').read_text())
        require(all(sha(p)==h for p,h in frozen.items()), '工具不再冻结')
        oracle.mkdir()
        for case,entry in (('upstream',prepared/'package/pyfiglet/__main__.py'),('console',prepared/'console_entry.py')):
            values=dict(ENTRY=entry,ROOT=prepared/'package',OUTPUT=oracle/case,RESOURCES=prepared/'resources.json',
                        METADATA=prepared/'metadata-request.json',INCLUDE='["pyfiglet","pyfiglet.fonts"]',
                        CLI=tools/'ryupy',RUNTIME=tools/'RyuPy.Runtime.dll')
            command(case+'-oracle',['cargo','test','--locked','-p','ryupy-driver','--lib','-j','1',
                    'default_aot_real_graph_emit','--','--ignored','--nocapture'],600,
                    {'RYUPY_DEFAULT_AOT_'+k:str(v) for k,v in values.items()})
        require(all(sha(p)==h for p,h in frozen.items()), 'oracle改变工具')
    else:
        command('benchmark', [python,'-B',HERE/'run.py','--prepared',prepared,'--python',python,
                '--cli',tools/'ryupy','--runtime',tools/'RyuPy.Runtime.dll','--dotnet',dotnet,
                '--nuitka-archive',os.environ['NUITKA_ARCHIVE'],'--nuitka-tree-receipt',os.environ['NUITKA_TREE_RECEIPT'],
                '--oracle-root',oracle,'--runtime-image',(base/'runtime-image.id').read_text().strip(),
                '--output',evidence],2900)


if __name__ == '__main__': main()
