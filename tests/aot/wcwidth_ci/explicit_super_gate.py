"""同源显式super：默认/Instance JIT、Legacy Native及搬迁隔离；保留每步原流。"""
import json
import os
from pathlib import Path
import shutil
import sys
import traceback
import run as h
from docker_image import prepare_image
from instance_gate import DockerRunner, FIXED_PYTHON_SHA256
from native_evidence import preserve_dist, snapshot

EXPECTED = b'explicit super identity binding mutation phases\n'
FIXTURES = h.REPO / 'tests/cases/explicit_super'


def strict(name, args, **kwargs):
    h.run(name, args, **kwargs)
    if (h.OUT / (name + '.stderr')).read_bytes():
        raise RuntimeError(name + ' stderr非空，原流已保存')


def check_plan(path, instance):
    plan = json.loads(path.read_text())
    selected = [m for m in plan['modules'] if m['execution'] == 'InstanceGlobals']
    if instance:
        if len(selected) != 1 or selected[0]['module'] != 'subject' or not selected[0]['compiled']:
            raise RuntimeError('显式Instance JIT没有真正编译subject')
    elif selected:
        raise RuntimeError('普通class默认合同意外切到Instance')
    return plan


def check_native(dist):
    manifest = json.loads((dist / 'ryupy-artifact.json').read_text())
    modules = manifest['modules']
    if not {'__main__', 'subject'} <= {m['name'] for m in modules}:
        raise RuntimeError('Native模块不完整')
    if any(m['environment_reason'] != 'aot-not-migrated' for m in modules):
        raise RuntimeError('不应放开Instance class AOT')
    if any(p.suffix.lower() in {'.py', '.pyc', '.pyo'} for p in dist.rglob('*')):
        raise RuntimeError('Native交付包含Python源码或字节码')
    return manifest


def main():
    h.OUT = h.REPO / 'target/explicit-super-evidence'
    h.WORK = h.REPO / 'target/explicit-super-work'
    h.OUT.mkdir(exist_ok=False)
    h.WORK.mkdir(exist_ok=False)
    python = Path(sys.executable).resolve(strict=True)
    cli = h.REPO / 'target/debug/ryupy'
    runtime = h.REPO / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
    if sys.version_info[:3] != (3, 12, 13) or h.sha(python) != FIXED_PYTHON_SHA256:
        raise RuntimeError('必须使用已核固定CP3.12.13')
    identity = (h.sha(cli), h.sha(runtime))
    baseline = json.loads((h.REPO / 'target/wcwidth-aot-evidence/compiler-identity.json').read_text())
    if identity != (baseline['cli_sha256'], baseline['runtime_sha256']):
        raise RuntimeError('不能与旧Legacy阶段混用工具')
    sources = {str(p.relative_to(FIXTURES)): h.sha(p) for p in sorted(FIXTURES.rglob('*')) if p.is_file()}
    if (FIXTURES / 'default/subject.py').read_bytes() != (FIXTURES / 'instance/subject.py').read_bytes():
        raise RuntimeError('默认与Instance不是同源合同')
    h.save('inputs.json', {'source_sha256': sources, 'python_sha256': h.sha(python),
        'cli_sha256': identity[0], 'runtime_sha256': identity[1], 'commit': os.environ['GITHUB_SHA'],
        'pending_note': '原case仍pending；这里显式编译执行，不把pending跳过当通过'})
    original_out = h.OUT
    for mode in ['default', 'instance']:
        h.OUT = original_out / mode
        h.OUT.mkdir()
        h.COMMANDS = []
        root = h.WORK / mode
        shutil.copytree(FIXTURES / mode, root)
        entry = root / 'entry.py'
        strict('cp', [python, '-B', entry], expected=EXPECTED)
        output = h.OUT / 'jit'
        args = [cli]
        if mode == 'instance':
            args += ['--instance-module', 'subject']
        h.run('compile', args + ['build', entry, '--out-dir', output], timeout=300)
        h.save('plan.json', check_plan(output / 'module-environment.json', mode == 'instance'))
        if h.sha(output / 'RyuPy.Runtime.dll') != identity[1]:
            raise RuntimeError('JIT实际runtime身份改变')
        for pic in ['1', '0']:
            strict('jit-pic-' + pic, [os.environ['DOTNET'], output / 'entry.dll'],
                   env=dict(os.environ, RYUPY_PIC=pic), expected=EXPECTED)
    h.OUT = original_out / 'native'
    h.OUT.mkdir()
    h.COMMANDS = []
    entry = h.WORK / 'default/entry.py'
    image = prepare_image(DockerRunner(), 'docker', h.WORK)
    h.run('publish', [cli, 'build', entry, '--aot'], timeout=1500)
    dist = entry.with_suffix('.dist')
    exe = dist / 'entry'
    if exe.read_bytes()[:4] != b'\x7fELF':
        raise RuntimeError('非ELF')
    h.save('manifest-check.json', check_native(dist))
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
        '--entrypoint', '/bin/sh', image, '-c', script, 'explicit-super', '/app/entry'],
        expected=EXPECTED, timeout=300)
    after, _ = snapshot(relocated)
    if before != after or dist.exists() or identity != (h.sha(cli), h.sha(runtime)):
        raise RuntimeError('产物搬迁或工具身份改变')
    h.save('result.json', {'passed': True, 'before': before, 'after': after,
                          'scope': '普通单继承显式二参super Legacy Native'})
    h.OUT = original_out
    h.save('result.json', {'passed': True, 'jit_modes': ['default', 'explicit-instance'],
                          'aot': 'Legacy only', 'source_sha256': sources})


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        h.OUT.mkdir(parents=True, exist_ok=True)
        h.save('result.json', {'passed': False, 'error': str(error), 'traceback': traceback.format_exc()})
        raise
    finally:
        for path in h.WORK.rglob('*'):
            if path.is_file() and path.name in {'publish.stdout.log', 'publish.stderr.log', 'Host.csproj', 'Program.cs'}:
                target = h.REPO / 'target/explicit-super-evidence/publish' / path.relative_to(h.WORK)
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(path, target)
