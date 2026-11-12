"""平台、格式和原生依赖审计；不是无SDK干净机器证明。"""
import hashlib
import os
from pathlib import Path
import platform
import re
import shutil
import struct


def host(rid):
    actual = (platform.system(), platform.machine().lower())
    wanted = {'osx-arm64': ('Darwin', ('arm64', 'aarch64')),
              'win-x64': ('Windows', ('amd64', 'x86_64')),
              'linux-x64': ('Linux', ('x86_64',))}[rid]
    if actual[0] != wanted[0] or actual[1] not in wanted[1]:
        raise RuntimeError(f'不接受交叉宿主：{actual} != {rid}')
    return {'os': actual[0], 'arch': actual[1], 'platform': platform.platform(),
            'image_os': os.environ.get('ImageOS'), 'image_version': os.environ.get('ImageVersion')}


def native_format(path, rid, *, library=False):
    data = path.read_bytes()
    if rid == 'osx-arm64':
        if len(data) < 32:
            raise AssertionError(f'截断Mach-O：{path}')
        magic, cpu, _, kind = struct.unpack_from('<IIII', data)
        if (magic, cpu, kind) != (0xfeedfacf, 0x100000c, 6 if library else 2):
            raise AssertionError(f'需要arm64 Mach-O原生格式：{path}')
    elif rid == 'win-x64':
        if len(data) < 64 or data[:2] != b'MZ':
            raise AssertionError(f'非PE：{path}')
        offset = struct.unpack_from('<I', data, 60)[0]
        if offset + 24 + 240 > len(data) or data[offset:offset+4] != b'PE\0\0':
            raise AssertionError(f'截断PE：{path}')
        machine = struct.unpack_from('<H', data, offset+4)[0]
        flags = struct.unpack_from('<H', data, offset+22)[0]
        optional = offset+24
        magic = struct.unpack_from('<H', data, optional)[0]
        directories = struct.unpack_from('<I', data, optional+108)[0]
        clr = struct.unpack_from('<II', data, optional+112+14*8)
        if (machine != 0x8664 or magic != 0x20b or bool(flags & 0x2000) != library
                or directories < 15 or clr != (0, 0)):
            raise AssertionError(f'需要AMD64无CLR目录原生PE：{path}')
    else:
        raise AssertionError('Linux继续使用原smoke格式协议')
    return {'name': path.name, 'bytes': len(data), 'sha256': hashlib.sha256(data).hexdigest()}


def relocate(native, relocated, rid):
    relocated.mkdir()
    binary = relocated / native.name
    shutil.copy2(native, binary)
    manifest = [native_format(binary, rid)]
    suffix = '.dylib' if rid == 'osx-arm64' else '.dll'
    for source in native.parent.iterdir():
        if source.is_file() and source.suffix.lower() == suffix:
            # 每个随包库必须原生；不把RyuPy托管DLL误当Windows合法依赖。
            native_format(source, rid, library=True)
            shutil.copy2(source, relocated/source.name)
            manifest.append(native_format(relocated/source.name, rid, library=True))
    return binary, manifest


def audit(run, relocated, rid, tools, *, binaries=None):
    records = []
    forbidden = ('hostfxr', 'hostpolicy', 'coreclr', 'clrcreateinstance', 'mscoree')
    # 新清单还包含JSON/符号；调用方显式传需审计的native/dynamic库集合。
    selected = relocated.iterdir() if binaries is None else binaries
    for index, path in enumerate(sorted(selected)):
        native_format(path, rid, library=path.suffix.lower() in ('.dll', '.dylib'))
        if rid == 'osx-arm64':
            result = run.run(f'dependency-{index}', [tools['otool'], '-L', str(path)], cwd=relocated, check=True)
            commands = run.run(f'load-commands-{index}', [tools['otool'], '-l', str(path)], cwd=relocated, check=True)
            text = result.stdout.decode('utf-8')
            rpaths = re.findall(r'cmd LC_RPATH\s+cmdsize \d+\s+path (.*?) \(offset', commands.stdout.decode('utf-8'))
            dependencies = [line.strip().split(' (compatibility version', 1)[0]
                            for line in text.splitlines()[1:] if line.strip()]
            for dependency in dependencies:
                if any(word in dependency.lower() for word in forbidden):
                    raise AssertionError(f'托管宿主依赖：{dependency}')
                if dependency.startswith(('/usr/lib/', '/System/Library/')):
                    continue
                candidates = []
                if dependency.startswith('@rpath/'):
                    for entry in rpaths:
                        base = entry.replace('@loader_path', str(path.parent)).replace('@executable_path', str(relocated))
                        candidates.append(Path(base)/dependency[len('@rpath/'):])
                else:
                    candidates.append(Path(dependency.replace('@loader_path', str(path.parent)).replace('@executable_path', str(relocated))))
                def local_file(candidate):
                    return candidate.is_file() and candidate.resolve().is_relative_to(relocated.resolve())
                if not any(local_file(candidate) for candidate in candidates):
                    raise AssertionError(f'未闭合的非系统dylib依赖：{dependency}; rpaths={rpaths}')
        else:
            result = run.run(f'dependency-{index}', [tools['dumpbin'], '/DEPENDENTS', str(path)], cwd=relocated, check=True)
            imports = run.run(f'imports-{index}', [tools['dumpbin'], '/IMPORTS', str(path)], cwd=relocated, check=True)
            text = (result.stdout + imports.stdout).decode('utf-8', errors='replace')
            if any(word in text.lower() for word in forbidden):
                raise AssertionError(f'托管宿主导入：{path}')
            dependencies = re.findall(r'^\s+([^\s]+\.dll)\s*$', result.stdout.decode('utf-8', errors='replace'), re.M | re.I)
            if not dependencies:
                raise AssertionError(f'未读到PE依赖清单（不把解析失败当无依赖）：{path}')
            local = {item.name.lower() for item in relocated.iterdir()}
            system = Path(os.environ['SystemRoot'])/'System32'
            for dependency in dependencies:
                if (dependency.lower() not in local and not dependency.lower().startswith(('api-ms-win-', 'ext-ms-win-'))
                        and not (system/dependency).is_file()):
                    raise AssertionError(f'未闭合PE依赖：{dependency}')
        records.append({'file': path.name, 'dependencies': dependencies})
    return records


def clean_environment(work):
    work = work.resolve()
    empty = work/'empty-path'
    empty.mkdir()
    env = {'PATH': str(empty), 'HOME': str(work)}
    if os.name == 'nt':
        # 保留正常Windows用户目录合同，但绝不借用宿主配置、缓存或临时文件。
        profile = work/'profile'
        roaming = profile/'AppData'/'Roaming'
        local = profile/'AppData'/'Local'
        temporary = local/'Temp'
        roaming.mkdir(parents=True)
        temporary.mkdir(parents=True)
        env.update({'HOME': str(profile), 'USERPROFILE': str(profile),
                    'APPDATA': str(roaming), 'LOCALAPPDATA': str(local),
                    'TEMP': str(temporary), 'TMP': str(temporary)})
        for key in ('SystemRoot', 'WINDIR'):
            if key in os.environ:
                env[key] = os.environ[key]
    else:
        env.update({'TMPDIR': str(work), 'LANG': 'en_US.UTF-8', 'LC_ALL': 'en_US.UTF-8'})
    for name in ('dotnet', 'python', 'python3', 'python3.12'):
        if shutil.which(name, path=env['PATH']):
            raise AssertionError(f'精简PATH仍包含{name}')
    return env
