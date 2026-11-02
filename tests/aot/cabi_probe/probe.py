#!/usr/bin/env python3
"""隔离固定 C ABI 机制验收；无公开 CLI/binding/schema，无性能采样。"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import platform
import re
import shutil
import sys
import tempfile
import time
from xml.sax.saxutils import escape

EXPECTED = b'0\n3\n4294967294\n-4294967296\n-1\n'
MODULE = 'ryupy_cabi_probe'


def digest(path):
    data = path.read_bytes()
    return {'name': path.name, 'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()}


def tool(name):
    value = shutil.which(name)
    if not value:
        raise FileNotFoundError(name)
    # cargo 常是 rustup 代理：保留 argv[0]，不解析符号链接。
    return os.path.abspath(value)


def librarian_version(returncode, stdout, stderr):
    """lib /? 的已实证帮助退出码不等于编译失败；必须有真实数字版本横幅。"""
    assert returncode in (0, 1100), 'unexpected lib help exit code'
    match = re.search(rb'Microsoft \(R\) Library Manager Version ([0-9]+(?:\.[0-9]+)+)', stdout + stderr)
    assert match, 'missing Microsoft Library Manager version banner'
    return match.group(1).decode('ascii')


def xcode_version(stdout):
    """记录所选 Xcode 工具链版本，不把它冒充 ar 自身版本号。"""
    text = stdout.decode('utf-8', errors='strict').strip()
    assert re.fullmatch(r'Xcode [0-9]+(?:\.[0-9]+)*\r?\nBuild version [A-Za-z0-9]+', text), 'invalid Xcode version evidence'
    return text


def archive_objects(path, rid):
    """检查真实 ar/COFF archive 的对象成员，而非把 Windows import .lib 当 static。"""
    data = path.read_bytes()
    if not data.startswith(b'!<arch>\n'):
        raise AssertionError('not a regular static archive')
    position, objects = 8, []
    while position < len(data):
        header = data[position:position+60]
        if len(header) != 60 or header[58:] != b'`\n':
            raise AssertionError('bad archive header')
        size = int(header[48:58].strip())
        assert 0 <= size <= len(data) - position - 60, 'invalid archive member size'
        name = header[:16].decode('ascii').strip()
        payload = data[position+60:position+60+size]
        if len(payload) != size:
            raise AssertionError('truncated archive')
        if name.startswith('#1/'):
            length = int(name[3:]); name = payload[:length].rstrip(b'\0').decode('utf-8')
            payload = payload[length:]
        if name not in ('/', '//', '/SYM64/') and not name.startswith('__.SYMDEF'):
            if rid == 'win-x64':
                assert payload[:2] == b'\x64\x86', 'not AMD64 COFF object (import library forbidden)'
            elif rid == 'linux-x64':
                assert payload[:6] == b'\x7fELF\x02\x01' and payload[16:20] == b'\x01\x00\x3e\x00'
            else:
                assert payload[:4] == b'\xcf\xfa\xed\xfe' and payload[4:8] == b'\x0c\x00\x00\x01'
                assert payload[12:16] == b'\x01\x00\x00\x00', 'not relocatable arm64 object'
            objects.append({'member': name, 'bytes': len(payload), 'sha256': hashlib.sha256(payload).hexdigest()})
        position += 60 + size + (size & 1)
    assert position == len(data) and len(objects) == 1, 'expected exactly one owned C object'
    return objects


def main():
    ap = argparse.ArgumentParser(description=__doc__)
    ap.add_argument('--repo', type=Path, required=True)
    ap.add_argument('--out', type=Path, required=True)
    ap.add_argument('--rid', choices=['linux-x64', 'osx-arm64', 'win-x64'], required=True)
    args = ap.parse_args()
    start = time.monotonic()
    sys.path.insert(0, str(args.repo.resolve()/'tests/aot'))
    from platform_runner import Runner
    from platform_support import host, clean_environment
    host_info = host(args.rid)
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='cabi-probe-', dir=args.out.resolve()))
    logs = work/'logs'; logs.mkdir()
    run = Runner(logs, deadline=start+1800)
    print(work, flush=True)
    records = {'scope': 'fixed C ABI mechanism only', 'host': host_info, 'rid': args.rid,
               'budget_seconds': 1800, 'public_binding': False, 'aot_shared': False,
               'python_extension_abi': False, 'ci_source_commit': os.environ.get('GITHUB_SHA'),
               'behaviors': {}, 'hashes': {}}
    def command(label, argv, cwd=work, timeout=180):
        return run.run(label, list(map(str, argv)), cwd=cwd, check=True, timeout=timeout)
    def behavior(label, argv, *, env=None):
        result = run.run(label, list(map(str, argv)), cwd=work, env=env, check=True, timeout=30)
        expected = EXPECTED.replace(b'\n', b'\r\n') if os.name == 'nt' else EXPECTED
        assert result.stdout == expected and result.stderr == b'', label
        records['behaviors'][label] = {'exit': result.returncode,
            'stdout_sha256': hashlib.sha256(result.stdout).hexdigest(),
            'stderr_sha256': hashlib.sha256(result.stderr).hexdigest()}
    try:
        records['hashes']['emitter_source'] = digest(args.repo/'crates/ryupy-codegen-cil/examples/cabi_emit_probe.rs')
        records['hashes']['harness_source'] = digest(Path(__file__))
        source = work/'source'; source.mkdir()
        c = source/'probe.c'; shutil.copy2(Path(__file__).with_name('probe.c'), c)
        records['hashes']['c_source'] = digest(c)
        native = work/'c-products'; native.mkdir()
        jit = work/'jit'; jit.mkdir()
        is_win = args.rid == 'win-x64'
        ext = '.exe' if is_win else ''
        obj = native/('probe.obj' if is_win else 'probe.o')
        archive = native/('probe_static.lib' if is_win else 'libprobe_static.a')
        shared = jit/('ryupy_cabi_probe.dll' if is_win else
                      'libryupy_cabi_probe.dylib' if args.rid == 'osx-arm64' else 'libryupy_cabi_probe.so')
        reference = native/('reference'+ext)
        if is_win:
            cc, librarian, symbols = tool('cl'), tool('lib'), tool('dumpbin')
            # cl 无输入通常返回 2；保留真实退出码及双流，不能把缺输入当编译失败。
            version = run.run('cc-version', [cc], cwd=work, timeout=30)
            assert version.returncode in (0, 2) and b'Compiler Version' in version.stdout + version.stderr
            version = run.run('archive-tool-version', [librarian, '/?'], cwd=work, timeout=30)
            records['archive_tool_version'] = librarian_version(version.returncode, version.stdout, version.stderr)
            command('c-static-object', [cc, '/nologo', '/utf-8', '/O2', '/MT', '/c', c, '/Fo'+str(obj)])
            command('c-static-archive', [librarian, '/NOLOGO', '/OUT:'+str(archive), obj])
            command('c-shared', [cc, '/nologo', '/utf-8', '/O2', '/MT', '/DRYUPY_SHARED', '/LD', c,
                    '/Fo'+str(native/'shared.obj'), '/Fe'+str(shared), '/link', '/IMPLIB:'+str(native/'shared_import.lib')])
            command('c-reference-build', [cc, '/nologo', '/utf-8', '/O2', '/MT', '/DRYUPY_REFERENCE', c,
                    '/Fo'+str(native/'reference.obj'), '/Fe'+str(reference)])
            command('archive-symbols', [symbols, '/SYMBOLS', archive])
            command('shared-symbols', [symbols, '/EXPORTS', shared])
        else:
            cc, librarian, symbols = tool('cc'), tool('ar'), tool('nm')
            command('cc-version', [cc, '--version'], timeout=30)
            if args.rid == 'osx-arm64':
                selected = command('archive-tool-path', [tool('xcrun'), '--find', 'ar'], timeout=30)
                librarian = selected.stdout.decode('utf-8', errors='strict').strip()
                assert Path(librarian).is_absolute() and Path(librarian).is_file() and '\n' not in librarian
                version = command('xcode-version', [tool('xcodebuild'), '-version'], timeout=30)
                records['archive_toolchain'] = {'xcode': xcode_version(version.stdout),
                    'ar_path': librarian, 'ar_file': digest(Path(librarian))}
            else:
                command('archive-tool-version', [librarian, '--version'], timeout=30)
            command('c-static-object', [cc, '-O2', '-c', c, '-o', obj])
            command('c-static-archive', [librarian, 'rcs', archive, obj])
            kind = '-dynamiclib' if args.rid == 'osx-arm64' else '-shared'
            command('c-shared', [cc, '-O2', '-fPIC', '-DRYUPY_SHARED', kind, c, '-o', shared])
            command('c-reference-build', [cc, '-O2', '-DRYUPY_REFERENCE', c, '-o', reference])
            command('archive-symbols', [symbols, '-g', archive])
            command('shared-symbols', [symbols, '-g', shared])
        records['archive_objects'] = archive_objects(archive, args.rid)
        shutil.copy2(archive, logs/archive.name)  # 保留不可动态加载的静态输入，供独立核对 archive。
        records['hashes'].update({k: digest(p) for k,p in [('static_archive',archive),('shared_library',shared)]})
        behavior('c-reference', [reference])
        target = work/'emitter-target'
        command('emitter-build', [tool('cargo'), 'build', '--locked', '-p', 'ryupy-codegen-cil',
                '--example', 'cabi_emit_probe', '--target-dir', target], cwd=args.repo.resolve(), timeout=180)
        emit = target/'debug'/'examples'/('cabi_emit_probe'+ext)
        # 发射器要求空目录；发射时共享库产物单独存放。
        il = work/'emitted'; il.mkdir()
        command('emit-readback', [emit, il])
        for name in ('RyupyCabiProbe.dll', 'RyupyCabiProbe.runtimeconfig.json', 'metadata.txt'):
            path = il/name; assert path.is_file(); records['hashes'][name] = digest(path)
            shutil.copy2(path, jit/name)
        metadata = (il/'metadata.txt').read_text(encoding='utf-8')
        assert 'readback_verified=true\n' in metadata and 'native_execution_verified=false\n' in metadata
        shutil.copy2(il/'metadata.txt', logs/'metadata.txt')
        dotnet = tool('dotnet')
        command('sdk-version', [dotnet, '--version'])
        behavior('jit-shared', [dotnet, jit/'RyupyCabiProbe.dll'])
        project = work/'host'; project.mkdir()
        # 仅以静态可达调用保留生成程序集，不添加广泛 root 描述。
        (project/'Program.cs').write_text('return RyuPy.CabiProbe.Program.Main(args);\n', encoding='utf-8')
        def xmlpath(path):
            value = str(path)
            for a,b in [('%','%25'),('$','%24'),('@','%40'),(';','%3B'),("'",'%27'),('(','%28'),(')','%29')]:
                value = value.replace(a,b)
            return escape(value, {'"':'&quot;'})
        (project/'host.csproj').write_text(f'''<Project Sdk="Microsoft.NET.Sdk">
<PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework>
<AssemblyName>CabiStaticHost</AssemblyName><PublishAot>true</PublishAot><StripSymbols>false</StripSymbols>
<TreatWarningsAsErrors>true</TreatWarningsAsErrors><IlcTreatWarningsAsErrors>true</IlcTreatWarningsAsErrors>
<ILLinkTreatWarningsAsErrors>true</ILLinkTreatWarningsAsErrors>
<ImportDirectoryBuildProps>false</ImportDirectoryBuildProps><ImportDirectoryBuildTargets>false</ImportDirectoryBuildTargets>
<EnableDefaultCompileItems>false</EnableDefaultCompileItems>
</PropertyGroup><ItemGroup><Compile Include="Program.cs"/>
<Reference Include="RyupyCabiProbe"><HintPath>{xmlpath(il/'RyupyCabiProbe.dll')}</HintPath></Reference>
<DirectPInvoke Include="{MODULE}"/><NativeLibrary Include="{xmlpath(archive)}"/>
</ItemGroup></Project>''', encoding='utf-8')
        publish = work/'publish'
        published = command('aot-static-publish', [dotnet, 'publish', project/'host.csproj', '-c', 'Release', '-r', args.rid,
                '-o', publish, '--nologo', '-p:ImportDirectoryBuildProps=false',
                '-p:ImportDirectoryBuildTargets=false'], timeout=600)
        assert b'Generating native code' in published.stdout, 'SDK did not report real ILC generation'
        assert published.stderr == b'', 'unexpected SDK stderr'
        binary = publish/('CabiStaticHost'+ext); assert binary.is_file()
        moved = work/'relocated'; moved.mkdir()
        deployed = moved/binary.name; shutil.copy2(binary, deployed)
        records['hashes']['static_native'] = digest(deployed)
        if is_win:
            deps = command('native-dependencies', [symbols, '/DEPENDENTS', deployed])
            command('native-symbols', [symbols, '/SYMBOLS', deployed])
        elif args.rid == 'osx-arm64':
            deps = command('native-dependencies', [tool('otool'), '-L', deployed])
            command('native-symbols', [symbols, '-g', deployed])
        else:
            deps = command('native-dependencies', [tool('readelf'), '-d', deployed])
            command('native-symbols', [symbols, '-g', deployed])
        assert MODULE.encode() not in deps.stdout.lower(), 'static native still imports probe shared library'
        # 保留日志、哈希及非运行依赖的 archive 证据；删除源码、共享库、生成 IL 和构建树。
        for directory in (source, native, jit, il, project, publish, target):
            shutil.rmtree(directory)
        records['removed_owned_inputs'] = True
        behavior('aot-static-relocated', [deployed], env=clean_environment(work))
        records['passed'] = True
    finally:
        records['elapsed_seconds'] = time.monotonic()-start
        (work/'result.json').write_text(json.dumps(records, indent=2), encoding='utf-8')


if __name__ == '__main__':
    main()
