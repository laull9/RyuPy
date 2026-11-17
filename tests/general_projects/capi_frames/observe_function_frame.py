#!/usr/bin/env python3
"""普通Instance函数frame的真实CP/CIL差分取证；已知差异保留原三流与终态报告，产物自动清理。"""
import argparse
import csv
from datetime import datetime, timezone
import io
import json
import os
from pathlib import Path
import shutil
import subprocess
import tempfile
import zipfile
import sys


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--cli', type=Path, default=Path(__file__).resolve().parents[3]/'target/debug/ryupy')
    parser.add_argument('--case', choices=('function-frame', 'uncaught', 'system_exit', 'native_error'), default='function-frame')
    parser.add_argument('--aot', action='store_true')
    args = parser.parse_args()
    args.python = args.python.resolve(); args.cli = args.cli.resolve()
    source = Path(__file__).resolve().parent
    logs = Path.home()/'.cache/ryupy/validation/capi-function-frame'/datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')
    logs.mkdir(parents=True)
    (logs/'scope.json').write_text(json.dumps(dict(case=args.case, mode='standalone' if args.aot else 'jit', python=str(args.python), cli=str(args.cli)), indent=2))
    print('EVIDENCE', logs, flush=True)
    with tempfile.TemporaryDirectory(prefix='ryupy-function-frame-', dir='/tmp') as temp:
        root = Path(temp)
        def run(name, command, env=None, required=True):
            result = subprocess.run(list(map(str, command)), cwd=root, env=env, text=True, stdout=subprocess.PIPE, stderr=subprocess.PIPE, timeout=300)
            (logs/(name+'.json')).write_text(json.dumps(dict(command=list(map(str, command)), exit=result.returncode, stdout=result.stdout, stderr=result.stderr), ensure_ascii=False, indent=2))
            if required and result.returncode: raise RuntimeError(name+'失败，见'+str(logs))
            return result
        cfg = json.loads(run('configuration', [args.python, '-I', '-c',
            'import sysconfig,json;print(json.dumps(dict(include=sysconfig.get_config_var("INCLUDEPY"),suffix=sysconfig.get_config_var("EXT_SUFFIX"),platform=sysconfig.get_platform(),tag="cp%d%d"%__import__("sys").version_info[:2])))']).stdout)
        inputs = root/'inputs'
        if args.case == 'function-frame':
            shutil.copytree(source/'pending', inputs)
        else:
            inputs.mkdir(); shutil.copy2(source/'ordinary'/(args.case+'.py'), inputs/'entry.py')
        run('venv', ['uv', 'venv', '--python', args.python, inputs/'.venv'])
        extension = root/('native_scope_probe'+cfg['suffix'])
        run('native-extension', ['clang', '-bundle', '-undefined', 'dynamic_lookup', '-Wall', '-Wextra', '-Werror', '-I'+cfg['include'], source/'native_scope_probe.c', '-o', extension])
        tag = cfg['tag']+'-'+cfg['tag']+'-'+cfg['platform'].replace('-', '_').replace('.', '_')
        info = 'capi_frame_fixture-1.0.dist-info'
        files = {extension.name:extension.read_bytes(), info+'/METADATA':b'Metadata-Version: 2.3\nName: capi-frame-fixture\nVersion: 1.0\nLicense: MIT\n',
                 info+'/WHEEL':f'Wheel-Version: 1.0\nRoot-Is-Purelib: false\nTag: {tag}\n'.encode(), info+'/licenses/LICENSE':b'MIT hand-written test fixture\n'}
        record = io.StringIO(); csv.writer(record).writerows((name,'','') for name in [*files,info+'/RECORD'])
        files[info+'/RECORD'] = record.getvalue().encode()
        wheel = root/f'capi_frame_fixture-1.0-{tag}.whl'
        with zipfile.ZipFile(wheel,'w') as archive:
            for name,data in files.items(): archive.writestr(name,data)
        run('install', ['uv', 'pip', 'install', '--python', inputs/'.venv/bin/python', '--no-deps', wheel])
        oracle = run('oracle', [inputs/'.venv/bin/python', '-B', inputs/'entry.py'], required=args.case == 'function-frame')
        output = root/'output'
        command = [args.cli, '--cpython-provider', 'auto', '--experimental-stock-module-namespaces', 'build', inputs/'entry.py', '-o', output]
        if args.aot: command.append('--aot')
        built = run('build', command)
        sys.path.insert(0, str(source.parents[1]/'cpython'))
        from support import built_product
        if args.aot:
            sys.path.insert(0, str(source.parents[1]/'cpython'))
            from support import built_product, standalone_layout, rid
            layout = standalone_layout(built.stderr.encode(), output, 'entry', rid())
            bundle, filename = layout.dist, layout.native.name
        else:
            layout = built_product(built.stderr, output, 'entry', 'jit')
            bundle, filename = layout.root, layout.entry.name
        moved = root/'moved'; shutil.move(bundle,moved); shutil.rmtree(inputs); wheel.unlink(); extension.unlink()
        if output.exists(): shutil.rmtree(output)
        report = root/'host.json'; env = dict(os.environ, RYUPY_CPYTHON_REPORT=str(report), PATH='', PYTHONHOME='/missing', PYTHONPATH='/missing')
        command = [moved/filename]
        actual = run('candidate', command, env=env, required=False)
        assert report.exists(), '候选必须生成实际Host报告'
        shutil.copy2(report,logs/'host.json')
        observed = json.loads(report.read_text())
        carrier = observed['call_carriers']
        closed = observed['state'] == 'closed' and all(carrier[key] == 0 for key in ('native_live','managed_handles','code_origin_records','code_origin_active'))
        print('CARRIER_CLOSED', closed, flush=True)
        assert closed, carrier
        if args.case != 'function-frame':
            assert actual.returncode == oracle.returncode
            assert actual.stdout == oracle.stdout
            assert actual.stderr.splitlines()[-1] == oracle.stderr.splitlines()[-1]
            assert 'Exception ignored' not in actual.stderr and 'CPython关闭失败' not in actual.stderr
        print('ORACLE', oracle.returncode, oracle.stdout.strip(), flush=True)
        print('CANDIDATE', actual.returncode, actual.stdout.strip(), flush=True)
        print('MATCH', (actual.returncode,actual.stdout,actual.stderr)==(oracle.returncode,oracle.stdout,oracle.stderr), flush=True)


if __name__ == '__main__':
    main()
