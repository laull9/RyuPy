#!/usr/bin/env python3
"""恢复版固定绑定验收：每host一JIT/一静态AOT，独立副本负例，不采样性能。"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import tempfile
import time
from binding_c import build_c
from binding_manifest import RELATIVE, verify, relocate
from cabi_probe.probe import tool
from manifest_files import ManifestError, digest, plain_stat
from manifest_verify import MANIFEST_NAME, dependency_binaries, read_sdk_version
from platform_runner import Runner
from platform_support import host, clean_environment, audit
from single_file import prepare_single_file, verify_single_file
from smoke import IMAGE


def file_digest(path):
    size, sha = digest(Path(path))
    return {'size':size, 'sha256':sha}


def published(stderr, output, aot):
    lines = stderr.decode('utf-8', errors='strict').splitlines()
    paths = [Path(line.removeprefix('已生成 ')) for line in lines if line.startswith('已生成 ')]
    selected = [p for p in paths if p.suffix.lower() != '.dll'] if aot else paths
    assert len(selected) == 1, 'CLI未报告唯一入口'
    entry = selected[0]; assert entry.is_absolute(); plain_stat(entry)
    if aot:
        publish = entry.parent; owned_aot = publish.parent; bundle = owned_aot.parent
        assert publish.name == 'publish' and owned_aot.name.startswith('ryupy-aot-')
    else:
        bundle = entry.parent
    owned = bundle.parent
    assert bundle.name == 'bundle' and owned.name.startswith('ryupy-binding-')
    assert os.path.samefile(owned.parent, output), '产物不属于本次输出根'
    prefix = 'AOT 清单：' if aot else '产物清单：'
    manifests = [Path(line.removeprefix(prefix)) for line in lines if line.startswith(prefix)]
    assert len(manifests) == 1 and manifests[0].is_absolute()
    plain_stat(manifests[0])
    assert os.path.samefile(manifests[0], entry.parent/MANIFEST_NAME)
    return entry


def failure_signature(result, mode):
    assert result.returncode == 1 and result.stdout == b'', '负例退出/输出不符'
    text = result.stderr.decode('utf-8', errors='strict')
    assert 'Unhandled exception' not in text
    # 只排除诊断路径前缀/源码展示，保留所有真实帧及最终错误。
    frames = [(Path(name).name, int(line), function.rstrip('\r')) for name,line,function in
              re.findall(r'File "([^"]+)", line (\d+), in ([^\n]+)', text)]
    expected_frames = [('binding_app.py',15,'<module>'), ('binding_app.py',9 if mode == 'type' else 11,'fail_call')]
    assert frames == expected_frames, frames
    expected = ("TypeError: add() argument 'left' must be an actual integer, not bool or a converted value" if mode == 'type'
                else "ImportError: cannot load declared native library 'ryupy_cabi_probe' symbol 'ryupy_demo_add_i32'")
    assert text.rstrip().splitlines()[-1] == expected, text
    return {'frames':frames, 'exception':expected}


def normal_stdout(numbers):
    newline = os.linesep.encode()
    markers = ['kind']*5 + ['range']*4 + ['arity0','arity1','arity3','posonly',
        'evaluate-before-check','left-before-right','callee-frozen','noncallable','binding-boundary-ok']
    return b'file-absent'+newline+numbers+newline.join(x.encode() for x in markers)+newline


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out', required=True, type=Path); parser.add_argument('--python', default='python3.12')
    parser.add_argument('--ryupy', required=True); parser.add_argument('--dotnet', default=os.environ.get('DOTNET','dotnet'))
    parser.add_argument('--rid', required=True, choices=list(RELATIVE)); args = parser.parse_args()
    start = time.monotonic(); metadata = host(args.rid)
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='python-binding-', dir=args.out.resolve()))
    logs = work/'logs'; logs.mkdir(); print(work, flush=True)
    run = Runner(logs, deadline=start+1800)
    result = {'status':'failed', 'rid':args.rid, 'host':metadata, 'budget_seconds':1800,
        'jit_build_count':0, 'aot_publish_count':0, 'applications':{}, 'references':{},
        'clean_machine_without_runtime_tested':False, 'ci_source_commit':os.environ.get('GITHUB_SHA')}
    def command(label, argv, *, cwd=work, env=None, timeout=120, check=True):
        return run.run(label, list(map(str,argv)), cwd=cwd, env=env, timeout=timeout, check=check)
    try:
        python, dotnet, ryupy = tool(args.python), tool(args.dotnet), tool(args.ryupy)
        env = {**os.environ, 'DOTNET':dotnet, 'PYTHONUTF8':'1', 'PYTHONIOENCODING':'utf-8', 'DOTNET_CLI_UI_LANGUAGE':'en-US'}
        command('python-version', [python,'-c',"import sys; assert sys.implementation.name=='cpython' and sys.version_info[:3]==(3,12,13); print(sys.version)"], env=env, timeout=30)
        command('dotnet-info', [dotnet,'--info'], env=env, timeout=30)
        if args.rid == 'linux-x64': command('docker-pull', [tool('docker'),'pull',IMAGE], env=env, timeout=120)
        result['cli'] = file_digest(ryupy)
        source = work/'source'; source.mkdir(); fixtures = Path(__file__).with_name('binding_fixtures')
        for name in ('binding_app.py','numeric_reference.py'): shutil.copy2(fixtures/name, source/name)
        shutil.copy2(Path(__file__).with_name('cabi_probe')/'probe.c', source/'probe.c')
        result['source_files'] = {p.name:file_digest(p) for p in source.iterdir()}
        products = build_c(run, source/'probe.c', work/'c-products', args.rid, result)
        cp = command('python-numeric', [python,source/'numeric_reference.py'], env=env, timeout=30)
        c = command('c-reference', [products['reference']], timeout=30)
        assert cp.stdout == c.stdout and cp.stderr == c.stderr == b''
        expected_numbers = '0\n3\n4294967294\n-4294967296\n-1\n'.replace('\n',os.linesep).encode()
        assert cp.stdout == expected_numbers
        result['references'] = {'python_exit':cp.returncode,'c_exit':c.returncode,'stdout_sha256':hashlib.sha256(cp.stdout).hexdigest()}
        declaration = {'schema_version':1,'module':'declared_demo','export':'add','signature':'cdecl_i32_i32_i64',
            'library':'ryupy_cabi_probe','symbol':'ryupy_demo_add_i32','target':{'rid':args.rid,
            'shared':products['shared'].relative_to(work).as_posix(),'archive':products['archive'].relative_to(work).as_posix()}}
        declaration_file = work/'binding.json'; declaration_file.write_bytes((json.dumps(declaration,indent=2)+'\n').encode())
        base = {key:declaration[key] for key in ('module','export','library','symbol')}
        base.update(declaration_sha256=file_digest(declaration_file)['sha256'],sources={'__main__':result['source_files']['binding_app.py']['sha256']})
        entry = source/'binding_app.py'; jit_out = work/'jit-output'; aot_out = work/'aot-output'
        result['jit_build_count'] += 1
        built = command('jit-build', [ryupy,'build',entry,'--out-dir',jit_out,'--native-binding',declaration_file], env=env, timeout=180)
        dll = published(built.stderr, jit_out, False)
        jit_expected = {**base,'native_input':file_digest(products['shared'])}
        moved_dll, jit_manifest = relocate(dll.parent,dll,work/'搬迁 & jit',args.rid,'jit_shared',None,jit_expected)
        result['runtime'] = next(x for x in jit_manifest['artifacts'] if x['role']=='runtime_managed')
        result['aot_publish_count'] += 1
        built = command('aot-publish', [ryupy,'build',entry,'--out-dir',aot_out,'--native-binding',declaration_file,'--aot'], env=env, timeout=600)
        native = published(built.stderr,aot_out,True); aot_root = native.parent.parent
        for name in ('publish.stdout.log','publish.stderr.log'): shutil.copy2(aot_root/name,logs/('driver-'+name))
        assert b'Generating native code' in (logs/'driver-publish.stdout.log').read_bytes(), '无真实ILC日志'
        sdk = read_sdk_version(aot_root/'host/sdk.version'); shutil.copy2(aot_root/'host/sdk.version',logs/'sdk.version')
        aot_expected = {**base,'native_input':file_digest(products['archive'])}
        moved_native, aot_manifest = relocate(native.parent,native,work/'搬迁 & native',args.rid,'aot_static',sdk,aot_expected)
        assert next(x for x in aot_manifest['build_inputs'] if x['role']=='runtime_managed_input')['sha256'] == result['runtime']['sha256']
        result['manifests'] = {'jit':file_digest(moved_dll.parent/MANIFEST_NAME),'aot':file_digest(moved_native.parent/MANIFEST_NAME)}
        result['sdk_version'] = sdk
        if args.rid == 'linux-x64':
            deps = command('native-dependencies',[tool('readelf'),'-d',moved_native])
            text = deps.stdout.decode('utf-8',errors='strict')
            assert not any(x in text.lower() for x in ('ryupy_cabi_probe','library.so','hostfxr','hostpolicy','coreclr','mscoree'))
            result['dependencies'] = text
        else:
            tools = {'otool':tool('otool')} if args.rid=='osx-arm64' else {'dumpbin':tool('dumpbin')}
            result['dependencies'] = audit(run,moved_native.parent,args.rid,tools,binaries=dependency_binaries(moved_native.parent,aot_manifest))
            assert not any('ryupy_cabi_probe' in dep.lower() or Path(dep).name.lower().startswith('library.')
                for item in result['dependencies'] for dep in item['dependencies'])
        # 原路径失效而证据保留；只移动本轮创建的根，不声称SDK/证据在宿主不可见。
        evidence = work/'build-evidence'; evidence.mkdir()
        for directory in (source,jit_out,aot_out):
            plain_stat(directory,directory=True); assert directory.resolve().parent==work.resolve()
            directory.rename(evidence/directory.name); assert not directory.exists()
        native_hash = next(x['sha256'] for x in aot_manifest['artifacts'] if x['role']=='native_executable')
        bare, result['bare_native'] = prepare_single_file(moved_native,work,(source,jit_out,aot_out),native_hash)
        clean = clean_environment(work); expected_stdout = normal_stdout(cp.stdout)
        def app(label, argv, *, cwd, app_env, mode=None):
            actual = command(label,argv,cwd=cwd,env=app_env,timeout=30,check=False)
            record = {'exit':actual.returncode,'stdout_sha256':hashlib.sha256(actual.stdout).hexdigest(),'stderr_sha256':hashlib.sha256(actual.stderr).hexdigest()}
            result['applications'][label] = record
            if mode: record['failure'] = failure_signature(actual,mode)
            else: assert actual.returncode==0 and actual.stdout==expected_stdout and actual.stderr==b'', label
            return actual
        app('jit-normal',[dotnet,moved_dll],cwd=moved_dll.parent,app_env=env)
        app('aot-normal',[bare],cwd=bare.parent,app_env=clean)
        for label,kind in [('jit-missing','missing'),('jit-missing-dynamicfalse','dynamicfalse'),('jit-missing-symbol','symbol'),('jit-bad-format','bad')]:
            copy_dll,_ = relocate(moved_dll.parent,moved_dll,work/label,args.rid,'jit_shared',None,jit_expected)
            library = copy_dll.parent/RELATIVE[args.rid]; case_env = dict(env); cwd = copy_dll.parent
            if kind in ('missing','dynamicfalse'):
                library.unlink(); cwd = work/(label+'-decoy'); cwd.mkdir()
                shutil.copy2(products['decoy'],cwd/products['library_name'])
                case_env['PATH'] = str(cwd)+os.pathsep+env.get('PATH','')
                variable = 'LD_LIBRARY_PATH' if args.rid=='linux-x64' else 'DYLD_LIBRARY_PATH' if args.rid=='osx-arm64' else None
                if variable: case_env[variable] = str(cwd)
                result.setdefault('decoy_environment',{})[label] = {'variable':variable,'value':str(cwd),
                    'mac_dyld_visibility_proven':False,'scope':'child env requested; no additional load probe'}
                if kind == 'dynamicfalse':
                    config = copy_dll.with_suffix('.runtimeconfig.json'); value = json.loads(config.read_text())
                    value['runtimeOptions'].setdefault('configProperties',{})['System.Runtime.CompilerServices.RuntimeFeature.IsDynamicCodeSupported'] = False
                    config.write_text(json.dumps(value),encoding='utf-8')
            elif kind == 'symbol': shutil.copy2(products['missing'],library)
            else: library.write_bytes(b'not a native library\n')
            try: verify(copy_dll.parent,copy_dll,args.rid,'jit_shared',None,jit_expected)
            except ManifestError as error:
                result.setdefault('tamper_rejected',{})[label] = {'error':str(error),'path':RELATIVE[args.rid],
                    'expected':jit_expected['native_input'],'actual':file_digest(library) if library.exists() else None}
            else: raise AssertionError('篡改副本未被清单拒绝')
            app(label,[dotnet,copy_dll,'load'],cwd=cwd,app_env=case_env,mode='load')
        recovered,_ = relocate(moved_dll.parent,moved_dll,work/'jit-new-good',args.rid,'jit_shared',None,jit_expected)
        app('jit-new-good',[dotnet,recovered],cwd=recovered.parent,app_env=env)
        app('jit-type',[dotnet,moved_dll,'type'],cwd=moved_dll.parent,app_env=env,mode='type')
        app('aot-type',[bare,'type'],cwd=bare.parent,app_env=clean,mode='type')
        if args.rid == 'linux-x64':
            script = 'for tool in dotnet python python3 python3.12; do if command -v "$tool" >/dev/null 2>&1; then exit 97; fi; done; exec "$1"'
            app('aot-clean-container',[tool('docker'),'run','--rm','--network','none','--read-only','--mount',
                f'type=bind,src={bare.parent},dst=/app,readonly','--workdir','/app','--entrypoint','/bin/sh',IMAGE,
                '-c',script,'binding-smoke','/app/'+bare.name],cwd=work,app_env=env)
            result['clean_machine_without_runtime_tested'] = True
        assert len(result['applications']) == (10 if args.rid=='linux-x64' else 9)
        result['bare_after'] = verify_single_file(bare,native_hash)
        verify(moved_dll.parent,moved_dll,args.rid,'jit_shared',None,jit_expected)
        verify(moved_native.parent,moved_native,args.rid,'aot_static',sdk,aot_expected)
        result['status'] = 'passed'
    except BaseException as error:
        result['failure'] = repr(error); raise
    finally:
        result['elapsed_seconds'] = time.monotonic()-start
        (work/'result.json').write_text(json.dumps(result,indent=2,ensure_ascii=False),encoding='utf-8')


if __name__ == '__main__': main()
