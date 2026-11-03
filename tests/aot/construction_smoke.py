#!/usr/bin/env python3
"""普通构造有限验收：一次公开AOT、同DLL两JIT/两native；准备也计入总预算。"""
import argparse
from contextlib import contextmanager
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import tempfile
import time
import xml.etree.ElementTree as ET

from manifest_files import digest, files_under, plain_stat
from manifest_verify import (MANIFEST_NAME, dependency_binaries, published_manifest,
                             read_sdk_version, relocate_manifest, verify_manifest)
from platform_runner import Runner
from platform_smoke import published_platform_native
from platform_support import audit, clean_environment, host

SDK = '10.0.403'
RUST = '1.99.1'
MODES = ('positive', 'ryupy-boundaries')
# 与唯一fixture一起审定；不能从当次程序输出生成期望。
FIXTURE_SHA256 = 'ed97c274b1d5159486c87ca7fbf5594a0d9b815898f26dea7298c2e9df66d34e'
EXPECTED = {'positive': {'stdout': 'sentinel-ok\n'
                        'allocation-ok\n'
                        'binding-ok\n'
                        'BadReturn TypeError\n'
                        'BindNew TypeError\n'
                        'BindInit TypeError\n'
                        'NewError RuntimeError\n'
                        'InitError RuntimeError\n'
                        'errors-ok\n'
                        'positive-assertions-ok\n',
              'exit': 1,
              'frames': [['construction.py', 297, '<module>'],
                         ['construction.py', 295, 'caller'],
                         ['construction.py', 292, '__new__']],
              'exception': 'ValueError: new-body'},
 'ryupy-boundaries': {'stdout': 'ordinary-mutation-atomic-ok\n'
                                'special-layout-guard-ok\n'
                                'ryupy-boundaries-ok\n',
                      'exit': 1,
                      'frames': [['construction.py', 422, '<module>'],
                                 ['construction.py', 420, 'boundary_caller']],
                      'exception': 'NotImplementedError: deleting finalized __new__ is not supported'}}
PHASES = {'preflight': 120, 'reference': 30, 'runtime': 180, 'compiler': 600,
          'publish': 600, 'relocation': 120, 'applications': 120}


def require(condition, message):
    if not condition:
        raise AssertionError(message)


def write_json(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2, allow_nan=False) + '\n', encoding='utf-8')


def file_record(path):
    size, sha = digest(Path(path))
    return {'size': size, 'sha256': sha}


def inventory(root, check=lambda: None):
    check()
    result = {}
    for name, path in sorted(files_under(root).items()):
        check()
        result[name] = file_record(path)
        check()
    return result


def unchanged(root, expected, check=lambda: None):
    actual = inventory(root, check)
    require(actual == expected, f'完整文件集合/字节变化：{root}')
    return actual


def executable(value, *, proxy=False):
    found = shutil.which(str(value))
    if not found:
        raise FileNotFoundError(f'未找到工具：{value}')
    # cargo/rustc代理按argv[0]分派，不能resolve成rustup。
    return os.path.abspath(found) if proxy else str(Path(found).resolve())


def expected_stdout(mode):
    text = EXPECTED[mode]['stdout']
    require('\r' not in text, '冻结stdout必须使用LF')
    return text.replace('\n', os.linesep).encode('utf-8')


def signature(result, mode, *, allow_source=False):
    """原日志不改字节；结构比较只排除CP源码展示和物理目录前缀。"""
    expected = EXPECTED[mode]
    require(result.returncode == expected['exit'] == 1, f'{mode}: exit不符')
    require(result.stdout == expected_stdout(mode), f'{mode}: 固定stdout不符')
    text = result.stderr.decode('utf-8', errors='strict')
    lines = text.splitlines()
    require(bool(lines) and lines[0] == 'Traceback (most recent call last):', '错误报告有非预期前缀')
    for line in lines[1:-1]:
        frame = re.fullmatch(r'  File "[^"]+", line [0-9]+, in [^\r\n]+', line)
        require(frame is not None or (allow_source and line.startswith('    ')), '错误报告含非预期行')
    require(text.count('Traceback (most recent call last):') == 1, '需要唯一完整traceback链')
    require(not any(x in text for x in ('Unhandled exception',
            'During handling of the above exception', 'The above exception was the direct cause')), '非预期错误链/CLR报告')
    frames = [[name.replace('\\', '/').rsplit('/', 1)[-1], int(line), function.rstrip('\r')]
              for name, line, function in re.findall(r'File "([^"]+)", line (\d+), in ([^\n]+)', text)]
    require(frames == expected['frames'], f'{mode}: 完整帧不符：{frames}')
    require(bool(lines) and lines[-1] == expected['exception'], f'{mode}: 最终异常不符')
    return {'frames': frames, 'exception': lines[-1]}


