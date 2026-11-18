#!/usr/bin/env python3
"""provider资源锚点真实合同：两独立入口同次CP差分，删除输入和构建树后搬迁AOT执行。"""
import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import shutil
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT/'tests/cpython'))
from support import built_product, product_layout, environment, inspect_host_report
from stock_thin_compiler import Recorder, require_success


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cli', type=Path, required=True)
    parser.add_argument('--python', type=Path, required=True)
    args = parser.parse_args()
    args.cli = args.cli.resolve(); args.python = args.python.resolve()
    evidence = Path.home()/'.cache/ryupy/validation/stock-resource-locations'/datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')
    evidence.mkdir(parents=True)
    print(evidence, flush=True)
    with (evidence/'commands.log').open('x') as stream, tempfile.TemporaryDirectory(prefix='ryupy-resource-anchor-') as temporary:
        work = Path(temporary).resolve(); recorder = Recorder(stream); env = environment()
        fixture = ROOT/'tests/cases/stock_resource_locations'
        products = []
        for name, resource in [('orchard', 'harvest.txt'), ('estuary', 'samples/tide.txt')]:
            inputs = work/(name+' 输入'); inputs.mkdir()
            entry = inputs/(name+'.py'); shutil.copy2(fixture/entry.name, entry)
            data = inputs/resource; data.parent.mkdir(exist_ok=True); shutil.copy2(fixture/resource, data)
            oracle = require_success(recorder.run([args.python, '-I', '-B', entry], work, env), 'CP即时oracle')
            assert not oracle.stderr
            require_success(recorder.run(['uv', 'venv', '--python', args.python, inputs/'.venv'], work, env), 'venv')
            request = inputs/'resources.json'
            request.write_text(json.dumps({'schema_version':1,'modules':[{'module':'__main__','files':[resource]}]}))
            output = work/(name+' 构建')
            built = require_success(recorder.run([args.cli, '--cpython-provider', 'auto',
                '--experimental-stock-thin-functions', 'build', entry, '--aot', '--resources', request,
                '-o', output], work, env, 360), name+'-AOT')
            layout = built_product(built.stderr, output, name, 'aot')
            manifest = json.loads((layout.data/'ryupy-artifact.json').read_text())
            locations = [row for row in manifest['module_locations'] if row['module_name']=='__main__']
            assert len(locations)==1 and locations[0]['kind']=='executable_relative', locations
            assert locations[0]['relative_file'].startswith('internal/resources/v1/')
            deployed = layout.root/locations[0]['relative_file']
            assert deployed.name==entry.name
            assert (deployed.parent/resource).read_bytes()==data.read_bytes()
            moved = work/(name+' 已搬迁'); shutil.move(layout.root, moved)
            products.append((name, moved, layout.entry.name, oracle))
            shutil.rmtree(inputs); shutil.rmtree(output)
        stream.write('INPUTS_AND_BUILDS_REMOVED True\n'); stream.flush()
        unrelated = work/'任意工作目录'; unrelated.mkdir()
        for name, moved, filename, oracle in products:
            assert product_layout(moved).entry==moved/filename
            report = work/(name+'-host.json')
            run_env = dict(env, PATH='', PYTHONHOME='/missing-python', PYTHONPATH='/missing-python',
                           RYUPY_CPYTHON_REPORT=str(report))
            actual = recorder.run([moved/filename], unrelated, run_env)
            assert (actual.returncode,actual.stdout,actual.stderr)==(oracle.returncode,oracle.stdout,oracle.stderr), name
            observed = inspect_host_report(report, moved)
            stock = observed['stock_compiled_program']
            assert stock['native_calls']==3 and stock['destroyed']==1, stock
            (evidence/(name+'-host.json')).write_text(json.dumps(observed,ensure_ascii=False,indent=2))
            print('PASS',name,flush=True)


if __name__=='__main__':
    main()
