#!/usr/bin/env python3
"""通用Mach-O目标slice真实产品门：双独立fat扩展、签名授权、原生调用与资源保持。"""
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

sys.dont_write_bytecode = True
from fat_fixtures import HERE, prepare, RESOURCE, entitlements, signature_metadata

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
        with tempfile.TemporaryDirectory(prefix='ryupy-macho-slices-', dir='/tmp') as temporary:
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


def audit_convergence(report,case):
    """源universal事实与最终thin库存同时核对，普通库不取得Python执行身份。"""
    audit=report['native_architecture_convergence']
    assert audit['scope']=='existing_native_relocation_owned_images_only' and audit['target_rid']=='osx-arm64'
    prefix='python/lib/python3.12/site-packages/'+case['directory']+'/'
    selected=[row for row in audit['files'] if row['path'].startswith(prefix)]
    assert len(selected)==3,selected
    sizes={row['path']:row['bytes'] for row in report['files']}
    for row in selected:
        assert row['action']=='selected_target_slice' and row['output_architecture']=='arm64_all',row
        expected_container='fat64' if case['id']=='multiphase' and not row['path'].endswith('/native_helper') else 'fat32'
        assert row['source_container']==expected_container,row
        assert {part['cpu_type'] for part in row['source_fat_table']}=={0x1000007,0x100000c}
        assert row['selected_slice']['subtype']==0 and row['source_bytes']>row['output_bytes']>0
        assert row['output_bytes']==sizes[row['path']]
        expected_kind=2 if row['path'].endswith('/native_helper') else 8 if case['id']=='multiphase' and '/'+case['extension'] in row['path'] else 6
        assert row['verified_target_filetype']==expected_kind
    return audit


def dependency_root(root):
    """provider的相对路径以实际config/report目录为锚，canonical时为exact internal。"""
    candidate=root/'internal'
    return candidate if (candidate/'ryupy-cpython.json').is_file() else root


def audit_signatures(recorder,case,root,work,env):
    """真正codesign验签、授权元数据与部署头；loader/回调随后由实际搬迁进程消费。"""
    def run(command,cwd,run_env,label):
        return require_success(recorder.run(command,cwd,run_env),label)
    directory=dependency_root(root)/'python/lib/python3.12/site-packages'/case['directory']
    binaries=[*directory.glob('*.so'),directory/'native_helper']
    assert len(binaries)==3,binaries
    for binary in binaries:
        with binary.open('rb') as file: header=file.read(12)
        assert header[:4]==bytes.fromhex('cffaedfe')
        assert int.from_bytes(header[4:8],'little')==0x100000c and int.from_bytes(header[8:12],'little')==0
        run(['codesign','--verify','--strict',binary],work,env,'deployed-signature')
        helper=binary.name=='native_helper'
        expected={} if helper else {'org.ryupy.fixture':case['id'],'com.apple.security.cs.disable-library-validation':True}
        metadata=signature_metadata(binary)
        assert metadata['flags']&0x10000
        assert metadata['identifier']=='org.ryupy.fixture.'+('helper.' if helper else '')+case['id']
        assert entitlements(run,binary,work,env,'deployed-entitlements')==expected
    wheel=list((dependency_root(root)/'python/lib/python3.12/site-packages').glob(case['distribution']+'-*.dist-info/WHEEL'))
    assert len(wheel)==1 and 'universal2' in wheel[0].read_text()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cli', type=Path, default=ROOT/'target/debug/ryupy')
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--mode', choices=('jit', 'standalone', 'onefile'), action='append')
    parser.add_argument('--case', choices=('legacy', 'multiphase'), action='append')
    parser.add_argument('--expect-unconverged', action='store_true', help='旧CLI红证：要求目标收敛报告缺失且部署扩展仍为fat')
    args = parser.parse_args()
    args.python = args.python.resolve(); args.cli = args.cli.resolve()
    version = subprocess.check_output([args.python, '-I', '-B', '-c',
        'import platform; print(platform.python_version())'], text=True).strip()
    if version != '3.12.13':
        raise RuntimeError('此门显式固定CPython3.12.13；其它版本另行运行并登记')
    evidence = Path.home()/'.cache/ryupy/validation/macho-slices'/datetime.now(timezone.utc).strftime('%Y%m%dT%H%M%S.%fZ')
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
                entry.write_text(entry.read_text()+'''
import sys
import subprocess
from pathlib import Path
print(subprocess.check_output([str(Path(native.__file__).with_name('native_helper'))]).decode().strip())
if len(sys.argv)>1:
    raw=Path(__file__).with_name('preserved-fat.bin').read_bytes()
    print('raw-fat-resource',raw[:4].hex(),len(raw))
''')
                for mode in args.mode or ('jit', 'standalone', 'onefile'):
                    label = case['id']+'-'+mode
                    application_args=[] if mode=='jit' else ['resource-check']
                    oracle = run([python, '-B', entry, *application_args], work, env, label+'-oracle')
                    assert not oracle.stderr
                    output = work/('build-'+label)
                    command = [args.cli, '--cpython-provider', 'auto', '--experimental-stock-thin-functions',
                               'build', entry, '--out-dir', output]
                    if mode != 'jit':
                        command.append('--aot' if mode == 'standalone' else '--onefile')
                        resource_list=inputs/(case['id']+'-resources.json')
                        resource_list.write_text(json.dumps({'schema_version':1,'modules':[{'module':'__main__','files':[RESOURCE]}]}))
                        command.extend(['--resources',resource_list])
                    built = run(command, work, env, label+'-build')
                    data = provider_data(output, entry.stem, mode)
                    report = json.loads((data/'cpython-provider.json').read_text())
                    if args.expect_unconverged:
                        assert 'native_architecture_convergence' not in report
                        binary=next((data/'python/lib/python3.12/site-packages'/case['directory']).glob(case['extension']+'*.so'))
                        assert binary.read_bytes()[:4] in (bytes.fromhex('cafebabe'),bytes.fromhex('cafebabf'))
                        print('EXPECTED_OLD_UNCONVERGED',label,flush=True)
                        continue
                    catalog = audit_catalog(report, case)
                    audit_convergence(report,case)
                    (evidence/(label+'-catalog.json')).write_text(json.dumps(catalog, ensure_ascii=False, indent=2)+'\n')
                    if mode=='onefile':
                        dist_candidates=list(output.rglob(case['id']+'.dist'))
                        assert len(dist_candidates)==1,dist_candidates
                        audit_signatures(recorder,case,dist_candidates[0],work,env)
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
                    if mode!='onefile': audit_signatures(recorder,case,product_layout(relocated).data,work,env)
                    products.append((label, mode, executable, oracle, application_args))
            # 所有模式都完成后物理删除原C源码副本、wheel、venv与应用输入，再执行已搬迁产品。
            shutil.rmtree(inputs)
            stream.write('INPUTS_REMOVED '+str(not inputs.exists())+'\n'); stream.flush()
            for label, mode, executable, oracle, application_args in products:
                runtime_temp = work/(label+'-runtime-temp'); runtime_temp.mkdir()
                report = work/(label+'-host.json')
                run_env = environment()
                run_env.update(PATH='', PYTHONHOME='/missing-python-home', PYTHONPATH='/missing-python-path',
                               TMPDIR=str(runtime_temp), RYUPY_CPYTHON_REPORT=str(report))
                command = [executable]
                actual = recorder.run([*command,*application_args], work, run_env)
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