def compare(reference, actual, mode, *, python=False):
    # 两端先独立满足冻结合同，不能双方同样错误就通过。
    before, after = signature(reference, mode, allow_source=python), signature(actual, mode)
    require(reference.stdout == actual.stdout and reference.returncode == actual.returncode, '原stdout/exit不一致')
    if python:
        require(mode == 'positive' and before == after, 'CP只能比较positive的完整帧签名')
    else:
        require(reference.stderr == actual.stderr, 'JIT/native原stderr字节不一致')


class Budget:
    """仅本入口的固定阶段账本；一个Runner的截止始终不晚于入口总截止。"""
    def __init__(self, run, start, result, *, clock=time.monotonic):
        self.runner, self.start, self.result, self.clock = run, start, result, clock
        self.deadline = start + 1800
        self.current = None
        self.used = set()
        self.runner.deadline = self.deadline

    def check(self):
        if self.clock() >= self.runner.deadline:
            raise TimeoutError(f'{self.current or "total"}预算耗尽')

    @contextmanager
    def phase(self, name):
        require(name in PHASES and name not in self.used, '阶段未知或重复，不能重置预算')
        self.used.add(name)
        self.current = name
        began = self.clock()
        self.runner.deadline = min(self.deadline, began + PHASES[name])
        self.result['stage'] = name
        try:
            self.check()
            yield
            self.check()
        finally:
            self.result.setdefault('phases', {})[name] = {'seconds': self.clock() - began, 'limit_seconds': PHASES[name]}
            self.runner.deadline = self.deadline

    def command(self, label, command, *, cwd, env=None, timeout=30, check=True):
        self.check()
        self.result.setdefault('commands', []).append({'label': label, 'phase': self.current,
            'argv': list(map(str, command)), 'cwd': str(cwd), 'limit_seconds': timeout})
        return self.runner.run(label, list(map(str, command)), cwd=cwd, env=env, timeout=timeout, check=check)

    def run(self, label, command, *, cwd, env=None, timeout=30, check=False):
        # 平台依赖审计沿其原接口进入同一账本，工具命令不获得180秒默认窗口。
        return self.command(label, command, cwd=cwd, env=env, timeout=min(timeout, 30), check=check)


def published_inputs(output, il, rid):
    native = published_platform_native(output.stderr, il, rid)
    published_manifest(output.stderr, native)
    paths = [Path(line.removeprefix('已生成 ')) for line in output.stderr.decode('utf-8').splitlines()
             if line.startswith('已生成 ')]
    managed = [p for p in paths if p.suffix.lower() == '.dll']
    require(len(paths) == 2 and len(managed) == 1, 'CLI需要唯一managed和native行')
    dll = il/'construction.dll'
    require(managed[0].is_absolute() and managed[0].samefile(dll), 'CLI托管入口不是同次DLL')
    for p in (dll, il/'construction.runtimeconfig.json', il/'RyuPy.Runtime.dll'):
        plain_stat(p)
    return native, dll


def verify_inputs(data, il, runtime):
    require(data['schema_version'] == 1, '只接受无资源schema1')
    require(all(value is False for value in data['compiler']['options'].values()), '不能打开实验开关')
    sources = data['sources']
    require(len(sources) == 1 and sources[0]['module_name'] == '__main__'
            and sources[0]['compiled_text_sha256'] == FIXTURE_SHA256, '源码身份或冻结SHA不符')
    inputs = {item['relative_path']: {'size': item['size'], 'sha256': item['sha256']}
              for item in data['build_inputs']}
    require(inputs == {'managed-entry.dll': file_record(il/'construction.dll'),
                      'RyuPy.Runtime.dll': file_record(il/'RyuPy.Runtime.dll')}, 'ILC输入与同次托管文件不符')
    require(inputs['RyuPy.Runtime.dll'] == file_record(runtime), 'runtime不是本次准备的字节')
    return inputs


