#!/usr/bin/env python3
"""通用接口目录真实产品门：传统扩展/多阶段heap type、stub匹配、普通共享库与声明负例。"""
import argparse
from contextlib import contextmanager
from datetime import datetime, timezone
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

from fixtures import HERE, prepare

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT/'tests/cpython'))
from stock_thin_compiler import Recorder, require_success
from support import built_product, provider_data, audit_native_paths, environment, inspect_host_report, rid, standalone_layout
from onefile_support import audit_onefile_output


@contextmanager
def workspace(stream):
    """成功和失败都物理清理，并把清理事实保存在仓外证据。"""
    root = None
    try:
        with tempfile.TemporaryDirectory(prefix='ryupy-interface-exports-', dir='/tmp') as temporary:
            root = Path(temporary)
            stream.write('WORK '+str(root)+'\n'); stream.flush()
            yield root
    finally:
        stream.write('TEMPORARY_REMOVED '+str(root is not None and not root.exists())+'\n')
        stream.flush()


def audit_catalog(report, case):
    """只按公共目录schema核对实际导出/声明；普通同后缀库与stub不能获得执行身份。"""
    catalog = report['interface_catalog']
    assert catalog['schema'] == 1
    module = case['directory'].replace('/', '.')+'.'+case['extension']
    ordinary = case['directory'].replace('/', '.')+'.ordinary_image'
    stub_only = case['directory'].replace('/', '.')+'.stub_only'
    files = catalog['files']
    binary = next(row for row in files if row['module'] == module and row['kind'] == 'native_image')
    declaration = next(row for row in files if row['module'] == module and row['kind'] == 'type_stub')
    assert binary['execution'] == 'cpython_extension'
    assert binary['native']['status'] == 'audited'
    assert binary['native']['initializer_verified'] is True
    assert binary['native']['expected_initializer'] == 'PyInit_'+case['extension']
    assert binary['native']['method_signatures_verified'] is False
    assert catalog['modules'][module]['execution_provider'] == 'cpython_extension'
    assert declaration['execution'] == 'declaration_only' and declaration['business_body'] is False
    assert declaration['interface']['parse_status'] == 'parsed'
    assert declaration['interface']['machine_abi']['status'] == 'not_derived'
    declarations = {row['name']: row for row in declaration['interface']['declarations']}
    function = declarations['relay' if case['id'] == 'legacy' else 'Gauge.__init__']
    assert function['parameters'][0]['kind'] == 'positional_only'
    keyword = next(row for row in function['parameters'] if row['kind'] == 'keyword_only')
    assert keyword['name'] == ('offset' if case['id'] == 'legacy' else 'label')
    assert keyword['required'] is False
    assert keyword['default'] == ('0' if case['id'] == 'legacy' else '"seed"')
    if case['id'] == 'multiphase':
        assert {'Gauge', 'Gauge.sample', 'Gauge.echo', 'Gauge.label'} <= declarations.keys()
    auxiliary = next(row for row in files if row['module'] == ordinary and row['kind'] == 'native_image')
    assert auxiliary['native']['status'] == 'audited'
    assert auxiliary['execution'] == 'native_dependency'
    assert auxiliary['native']['initializer_verified'] is False
    assert catalog['modules'][ordinary]['execution_provider'] is None
    assert len(catalog['modules'][ordinary]['declarations']) == 1
    assert catalog['modules'][stub_only]['execution_provider'] is None
    assert catalog['modules'][stub_only]['implementations'] == []
    stub = next(row for row in files if row['module'] == stub_only)
    assert stub['kind'] == 'type_stub' and stub['business_body'] is False
    assert stub['interface']['parse_status'] == 'parsed'
    assert catalog['modules']['__main__']['execution_provider'] == 'cil'
    return catalog


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cli', type=Path, default=ROOT/'target/debug/ryupy')
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--mode', choices=('jit', 'standalone', 'onefile'), action='append')
    parser.add_argument('--case', choices=('legacy', 'multiphase'), action='append')
    args = parser.parse_args()
    args.python = args.python.resolve(); args.cli = args.cli.resolve()
    version = subprocess.check_output([args.python, '-I', '-B', '-c',
        'import platform; print(platform.python_version())'], text=True).strip()
    if version != '3.12.13':
        raise RuntimeError('此门显式固定CPython3.12.13；其它版本另行运行并登记')
    evidence = Path.home()/'.cache/ryupy/validation/interface-exports'/datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')
    evidence.mkdir(parents=True)
    print('EVIDENCE', evidence, flush=True)
    results = []
    with (evidence/'commands.log').open('x', encoding='utf-8') as stream:
        recorder = Recorder(stream)
        with workspace(stream) as work:
            inputs = work/'private input'; inputs.mkdir()
            env = environment()

            def run(command, cwd, run_env, label):
                return require_success(recorder.run(command, cwd, run_env, 360), label)

            venv = inputs/'.venv'
            run(['uv', 'venv', '--python', args.python, venv], work, env, 'venv')
            python = venv/('Scripts/python.exe' if sys.platform == 'win32' else 'bin/python')
            packages = prepare(inputs, python, env, run)
            for case, wheel in packages:
                run(['uv', 'pip', 'install', '--python', python, '--no-deps', wheel], work, env, case['id']+'-install')
            products = []
            for case, _ in packages:
                if args.case and case['id'] not in args.case:
                    continue
                entry = inputs/(case['id']+'.py')
                shutil.copy2(HERE/case['entry'], entry)
                oracle = run([python, '-I', '-B', entry], work, env, case['id']+'-oracle')
                assert not oracle.stderr
                for mode in args.mode or ('jit', 'standalone', 'onefile'):
                    label = case['id']+'-'+mode
                    output = work/('build-'+label)
                    command = [args.cli, '--cpython-provider', 'auto', '--experimental-stock-thin-functions',
                               'build', entry, '--out-dir', output]
                    if mode != 'jit': command.append('--aot' if mode == 'standalone' else '--onefile')
                    built = run(command, work, env, label+'-build')
                    data = provider_data(output, entry.stem, mode)
                    report = json.loads((data/'cpython-provider.json').read_text())
                    catalog = audit_catalog(report, case)
                    (evidence/(label+'-catalog.json')).write_text(json.dumps(catalog, ensure_ascii=False, indent=2)+'\n')
                    relocated = work/('已搬迁 产品 '+label)
                    if mode == 'standalone':
                        layout = standalone_layout(built.stderr, output, case['id'], rid())
                        shutil.move(layout.dist, relocated); executable = relocated/layout.native.name
                    elif mode == 'onefile':
                        executable = audit_onefile_output(output, case['id'])
                        relocated.mkdir(); shutil.move(executable, relocated/executable.name)
                        executable = relocated/executable.name
                    else:
                        public = built_product(built.stderr, output, entry.stem, 'jit')
                        filename = public.entry.name
                        shutil.move(output, relocated); executable = relocated/filename
                    if output.exists(): shutil.rmtree(output)
                    if mode != 'onefile': audit_native_paths(relocated)
                    products.append((label, mode, executable, oracle))
            # 所有模式都完成后物理删除原C源码副本、wheel、venv与应用输入，再执行已搬迁产品。
            shutil.rmtree(inputs)
            stream.write('INPUTS_REMOVED '+str(not inputs.exists())+'\n'); stream.flush()
            for label, mode, executable, oracle in products:
                runtime_temp = work/(label+'-runtime-temp'); runtime_temp.mkdir()
                report = work/(label+'-host.json')
                run_env = environment()
                run_env.update(PATH='', PYTHONHOME='/missing-python-home', PYTHONPATH='/missing-python-path',
                               TMPDIR=str(runtime_temp), RYUPY_CPYTHON_REPORT=str(report))
                command = [executable]
                actual = recorder.run(command, work, run_env)
                if report.exists(): shutil.copy2(report, evidence/(label+'-host.json'))
                assert (actual.returncode, actual.stdout, actual.stderr) == (oracle.returncode, oracle.stdout, oracle.stderr), label
                report_root = executable.parent
                if mode == 'onefile':
                    raw = json.loads(report.read_text())
                    report_root = Path(os.path.commonpath((raw['python_library'], raw['shim_library'])))
                    assert report_root.is_relative_to(runtime_temp.resolve())
                    assert not list(runtime_temp.iterdir())
                observed = inspect_host_report(report, report_root)
                thin = observed['stock_compiled_program']
                assert thin['terminal'] and thin['permission']
                assert (thin['native_calls'], thin['destroyed']) == (2, 2), thin
                assert all(thin[key] == 0 for key in ('native_live', 'core_roots', 'owned_h', 'ledger', 'queue',
                           'late_releases', 'post_finalize_decrefs', 'finalize_result'))
                results.append({'case': label, 'status': 'deleted_inputs_relocated_pass',
                                'callback_execution': 'static_cil', 'interface_and_runtime_negatives': 'pass'})
                print('PASS', label, flush=True)
        stream.write('CLEANED\n')
    (evidence/'results.json').write_text(json.dumps(results, ensure_ascii=False, indent=2)+'\n')


if __name__ == '__main__':
    main()
