"""Legacy动态生成器独立合同：observer前置、JIT、精确Instance负例、错误帧与Native。"""
import json
import os
import re
from pathlib import Path
import shutil
import sys
import traceback
import run as h
from docker_image import prepare_image
from instance_gate import DockerRunner, FIXED_PYTHON_SHA256
from native_evidence import preserve_dist, snapshot

FIXTURES = h.REPO / 'tests/cases/generator_dynamic'
EXPECTED = (b'observer identity context\ndelayed rebound short circuit\n'
            b'cleanup flow pending return\ncontext getter and caller identity\n'
            b'protocol delegate and pep479\ntargets validation and bare\n'
            b'workload shapes terminal and paths\n')


def strict(name, args, **kwargs):
    h.run(name, args, **kwargs)
    if (h.OUT / (name + '.stderr')).read_bytes():
        raise RuntimeError(name + ' stderr必须为空')


def compile_source(cli, entry, output, instance=False):
    args = [cli]
    if instance:
        args += ['--instance-module', 'subject']
    return args + ['build', entry, '--diagnostic-stages', '--out-dir', output]


def require_failure(name, args, **kwargs):
    try:
        h.run(name, args, **kwargs)
    except RuntimeError:
        record = h.COMMANDS[-1]
        if record['name'] != name or record.get('timeout_expired') or record.get('exit', 0) <= 0:
            raise
        return record
    raise RuntimeError(name + '必须真实失败，不能以成功冒负例')


def require_legacy(path):
    plan = json.loads(path.read_text())
    if not plan['modules'] or any(m['execution'] != 'LegacySingleton' or not m['compiled']
                                  for m in plan['modules']):
        raise RuntimeError('完整合同意外进入Instance或存在未完成模块')
    return plan


def jit_stage(python, cli, runtime_sha, observer=False):
    root = h.WORK / ('observer' if observer else 'default')
    shutil.copytree(FIXTURES / 'default', root)
    entry = root / ('observer_control.py' if observer else 'entry.py')
    expected = b'observer identity context\n' if observer else EXPECTED
    strict('cp', [python, '-S', '-B', entry], expected=expected)
    output = h.OUT / 'jit'
    h.run('compile', compile_source(cli, entry, output), timeout=300)
    h.save('plan.json', require_legacy(output / 'module-environment.json'))
    if h.sha(output / 'RyuPy.Runtime.dll') != runtime_sha:
        raise RuntimeError('实际JIT runtime身份不符')
    for pic in ['1', '0']:
        strict('jit-pic-' + pic, [os.environ['DOTNET'], output / (entry.stem + '.dll')],
               env=dict(os.environ, RYUPY_PIC=pic), expected=expected)


def negative_stage(python, cli):
    root = h.WORK / 'instance-negative'
    shutil.copytree(FIXTURES / 'instance_negative', root)
    entry = root / 'entry.py'
    strict('cp', [python, '-S', '-B', entry], expected=b'instance generator remains guarded\n')
    output = h.OUT / 'jit'
    require_failure('compile', compile_source(cli, entry, output, True), timeout=300)
    text = (h.OUT / 'compile.stderr').read_text() + (h.OUT / 'compile.stdout').read_text()
    if '[E0100]' not in text or '实例模块首切暂不支持生成器或类体源码except handler' not in text:
        raise RuntimeError('Instance负例未命中目标handler guard')
    if output.exists() and list(output.glob('*.dll')):
        raise RuntimeError('拒绝编译却发射了PE')
    # CLI仅成功发射后写module-environment.json；失败计划由真实driver单测检查Session。
    h.save('rejection.json', {'passed': True, 'expected_guard': 'Instance generator handler',
                              'plan_evidence': 'generator_dynamic_fixture_tests中的实际Session；非CLI报告'})


def check_frames(text):
    # 仅归一化绝对路径与CP源码/caret排版；不删帧、不改函数名/行号/异常链顺序。
    separator = 'During handling of the above exception, another exception occurred:'
    parts = text.split(separator)
    if len(parts) != 2:
        raise RuntimeError('错误链必须是一次隐式context连接')
    expected = [[(11, 'generate')], [(20, '<module>'), (12, 'generate'), (5, 'error')]]
    for part, frames, ending in zip(parts, expected, ['ValueError: original', 'TypeError: target failed']):
        observed = []
        for filename, line, function in re.findall(r'File "([^"\n]+)", line (\d+), in ([^\n]+)', part):
            if Path(filename).name != 'entry.py':
                raise RuntimeError('错误帧出现非固定源')
            observed.append((int(line), function))
        if observed != frames or not part.rstrip().endswith(ending):
            raise RuntimeError('固定错误帧/行号/最终错误不符：' + repr(observed))
    return expected


def frames_stage(python, cli, runtime_sha):
    root = h.WORK / 'frames'
    shutil.copytree(FIXTURES / 'frames', root)
    entry = root / 'entry.py'
    require_failure('cp', [python, '-S', '-B', entry])
    output = h.OUT / 'jit'
    h.run('compile', compile_source(cli, entry, output), timeout=300)
    h.save('plan.json', require_legacy(output / 'module-environment.json'))
    if h.sha(output / 'RyuPy.Runtime.dll') != runtime_sha:
        raise RuntimeError('错误帧PE实际runtime身份不符')
    require_failure('jit', [os.environ['DOTNET'], output / 'entry.dll'])
    for mode in ['cp', 'jit']:
        text = (h.OUT / (mode + '.stderr')).read_text()
        if (h.OUT / (mode + '.stdout')).read_bytes() != b'ready\n':
            raise RuntimeError(mode + '错误路径输出不符')
        check_frames(text)


