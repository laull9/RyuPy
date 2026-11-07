#!/usr/bin/env python3
"""一次 all-Legacy wcwidth 原四调用 NativeAOT；记录原流、身份、ELF 与搬移。"""
import hashlib
import json
import os
from pathlib import Path
import shutil
import signal
import subprocess
import sys
import traceback

# 共用AOT证据工具，不加载项目源码或修改目标进程的模块搜索路径。
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from native_evidence import preserve_dist, snapshot
import metadata_gate

REPO = Path(__file__).resolve().parents[3]
HERE = Path(__file__).resolve().parent
OUT = REPO / 'target/wcwidth-aot-evidence'
WORK = REPO / 'target/wcwidth-aot-work'
EXPECTED = b'3 4 2\n1\n'
COMMANDS = []


def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def save(name, value):
    (OUT / name).write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n')


def run(name, args, *, cwd=REPO, env=None, timeout=120, expected=None):
    args = list(map(str, args))
    record = {'name': name, 'argv': args, 'cwd': str(cwd), 'timeout': timeout}
    COMMANDS.append(record)
    save('commands.json', COMMANDS)
    print(json.dumps(record, ensure_ascii=False), flush=True)
    with (OUT / (name + '.stdout')).open('wb') as out, (OUT / (name + '.stderr')).open('wb') as err:
        try:
            process = subprocess.Popen(args, cwd=cwd, env=env, stdout=out, stderr=err,
                                       start_new_session=True)
            record['exit'] = process.wait(timeout=timeout)
        except subprocess.TimeoutExpired:
            record['timeout_expired'] = True
            try:
                os.killpg(process.pid, signal.SIGKILL)
            except ProcessLookupError:
                pass
            record['exit'] = process.wait()
            raise
        finally:
            save('commands.json', COMMANDS)
    if record['exit']:
        raise RuntimeError(f'{name} 退出码 {record["exit"]}，见原始双流')
    if expected is not None:
        actual = (OUT / (name + '.stdout')).read_bytes()
        record['stdout_matches'] = actual == expected
        save('commands.json', COMMANDS)
        if actual != expected:
            raise RuntimeError(f'{name} stdout 不符：{actual!r}')


def main():
    WORK.mkdir(parents=True, exist_ok=False)
    if sys.version_info[:3] != (3, 12, 13):
        raise RuntimeError(f'错误 CPython 版本：{sys.version}')
    source = Path(os.environ['WCWIDTH_SOURCE'])
    hashes = {name: sha(source / 'wcwidth' / name)
              for name in json.loads((HERE / 'source-hashes.json').read_text())}
    if hashes != json.loads((HERE / 'source-hashes.json').read_text()):
        raise RuntimeError('官方 wcwidth 六源哈希不匹配')
    entry = WORK / '01_original.py'
    shutil.copyfile(HERE / entry.name, entry)
    if sha(entry) != '2c537d4bd16d4c33316a28ac74155917ee986b91ab90060bee2875b3ac0c7a55':
        raise RuntimeError('原四调用入口哈希不匹配')
    save('input-identity.json', {'package': 'wcwidth==0.2.13', 'source_hashes': hashes,
         'entry_sha256': sha(entry), 'expected_stdout_hex': EXPECTED.hex(),
         'mode': 'all-Legacy; no --instance-module',
         'accepted_source_commit': '290905691e204f655dd03a02b194fc226a96322d',
         'ci_commit': os.environ['GITHUB_SHA'], 'python': sys.version,
         'python_sha256': sha(Path(sys.executable).resolve()),
         'python_path': str(Path(sys.executable).resolve()),
         'python_flags': str(sys.flags),
         'semantic_environment': {k: os.environ.get(k) for k in
             ['UNICODE_VERSION', 'LC_ALL', 'TZ', 'PYTHONHASHSEED']},
         'cpython_subprocess_flags': ['-B'],
         'cpython_subprocess_environment_overrides': {'PYTHONPATH': str(source),
                                                    'PYTHONDONTWRITEBYTECODE': '1'}})
    for name, cmd in [('git', ['git', 'rev-parse', 'HEAD', 'HEAD^{tree}']),
                      ('rustc', ['rustc', '--version']), ('cargo', ['cargo', '--version']),
                      ('dotnet', ['dotnet', '--info']), ('clang', ['clang', '--version']),
                      ('os-release', ['cat', '/etc/os-release']),
                      ('native-prerequisites', ['dpkg-query', '-W', 'zlib1g-dev', 'libc6'])]:
        run(name, cmd)
    env = dict(os.environ, PYTHONPATH=str(source), PYTHONDONTWRITEBYTECODE='1')
    run('cpython', [sys.executable, '-B', entry], env=env, expected=EXPECTED)
    run('runtime-build', ['make', 'runtime'], timeout=300)
    run('compiler-build', ['cargo', 'build', '--locked', '-p', 'ryupy-cli',
        '--features', 'ryupy-driver/ordinary-dynamic-module-pilot'], timeout=1200)
    cli = REPO / 'target/debug/ryupy'
    runtime = REPO / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
    frozen_hashes = (sha(cli), sha(runtime))
    save('compiler-identity.json', {'cli_sha256': frozen_hashes[0], 'runtime_sha256': frozen_hashes[1],
         'feature': 'ryupy-driver/ordinary-dynamic-module-pilot', 'configuration': 'debug'})
    metadata_gate.execute(sys.modules[__name__], cli, runtime)
    run('native-build', [cli, 'build', entry, '--aot', '-I', source], timeout=1200)
    dist = WORK / '01_original.dist'
    exe = dist / '01_original'
    if exe.read_bytes()[:4] != b'\x7fELF':
        raise RuntimeError('不是 ELF 原生产物')
    if not (dist / 'ryupy-artifact.json').is_file():
        raise RuntimeError('缺失原生交付manifest')
    before = preserve_dist(dist, OUT)
    run('file', ['file', exe])
    run('elf-header', ['readelf', '-h', exe])
    run('elf-dynamic', ['readelf', '-d', exe])
    run('dependencies', ['ldd', exe])
    run('native-run', [exe], expected=EXPECTED)
    relocated = Path(os.environ['RUNNER_TEMP']) / 'wcwidth-relocated-dist'
    shutil.move(str(dist), relocated)
    run('relocated-run', [relocated / exe.name], cwd=relocated, expected=EXPECTED)
    run('relocated-dependencies', ['ldd', relocated / exe.name])
    after, _ = snapshot(relocated)
    if before != after or dist.exists():
        raise RuntimeError('搬移身份或旧路径状态不符')
    save('relocation.json', {'before': before, 'after': after, 'original_dist_exists': dist.exists(),
         'relocated_path': str(relocated), 'note': '仅同一 Linux 主机搬移，不代表绝对无系统动态依赖'})
    if frozen_hashes != (sha(cli), sha(runtime)):
        raise RuntimeError('运行中CLI或runtime被改写')
    save('result.json', {'passed': True, 'scope': '仅 wcwidth 0.2.13 原四调用 all-Legacy NativeAOT'})


if __name__ == '__main__':
    OUT.mkdir(parents=True, exist_ok=True)
    try:
        main()
    except Exception as exc:
        save('result.json', {'passed': False, 'error': str(exc), 'traceback': traceback.format_exc()})
        raise
    finally:
        # 保存 SDK 原流与生成发布参数，不上传 obj、下载包、缓存或用户环境。
        for path in WORK.rglob('*'):
            if path.is_file() and path.name in {'publish.stdout.log', 'publish.stderr.log', 'Host.csproj', 'Program.cs'}:
                target = OUT / 'publish' / path.relative_to(WORK)
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(path, target)
