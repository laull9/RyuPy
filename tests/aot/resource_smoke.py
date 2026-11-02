#!/usr/bin/env python3
"""每宿主一次资源AOT发布；固定源码，无后端专用Python分支。"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import tempfile
import time
from smoke import executable, IMAGE
from platform_runner import Runner
from platform_support import host, audit, clean_environment
from platform_smoke import published_platform_native, rustc_executable
from manifest_verify import (ManifestError, published_manifest, read_sdk_version,
    relocate_manifest, verify_manifest, dependency_binaries)


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def check_fixture_sha(path, relative, expected):
    actual = sha(path)
    if actual != expected:
        raise AssertionError(f'fixture字节不一致：relative={relative!r}, '
                             f'expected_sha256={expected}, actual_sha256={actual}')


def expected_stdout(text):
    # fixture期望用LF存储；运行前按宿主生成期待字节，不归一化任何实际输出。
    assert '\r' not in text
    return text.replace('\n', os.linesep).encode('utf-8')


def equal(reference, actual):
    if (actual.returncode, actual.stdout, actual.stderr) != (
            reference.returncode, reference.stdout, reference.stderr):
        raise AssertionError('stdout/stderr/exit与固定CP参考不一致；见原始日志')
    if actual.returncode != 0:
        raise AssertionError('成功模式不能双方同非零算通过')


def missing_signature(result):
    # 只忽略平台绝对路径/源码展示；保全部Python帧与错误类型，不声称错误文案相同。
    text = result.stderr.decode('utf-8')
    frames = [(name.replace('\\', '/').rsplit('/', 1)[-1], int(line), function)
              for name, line, function in re.findall(r'File "([^"]+)", line (\d+), in ([^\n]+)', text)]
    if (result.returncode != 1 or result.stdout != expected_stdout('hello\n') or not frames
            or not text.rstrip().splitlines()[-1].startswith('FileNotFoundError:')
            or 'Unhandled exception' in text):
        raise AssertionError('缺数据没有产生完整Python FileNotFoundError报告')
    return frames, 'FileNotFoundError'


def owned_remove(path, work):
    if path.is_symlink() or path.resolve().parent != work.resolve():
        raise AssertionError('拒绝删除非本轮直接owned目录')
    shutil.rmtree(path)
    assert not path.exists()


def check_toolchains(run, work, env, python, dotnet, rid):
    cp = run.run('python-version', [python, '-c',
        "import sys; print(sys.implementation.name); print('.'.join(map(str,sys.version_info[:3])))"],
        cwd=work, env=env, check=True, timeout=30)
    assert cp.stdout.decode().splitlines() == ['cpython', '3.12.13']
    info = run.run('dotnet-info', [dotnet, '--info'], cwd=work, env=env, check=True, timeout=30)
    text = info.stdout.decode('utf-8-sig')
    architectures = re.findall(r'^\s*Architecture:\s*(\S+)\s*$', text, re.M)
    rids = re.findall(r'^\s*RID:\s*(\S+)\s*$', text, re.M)
    assert architectures and all(x == ('arm64' if rid == 'osx-arm64' else 'x64') for x in architectures)
    assert rids == [rid]
    rust = run.run('rustc-version', [rustc_executable(), '-vV'], cwd=work, env=env, check=True, timeout=30)
    rust_host = re.findall(r'^host: (\S+)\s*$', rust.stdout.decode(), re.M)
    wanted = {'linux-x64':'x86_64-unknown-linux-gnu', 'osx-arm64':'aarch64-apple-darwin',
              'win-x64':'x86_64-pc-windows-msvc'}[rid]
    assert rust_host == [wanted]
    return {'dotnet_architectures':architectures,'dotnet_rids':rids,'rust_host':wanted}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--out', required=True, type=Path)
    parser.add_argument('--python', default='python3.12')
    parser.add_argument('--dotnet', default=os.environ.get('DOTNET','dotnet'))
    parser.add_argument('--ryupy', required=True)
    parser.add_argument('--rid', required=True, choices=['linux-x64','osx-arm64','win-x64'])
    args = parser.parse_args()
    metadata = host(args.rid)
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='aot-resource-', dir=args.out.resolve()))
    logs = work/'logs'; logs.mkdir()
    print(f'本轮资源验收：{work}', flush=True)
    run = Runner(logs, deadline=time.monotonic()+600)
    result = {'status':'failed','rid':args.rid,'host':metadata,
              'clean_machine_without_runtime_tested':False, 'publish_count':0}
    try:
        python, dotnet, ryupy = map(executable, (args.python,args.dotnet,args.ryupy))
        env = {**os.environ,'DOTNET':dotnet,'PYTHONUTF8':'1','PYTHONIOENCODING':'utf-8',
               'PYTHONDONTWRITEBYTECODE':'1','DOTNET_CLI_UI_LANGUAGE':'en-US'}
        result['toolchain_architecture'] = check_toolchains(run,work,env,python,dotnet,args.rid)
        result['cli_sha256'] = sha(Path(ryupy))
        fixtures = Path(__file__).with_name('resource_fixtures')
        expected = json.loads((fixtures/'expected.json').read_text(encoding='utf-8'))
        source, il = work/'源码 & inputs', work/'产物 & outputs'
        source.mkdir()
        result['source_hashes'] = expected['source_hashes']
        for relative, digest in expected['source_hashes'].items():
            original = fixtures/relative
            check_fixture_sha(original, relative, digest)
            destination = source/relative; destination.parent.mkdir(parents=True,exist_ok=True)
            shutil.copy2(original,destination)
        main_file, data_file = source/'main.py', source/'greetings/data.txt'
        original_data = data_file.read_bytes()
        references = {}
        run.run('compile', [ryupy,'build',str(main_file),'--out-dir',str(il)], cwd=work,env=env,check=True,timeout=120)
        result['jit_il_sha256'], result['runtime_sha256'] = sha(il/'main.dll'), sha(il/'RyuPy.Runtime.dll')
        for phase in ('original','modified'):
            if phase == 'modified':
                data_file.write_bytes('Changed source data: 再见\n'.encode('utf-8'))
            for mode in ('en','fr'):
                cp = run.run(f'python-{phase}-{mode}',[python,'-B',str(main_file),mode],cwd=work,env=env,timeout=30)
                assert cp.stdout == expected_stdout(expected[phase][mode]['stdout'])
                assert cp.returncode == 0 and cp.stderr == b''
                jit = run.run(f'jit-{phase}-{mode}',[dotnet,str(il/'main.dll'),mode],cwd=work,env=env,timeout=30)
                equal(cp,jit)
                if phase == 'original':
                    references[mode] = cp
        data_file.unlink()
        absent_cp = run.run('python-missing',[python,'-B',str(main_file),'en'],cwd=work,env=env,timeout=30)
        absent_signature = missing_signature(absent_cp)
        data_file.write_bytes(original_data)
        assert sha(data_file) == expected['source_hashes']['greetings/data.txt']
        result['publish_count'] += 1
        published = run.run('publish',[ryupy,'build',str(main_file),'--out-dir',str(il),'--aot',
            '--resources',str(source/'resources.json')],cwd=work,env=env,check=True,timeout=300)
        native = published_platform_native(published.stderr,il,args.rid)
        manifest_file = published_manifest(published.stderr,native)
        sdk = read_sdk_version(native.parent.parent/'host/sdk.version')
        result['aot_il_sha256'] = sha(il/'main.dll')
        assert sha(il/'RyuPy.Runtime.dll') == result['runtime_sha256']
        for name in ('publish.stdout.log','publish.stderr.log'):
            shutil.copy2(native.parent.parent/name,logs/('driver-'+name))
        shutil.copy2(manifest_file,logs/'ryupy-artifact.json')
        shutil.copy2(native.parent.parent/'host/sdk.version',logs/'sdk.version')
        relocated = work/'搬迁 & bundle'
        binary, manifest = relocate_manifest(native.parent,native,relocated,args.rid,sdk)
        assert manifest['schema_version'] == 2 and len(manifest['resources']) == 1
        resource = manifest['resources'][0]
        assert resource['sha256'] == expected['source_hashes']['greetings/data.txt']
        selected = {x['module_name'] for x in manifest['module_locations']}
        assert selected == {'greetings.english','greetings.french'}
        assert len(resource['consumer_module_ids']) == 2
        for location in manifest['module_locations']:
            assert not (relocated/location['relative_file']).exists()
        assert not list(relocated.rglob('*.py')) and not list(relocated.rglob('*.runtimeconfig.json'))
        assert not (relocated/'RyuPy.Runtime.dll').exists()
        paths = dependency_binaries(relocated,manifest)
        if args.rid == 'linux-x64':
            for index,path in enumerate(paths):
                run.run(f'ldd-{index}',[executable('ldd'),str(path)],cwd=work,check=True,timeout=30)
        else:
            tool = 'otool' if args.rid == 'osx-arm64' else 'dumpbin'
            result['dependencies'] = audit(run,relocated,args.rid,{tool:executable(tool)},binaries=paths)
        result['manifest_sha256'], result['native_sha256'] = sha(relocated/'ryupy-artifact.json'),sha(binary)
        result['sdk_version'], result['resources'] = sdk,manifest['resources']
        data_file.write_bytes('Changed source data: 再见\n'.encode('utf-8'))
        for directory in (source,il):
            owned_remove(directory,work)
        result['owned_build_inputs_removed'] = True
        clean = clean_environment(work)
        unrelated = work/'unrelated-cwd'; unrelated.mkdir()
        for mode in ('en','fr'):
            actual = run.run(f'native-{mode}',[str(binary),mode],cwd=unrelated,env=clean,timeout=30)
            equal(references[mode],actual)
        if args.rid == 'linux-x64':
            script = ('for tool in dotnet python python3 python3.12; do '
                'if command -v "$tool" >/dev/null 2>&1; then exit 97; fi; done; '
                'binary=$1; shift; exec "$binary" "$@"')
            for mode in ('en','fr'):
                actual = run.run(f'docker-{mode}',[executable('docker'),'run','--rm','--network','none',
                    '--read-only','--mount',f'type=bind,src={relocated},dst=/app,readonly',
                    '--workdir','/tmp','--entrypoint','/bin/sh',IMAGE,'-c',script,'resource-smoke',
                    '/app/'+binary.name,mode],cwd=work,timeout=30)
                equal(references[mode],actual)
            result['clean_machine_without_runtime_tested'] = True
        negative = work/'missing-data'; shutil.copytree(relocated,negative)
        (negative/resource['relative_path']).unlink()
        try:
            verify_manifest(negative,negative/binary.name,args.rid,sdk)
        except ManifestError as error:
            result['missing_manifest_rejection'] = str(error)
        else:
            raise AssertionError('缺资源清单未拒绝')
        actual = run.run('native-missing',[str(negative/binary.name),'en'],cwd=unrelated,env=clean,timeout=30)
        assert missing_signature(actual) == absent_signature
        result['status'] = 'passed'
    except BaseException as error:
        result['failure'] = repr(error)
        raise
    finally:
        (work/'result.json').write_text(json.dumps(result,ensure_ascii=False,indent=2)+'\n',encoding='utf-8')


if __name__ == '__main__':
    main()
