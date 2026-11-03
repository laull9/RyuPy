"""恢复版：受信自有C源码，6次C工具构建，不加载未知ABI。"""
from pathlib import Path
from cabi_probe.probe import tool, digest, archive_objects, librarian_version, xcode_version


def build_c(run, source, output, rid, records):
    output.mkdir()
    win = rid == 'win-x64'
    obj = output/('probe.obj' if win else 'probe.o')
    archive = output/('probe_static.lib' if win else 'libprobe_static.a')
    name = 'ryupy_cabi_probe.dll' if win else 'libryupy_cabi_probe'+('.dylib' if rid == 'osx-arm64' else '.so')
    shared, reference = output/name, output/('reference.exe' if win else 'reference')
    missing, decoy = output/('missing-'+name), output/('decoy-'+name)
    def command(label, args, timeout=120, check=True):
        return run.run(label, list(map(str,args)), cwd=output, check=check, timeout=timeout)
    if win:
        cc, ar, symbols = tool('cl'), tool('lib'), tool('dumpbin')
        v = command('cc-version', [cc], 30, False)
        assert v.returncode in (0, 2) and b'Compiler Version' in v.stdout+v.stderr
        v = command('archive-version', [ar, '/?'], 30, False)
        records['archive_version'] = librarian_version(v.returncode, v.stdout, v.stderr)
        command('c-object', [cc, '/nologo', '/utf-8', '/O2', '/MT', '/c', source, '/Fo'+str(obj)])
        command('c-archive', [ar, '/NOLOGO', '/OUT:'+str(archive), obj])
        command('c-reference-build', [cc, '/nologo', '/utf-8', '/O2', '/MT', '/DRYUPY_REFERENCE', source,
                '/Fo'+str(output/'ref.obj'), '/Fe'+str(reference)])
        def shared_build(label, c, dest, defines):
            command(label, [cc, '/nologo', '/utf-8', '/O2', '/MT', '/DRYUPY_SHARED', *defines, '/LD', c,
                '/Fo'+str(output/(label+'.obj')), '/Fe'+str(dest), '/link', '/IMPLIB:'+str(output/(label+'.lib'))])
        shared_build('c-shared', source, shared, [])
        shared_build('c-missing-symbol', source, missing, ['/Dryupy_demo_add_i32=ryupy_other_i32'])
    else:
        cc, ar, symbols = tool('cc'), tool('ar'), tool('nm')
        command('cc-version', [cc, '--version'], 30)
        if rid == 'osx-arm64':
            ar = command('ar-path', [tool('xcrun'), '--find', 'ar'], 30).stdout.decode().strip()
            assert Path(ar).is_absolute() and Path(ar).is_file() and '\n' not in ar
            records['archive_version'] = xcode_version(command('xcode-version', [tool('xcodebuild'), '-version'], 30).stdout)
        else:
            command('archive-version', [ar, '--version'], 30)
        # 对齐 .NET 10 NativeAOT AppleMinOSVersion 默认值，仅约束自有测试输入。
        deployment = ['-mmacosx-version-min=12.0'] if rid == 'osx-arm64' else []
        if deployment: records['fixture_macos_deployment_target'] = '12.0'
        command('c-object', [cc, *deployment, '-O2', '-c', source, '-o', obj])
        command('c-archive', [ar, 'rcs', archive, obj])
        command('c-reference-build', [cc, *deployment, '-O2', '-DRYUPY_REFERENCE', source, '-o', reference])
        def shared_build(label, c, dest, defines):
            command(label, [cc, *deployment, '-O2', '-fPIC', '-DRYUPY_SHARED', *defines,
                '-dynamiclib' if rid == 'osx-arm64' else '-shared', c, '-o', dest])
        shared_build('c-shared', source, shared, [])
        shared_build('c-missing-symbol', source, missing, ['-Dryupy_demo_add_i32=ryupy_other_i32'])
    original = source.read_bytes(); needle = b'return (int64_t)left + (int64_t)right;'
    assert original.count(needle) == 1
    variant = output/'decoy.c'; variant.write_bytes(original.replace(needle, b'return INT64_C(7777777);'))
    shared_build('c-decoy', variant, decoy, [])
    records['c_inputs'] = {key:digest(path) for key,path in [('source',source),('decoy_source',variant),
        ('archive',archive),('shared',shared),('missing_symbol',missing),('decoy',decoy)]}
    records['archive_objects'] = archive_objects(archive,rid)
    command('archive-symbols', [symbols, '/SYMBOLS' if win else '-g', archive])
    command('shared-symbols', [symbols, '/EXPORTS' if win else '-g', shared])
    return dict(shared=shared, archive=archive, reference=reference, missing=missing, decoy=decoy, library_name=name)