def verify_project(root):
    text = (root/'host/Host.csproj').read_text(encoding='utf-8')
    project = ET.fromstring(text)
    for name in ('PublishAot', 'TreatWarningsAsErrors', 'IlcTreatWarningsAsErrors', 'ILLinkTreatWarningsAsErrors'):
        require(project.findtext('./PropertyGroup/' + name) == 'true', f'{name}没有严格开启')
    require(project.findtext('./PropertyGroup/TrimmerSingleWarn') == 'false', '裁剪警告被折叠')
    forbidden = ('TrimmerRoot', 'RootAll', 'DynamicDependency', 'NoWarn', 'WarningsNotAsErrors', 'SuppressTrim')
    require(not any(word in text for word in forbidden), '额外保根/警告屏蔽')
    require([x.attrib.get('Include') for x in project.findall('./ItemGroup/Reference')]
            == ['construction', 'RyuPy.Runtime'], '托管引用不是两个精确输入')
    expected = b'internal static class Program { private static int Main(string[] args) => global::RyuPy.Generated.Module.Main(args); }\n'
    require((root/'host/Program.cs').read_bytes() == expected, '公开薄入口改变')


def copy_jit(il, destination, expected, check=lambda: None):
    destination.mkdir()
    for name in expected:
        check()
        shutil.copy2(il/name, destination/name)
    unchanged(destination, expected, check)
    return destination/'construction.dll'


def retain_owned(directory, work, evidence, budget):
    budget.check()
    plain_stat(directory, directory=True)
    require(directory.resolve().parent == work.resolve(), '拒绝移动非本轮直接owned目录')
    target = evidence/directory.name
    require(not target.exists(), '证据目标已存在')
    before = inventory(directory, budget.check)
    directory.rename(target)
    require(not directory.exists(), '原构建路径仍存在')
    unchanged(target, before, budget.check)
    budget.check()
    return {'old': str(directory), 'new': str(target), 'files': before}


def linux_audit(budget, package, data, env):
    """有界核本包动态依赖；不声称无SDK宿主或一般闭包证明。"""
    records = []
    for index, binary in enumerate(dependency_binaries(package, data)):
        dynamic = budget.command(f'readelf-{index}', [executable('readelf'), '-d', binary], cwd=package, env=env)
        linked = budget.command(f'ldd-{index}', [executable('ldd'), binary], cwd=package, env=env)
        text = (dynamic.stdout + dynamic.stderr + linked.stdout + linked.stderr).decode('utf-8', errors='strict')
        require(not any(word in text.lower() for word in ('hostfxr', 'hostpolicy', 'coreclr', 'mscoree', 'not found')), '托管宿主或缺失依赖')
        for paths in re.findall(r'\((?:RPATH|RUNPATH)\).*?\[([^\]]*)\]', dynamic.stdout.decode('utf-8')):
            for entry in paths.split(':'):
                require(entry.startswith('$ORIGIN') and (len(entry) == 7 or entry[7] == '/'), '不接受非随包RPATH/RUNPATH')
                require((binary.parent/entry[7:].lstrip('/')).resolve().is_relative_to(package.resolve()), 'RPATH逃逸随包目录')
        needed = re.findall(r'\(NEEDED\).*Shared library: \[([^\]]+)\]', dynamic.stdout.decode('utf-8'))
        require(bool(needed), '未解析到本固定NativeAOT的NEEDED条目')
        for name in needed:
            match = re.search(r'^\s*' + re.escape(name) + r'\s+=>\s+(/\S+)\s+\(', linked.stdout.decode('utf-8'), re.M)
            direct_interpreter = match is None and name == 'ld-linux-x86-64.so.2'
            if direct_interpreter:
                # glibc解释器在ldd中可直接输出绝对路径，没有name =>前缀。
                match = re.search(r'^\s*(/\S*/' + re.escape(name) + r')\s+\(', linked.stdout.decode('utf-8'), re.M)
            require(match is not None, f'NEEDED未解析：{name}')
            path = Path(match.group(1)).resolve()
            system = any(path.is_relative_to(Path(prefix).resolve()) for prefix in ('/lib', '/lib64', '/usr/lib', '/usr/lib64'))
            require(path.is_file() and (system or (not direct_interpreter and path.is_relative_to(package.resolve()))), f'非系统/非随包依赖：{path}')
        records.append({'file': binary.name, 'needed': needed})
    return records