def native_stage(cli, identity):
    root = h.WORK / 'native'
    shutil.copytree(FIXTURES / 'default', root)
    entry = root / 'entry.py'
    image = prepare_image(DockerRunner(), 'docker', h.WORK)
    h.run('publish', [cli, 'build', entry, '--aot'], timeout=1500)
    dist = entry.with_suffix('.dist')
    exe = dist / 'entry'
    manifest = json.loads((dist / 'ryupy-artifact.json').read_text())
    required = {'__main__', 'observer', 'delayed', 'cleanup', 'context', 'protocol', 'targets', 'workloads'}
    if {m['name'] for m in manifest['modules']} != required:
        raise RuntimeError('综合Native模块不完整')
    if any(m['environment_reason'] != 'aot-not-migrated' for m in manifest['modules']):
        raise RuntimeError('不得扩大Native自动Instance')
    if exe.read_bytes()[:4] != b'\x7fELF' or any(p.suffix.lower() in {'.py', '.pyc', '.pyo'} for p in dist.rglob('*')):
        raise RuntimeError('Native产物身份/源码隔离失败')
    before = preserve_dist(dist, h.OUT)
    h.run('elf-header', ['readelf', '-h', exe])
    h.run('dependencies', ['ldd', exe])
    strict('native', [exe], expected=EXPECTED)
    relocated = h.WORK / 'relocated.dist'
    shutil.move(dist, relocated)
    strict('relocated', [relocated / 'entry'], cwd=relocated, expected=EXPECTED)
    script = ('for tool in dotnet python python3 python3.12; do '
              'if command -v "$tool" >/dev/null 2>&1; then exit 97; fi; done; exec "$1"')
    strict('docker', ['docker', 'run', '--pull=never', '--rm', '--network', 'none', '--read-only',
        '--mount', f'type=bind,src={relocated},dst=/app,readonly', '--workdir', '/app',
        '--entrypoint', '/bin/sh', image, '-c', script, 'generator-dynamic', '/app/entry'],
        expected=EXPECTED, timeout=300)
    after, _ = snapshot(relocated)
    if before != after or dist.exists():
        raise RuntimeError('Native搬移前后内容改变')
    h.save('manifest-check.json', {'manifest': manifest, 'before': before, 'after': after,
                                  'identity': identity})


def main():
    h.OUT = h.REPO / 'target/generator-dynamic-evidence'
    h.WORK = h.REPO / 'target/generator-dynamic-work'
    h.OUT.mkdir(exist_ok=False)
    h.WORK.mkdir(exist_ok=False)
    python = Path(sys.executable).resolve(strict=True)
    cli = h.REPO / 'target/debug/ryupy'
    runtime = h.REPO / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
    if sys.version_info[:3] != (3, 12, 13) or h.sha(python) != FIXED_PYTHON_SHA256:
        raise RuntimeError('必须使用固定CP3.12.13')
    identity = (h.sha(cli), h.sha(runtime))
    baseline = json.loads((h.REPO / 'target/wcwidth-aot-evidence/compiler-identity.json').read_text())
    if identity != (baseline['cli_sha256'], baseline['runtime_sha256']):
        raise RuntimeError('新门必须与本轮既有Legacy门使用同一实际工具')
    sources = {str(p.relative_to(FIXTURES)): h.sha(p) for p in sorted(FIXTURES.rglob('*')) if p.is_file()}
    h.save('inputs.json', {'source_sha256': sources, 'python_sha256': h.sha(python),
        'cli_sha256': identity[0], 'runtime_sha256': identity[1], 'commit': os.environ['GITHUB_SHA']})
    out = h.OUT
    failures = []
    observer_passed = False
    for stage in ['observer', 'default', 'instance-negative', 'frames', 'native']:
        h.OUT = out / stage
        h.OUT.mkdir()
        h.COMMANDS = []
        try:
            if stage in ['default', 'native'] and not observer_passed:
                raise RuntimeError('普通observer前置失败，不解释依赖该观察器的合同')
            if stage in ['observer', 'default']:
                jit_stage(python, cli, identity[1], stage == 'observer')
                observer_passed = True
            elif stage == 'instance-negative':
                negative_stage(python, cli)
            elif stage == 'frames':
                frames_stage(python, cli, identity[1])
            else:
                native_stage(cli, identity)
            h.save('result.json', {'passed': True, 'stage': stage})
        except Exception as error:
            failures.append(stage)
            h.save('result.json', {'passed': False, 'stage': stage, 'error': str(error),
                                  'traceback': traceback.format_exc()})
    h.OUT = out
    if sources != {str(p.relative_to(FIXTURES)): h.sha(p) for p in sorted(FIXTURES.rglob('*')) if p.is_file()}:
        raise RuntimeError('fixture运行中改变')
    if identity != (h.sha(cli), h.sha(runtime)):
        raise RuntimeError('实际工具运行中改变')
    h.save('result.json', {'passed': not failures, 'failures': failures,
                          'scope': 'Legacy动态生成器；Instance精确负例；无性能/历史结构相等承诺'})
    if failures:
        raise RuntimeError('独立合同失败：' + ', '.join(failures))


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        h.OUT.mkdir(parents=True, exist_ok=True)
        h.save('failure.json', {'error': str(error), 'traceback': traceback.format_exc()})
        raise
    finally:
        for path in h.WORK.rglob('*'):
            if path.is_file() and path.name in {'publish.stdout.log', 'publish.stderr.log', 'Host.csproj', 'Program.cs'}:
                target = h.REPO / 'target/generator-dynamic-evidence/publish' / path.relative_to(h.WORK)
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(path, target)