def toolchains(budget, repo, work, python, dotnet, cargo, env, rid):
    cp = budget.command('python-version', [python, '-c', "import sys; print(sys.implementation.name); print('.'.join(map(str,sys.version_info[:3])))"], cwd=work, env=env)
    require(cp.stdout.decode().splitlines() == ['cpython', '3.12.13'], '必须固定CPython3.12.13')
    # owned global.json不约束repo cwd，runtime准备所在cwd单独实核。
    for label, cwd in (('runtime-sdk', repo), ('owned-sdk', work)):
        value = budget.command(label, [dotnet, '--version'], cwd=cwd, env=env)
        require(value.stdout.decode('utf-8-sig').splitlines() == [SDK], '实际cwd SDK不是10.0.403')
    info = budget.command('dotnet-info', [dotnet, '--info'], cwd=work, env=env).stdout.decode('utf-8-sig')
    arch = 'arm64' if rid == 'osx-arm64' else 'x64'
    arches = re.findall(r'^\s*Architecture:\s*(\S+)\s*$', info, re.M)
    require(bool(arches) and all(x == arch for x in arches), '.NET实际架构不符')
    require(re.findall(r'^\s*RID:\s*(\S+)\s*$', info, re.M) == [rid], '.NET实际RID不符')
    rust = budget.command('rustc-version', [executable('rustc', proxy=True), '-vV'], cwd=repo, env=env).stdout.decode('utf-8')
    wanted = {'linux-x64': 'x86_64-unknown-linux-gnu', 'osx-arm64': 'aarch64-apple-darwin', 'win-x64': 'x86_64-pc-windows-msvc'}[rid]
    require(re.findall(r'^release: (\S+)$', rust, re.M) == [RUST], 'Rust版本不符')
    require(re.findall(r'^host: (\S+)$', rust, re.M) == [wanted], 'Rust host不符')
    budget.command('cargo-version', [cargo, '--version'], cwd=repo, env=env)
    tools = {}
    if rid == 'osx-arm64':
        tools['otool'] = executable('otool')
        budget.command('xcode-version', [executable('xcodebuild'), '-version'], cwd=work, env=env)
        budget.command('clang-version', [executable('clang'), '--version'], cwd=work, env=env)
    elif rid == 'win-x64':
        tools['dumpbin'] = executable('dumpbin')
        budget.command('dumpbin-version', [tools['dumpbin'], '/?'], cwd=work, env=env, check=False)
    else:
        budget.command('clang-version', [executable('clang'), '--version'], cwd=work, env=env)
    return tools


def main(argv=None):
    start, utc = time.monotonic(), time.time()
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repo', required=True, type=Path)
    parser.add_argument('--revision', required=True)
    parser.add_argument('--out', required=True, type=Path)
    parser.add_argument('--python', required=True)
    parser.add_argument('--dotnet', default=os.environ.get('DOTNET', 'dotnet'))
    parser.add_argument('--rid', required=True, choices=('linux-x64', 'osx-arm64', 'win-x64'))
    args = parser.parse_args(argv)
    args.out.mkdir(parents=True, exist_ok=True)
    work = Path(tempfile.mkdtemp(prefix='construction-aot-', dir=args.out.resolve()))
    logs = work/'logs'; logs.mkdir()
    result = {'status': 'failed', 'rid': args.rid, 'started_utc_epoch': utc, 'budget_seconds': 1800,
              'counts': {'runtime': 0, 'compiler': 0, 'publish': 0, 'reference': 0, 'applications': 0},
              'applications': {}, 'clean_machine_without_runtime_tested': False,
              'scope': 'same AOT DLL JIT and full-tree relocated native on SDK-installed host'}
    write_json(work/'started.json', {'started_utc_epoch': utc, 'budget_seconds': 1800})
    print(work, flush=True)
    budget = Budget(Runner(logs), start, result)
    try:
        with budget.phase('preflight'):
            require(re.fullmatch('[0-9a-f]{40}', args.revision) is not None, '需完整冻结revision')
            require(re.fullmatch('[0-9a-f]{64}', FIXTURE_SHA256) is not None and set(EXPECTED) == set(MODES), 'fixture尚未冻结')
            repo = args.repo.resolve(); result['host'] = host(args.rid)
            python, dotnet = executable(args.python), executable(args.dotnet)
            cargo = executable('cargo', proxy=True)
            env = {k: v for k, v in os.environ.items() if not k.startswith(('COMPlus_', 'DOTNET_Tiered', 'RYUPY_'))}
            runtime_dir = repo/'runtime/RyuPy.Runtime/bin/Release/net10.0'
            runtime = runtime_dir/'RyuPy.Runtime.dll'
            env.update(DOTNET=dotnet, RUSTUP_TOOLCHAIN=RUST, CARGO_TARGET_DIR=str(repo/'target'),
                       RYUPY_RUNTIME_DIR=str(runtime_dir), PYTHONUTF8='1', PYTHONIOENCODING='utf-8',
                       DOTNET_CLI_UI_LANGUAGE='en-US', MSBUILDDISABLENODEREUSE='1',
                       DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER='1', UseSharedCompilation='false')
            write_json(work/'global.json', {'sdk': {'version': SDK, 'rollForward': 'disable', 'allowPrerelease': False}})
            result['global_json'] = file_record(work/'global.json')
            head = budget.command('git-head', ['git', 'rev-parse', 'HEAD'], cwd=repo, env=env)
            require(head.stdout.decode().strip() == args.revision, '来源HEAD不符')
            dirty = budget.command('git-status', ['git', 'status', '--porcelain'], cwd=repo, env=env)
            require(dirty.stdout == b'', '来源工作树必须clean')
            result['revision'] = args.revision; result['source_clean'] = True
            tools = toolchains(budget, repo, work, python, dotnet, cargo, env, args.rid)
            cli = repo/'target/debug'/('ryupy.exe' if args.rid == 'win-x64' else 'ryupy')
            source = work/'source'; source.mkdir()
            fixture = repo/'tests/aot/construction_fixtures/construction.py'
            require(file_record(fixture)['sha256'] == FIXTURE_SHA256, '冻结fixture SHA不符')
            shutil.copy2(fixture, source/'construction.py')
            require(file_record(source/'construction.py')['sha256'] == FIXTURE_SHA256, 'fixture复制变化')
            result['fixture'] = file_record(fixture)
            result['harness'] = file_record(Path(__file__))
            result['frozen_modes'] = EXPECTED
        with budget.phase('reference'):
            result['counts']['reference'] += 1
            cp = budget.command('cp-positive', [python, '-B', source/'construction.py', 'positive'], cwd=work, env=env, check=False)
            result['reference'] = {'exit': cp.returncode, 'stdout_sha256': hashlib.sha256(cp.stdout).hexdigest(),
                'stderr_sha256': hashlib.sha256(cp.stderr).hexdigest(), 'signature': signature(cp, 'positive', allow_source=True)}
        with budget.phase('runtime'):
            result['counts']['runtime'] += 1
            budget.command('runtime-build', [dotnet, 'build', 'runtime/RyuPy.Runtime', '-c', 'Release', '-t:Rebuild', '-nologo', '-v', 'q'], cwd=repo, env=env, timeout=180)
            result['runtime'] = file_record(runtime)
        with budget.phase('compiler'):
            result['counts']['compiler'] += 1
            budget.command('compiler-build', [cargo, 'build', '-p', 'ryupy-cli', '--locked'], cwd=repo, env=env, timeout=600)
            result['cli'] = file_record(cli)
        with budget.phase('publish'):
            il = work/'out-il'
            result['counts']['publish'] += 1
            published = budget.command('aot-publish', [cli, 'build', source/'construction.py', '--out-dir', il, '--aot'], cwd=work, env=env, timeout=600)
            native, dll = published_inputs(published, il, args.rid)
        with budget.phase('relocation'):
            aot_root = native.parent.parent
            for name in ('publish.stdout.log', 'publish.stderr.log'):
                shutil.copy2(aot_root/name, logs/('driver-' + name))
            require(b'Generating native code' in (logs/'driver-publish.stdout.log').read_bytes(), '无本次真实ILC日志')
            verify_project(aot_root)
            sdk = read_sdk_version(aot_root/'host/sdk.version')
            require(sdk == SDK, '实际publish SDK不符')
            shutil.copy2(aot_root/'host/sdk.version', logs/'sdk.version')
            result['sdk'] = {'version': sdk, **file_record(logs/'sdk.version')}
            data = verify_manifest(native.parent, native, args.rid, sdk)
            result['build_inputs'] = verify_inputs(data, il, runtime)
            jit_files = {name: file_record(il/name) for name in ('construction.dll', 'construction.runtimeconfig.json', 'RyuPy.Runtime.dll')}
            archive, _ = relocate_manifest(native.parent, native, work/'native-archive', args.rid, sdk)
            result['native_manifest'] = file_record(archive.parent/MANIFEST_NAME)
            result['native_archive_files'] = inventory(archive.parent, budget.check)
            result['dependencies'] = (linux_audit(budget, archive.parent, data, env) if args.rid == 'linux-x64' else
                audit(budget, archive.parent, args.rid, tools, binaries=dependency_binaries(archive.parent, data)))
            runs = work/'runs'; runs.mkdir(); copies = {}
            result['copies'] = {}
            for mode in MODES:
                for kind in ('jit', 'native'):
                    label = kind + '-' + mode; directory = runs/label
                    entry = (copy_jit(il, directory, jit_files, budget.check) if kind == 'jit' else
                             relocate_manifest(archive.parent, archive, directory, args.rid, sdk)[0])
                    copies[label] = entry
                    result['copies'][label] = {'before': inventory(directory, budget.check)}
                    budget.check()
            plain_stat(aot_root, directory=True)
            require(os.path.samefile(aot_root.parent, il), '原AOT根不再属于本次out-il')
            evidence = work/'build-evidence'; evidence.mkdir()
            result['retained'] = [retain_owned(p, work, evidence, budget) for p in (source, il)]
            # Windows CLI可带\\?\前缀；父归属已用samefile核过，勿用字符串relative_to。
            original_native = evidence/'out-il'/aot_root.name/'publish'/native.name
            result.update(original_build_paths_absent=True, build_evidence_retained=True, source_tree_action='renamed')
            clean = clean_environment(work); result['native_environment'] = clean
            jit_env = {**clean, 'DOTNET_CLI_UI_LANGUAGE': 'en-US', 'DOTNET_NOLOGO': '1', 'DOTNET_CLI_TELEMETRY_OPTOUT': '1'}
            result['jit_environment'] = jit_env
        with budget.phase('applications'):
            jit = {}
            for kind in ('jit', 'native'):
                for mode in MODES:
                    label = kind + '-' + mode; entry = copies[label]
                    unchanged(entry.parent, result['copies'][label]['before'], budget.check)
                    result['counts']['applications'] += 1
                    result['applications'][label] = {'status': 'started'}
                    actual = budget.command(label, [dotnet, entry, mode] if kind == 'jit' else [entry, mode],
                                            cwd=entry.parent, env=jit_env if kind == 'jit' else clean, check=False)
                    record = {'status': 'finished', 'exit': actual.returncode, 'stdout_sha256': hashlib.sha256(actual.stdout).hexdigest(),
                              'stderr_sha256': hashlib.sha256(actual.stderr).hexdigest()}
                    result['applications'][label] = record
                    record['signature'] = signature(actual, mode)
                    if kind == 'jit':
                        if mode == 'positive': compare(cp, actual, mode, python=True)
                        jit[mode] = actual
                    else:
                        compare(jit[mode], actual, mode)
                        verify_manifest(entry.parent, entry, args.rid, sdk)
                    result['copies'][label]['after'] = unchanged(entry.parent, result['copies'][label]['before'], budget.check)
                    budget.check()
            for label, entry in copies.items():
                unchanged(entry.parent, result['copies'][label]['before'], budget.check)
            unchanged(archive.parent, result['native_archive_files'], budget.check)
            verify_manifest(archive.parent, archive, args.rid, sdk)
            verify_manifest(original_native.parent, original_native, args.rid, sdk)
            for kept in result['retained']:
                unchanged(Path(kept['new']), kept['files'], budget.check)
                require(not Path(kept['old']).exists(), '原构建路径恢复出现')
            require(result['counts'] == {'runtime': 1, 'compiler': 1, 'publish': 1, 'reference': 1, 'applications': 4}, '固定计数不符')
        result['status'] = 'passed'
    except BaseException as error:
        result['failure'] = f'{type(error).__name__}: {error}'
        result['failed_stage'] = result.get('stage')
        raise
    finally:
        result['elapsed_seconds'] = time.monotonic() - start
        result['not_executed'] = [kind + '-' + mode for kind in ('jit', 'native') for mode in MODES
                                  if kind + '-' + mode not in result['applications']]
        write_json(work/'result.json', result)
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
