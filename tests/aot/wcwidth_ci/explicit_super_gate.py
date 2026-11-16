"""三分显式super证据：原Legacy完整正例、原Instance精确负例、小caller正例，独立Native。"""
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
from super_contract import check_rejection
from super_caller_gate import run_caller

EXPECTED = b'explicit super identity binding mutation phases\n'
FIXTURES = h.REPO / 'tests/cases/explicit_super'


def compile_command(cli, entry, output, instance):
    """CLI的JIT计划是显式诊断产物；两种模式都请求真实报告，不生成替代报告。"""
    args = [cli]
    if instance:
        args += ['--instance-module', 'subject']
    return args + ['build', entry, '--diagnostic-stages', '--out-dir', output]


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


def default_stage(python, cli, runtime_sha):
    root = h.WORK / 'default'
    shutil.copytree(FIXTURES / 'default', root)
    entry = root / 'entry.py'
    strict('cp', [python, '-B', entry], expected=EXPECTED)
    output = h.OUT / 'jit'
    h.run('compile', compile_command(cli, entry, output, False), timeout=300)
    h.save('plan.json', check_plan(output / 'module-environment.json', False))
    if h.sha(output / 'RyuPy.Runtime.dll') != runtime_sha:
        raise RuntimeError('JIT实际runtime身份改变')
    for pic in ['1', '0']:
        strict('jit-pic-' + pic, [os.environ['DOTNET'], output / 'entry.dll'],
               env=dict(os.environ, RYUPY_PIC=pic), expected=EXPECTED)
    h.save('result.json', {'passed': True, 'status': 'passed', 'scope': '原完整Legacy fixture双PIC'})


def negative_stage(python, cli):
    root = h.WORK / 'instance-negative'
    shutil.copytree(FIXTURES / 'instance', root)
    entry = root / 'entry.py'
    strict('cp', [python, '-B', entry], expected=EXPECTED)
    output = h.OUT / 'jit'
    if output.exists():
        raise RuntimeError('负例输出目录必须不存在')
    try:
        h.run('compile', compile_command(cli, entry, output, True), timeout=300)
    except RuntimeError:
        # 只接收已记录的非零编译退出；超时/缺命令/其它异常仍失败。
        if not h.COMMANDS or h.COMMANDS[-1].get('name') != 'compile':
            raise
    command = h.COMMANDS[-1]
    if command.get('timeout_expired'):
        raise RuntimeError('负例编译超时')
    check_rejection(command.get('exit'), (h.OUT / 'compile.stdout').read_bytes(),
                    (h.OUT / 'compile.stderr').read_text(), output)
    h.save('result.json', {'passed': True, 'status': 'unsupported_expected',
                          'scope': '原完整Instance fixture两处既有del E0100，不是正向能力通过'})


def aggregate(directory):
    expected = {'default': 'passed', 'instance-negative': 'unsupported_expected',
                'instance-caller': 'passed', 'native': 'passed'}
    results = {}
    for name, status in expected.items():
        result = json.loads((directory / name / 'result.json').read_text())
        if result.get('passed') is not True or result.get('status') != status:
            raise RuntimeError('显式super阶段缺失或未通过：' + name)
        results[name] = result
    return results


def native_stage(cli, identity):
    root = h.WORK / 'native'
    shutil.copytree(FIXTURES / 'default', root)
    entry = root / 'entry.py'
    runtime = h.REPO / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
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
    h.save('result.json', {'passed': True, 'status': 'passed', 'before': before, 'after': after,
                          'scope': '普通单继承显式二参super Legacy Native'})


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
    for mode in ['default', 'instance']:
        for name, expected in {
                'entry.py': 'ee3830ee21f0713cbc818fe34c3af920657cb6d8aea59996d9e1253439a05c3e',
                'subject.py': 'bcc4b3e39c78515b3310d6f9d0b27bd96f5418d53d0db137fdeb03da6ea8a484'}.items():
            if sources[mode + '/' + name] != expected:
                raise RuntimeError('原完整fixture字节被改写')
    h.save('inputs.json', {'source_sha256': sources, 'python_sha256': h.sha(python),
        'cli_sha256': identity[0], 'runtime_sha256': identity[1], 'commit': os.environ['GITHUB_SHA'],
        'pending_note': '原完整Instance仍pending且真实E0100；小caller是独立证据，不能冒原程序通过'})
    original_out = h.OUT
    failures = []
    # 各阶段新目录独立执行；任何失败保留原流，继续收集其它合同，最终仍严格失败。
    for stage in ['default', 'instance-negative', 'instance-caller', 'native']:
        h.OUT = original_out / stage
        h.OUT.mkdir()
        h.COMMANDS = []
        try:
            if stage == 'default':
                default_stage(python, cli, identity[1])
            elif stage == 'instance-negative':
                negative_stage(python, cli)
            elif stage == 'instance-caller':
                run_caller(python, identity[1])
            else:
                native_stage(cli, identity)
        except Exception as error:
            failures.append(stage)
            h.save('result.json', {'passed': False, 'status': 'failed', 'error': str(error),
                                  'traceback': traceback.format_exc()})
    h.OUT = original_out
    if failures:
        raise RuntimeError('独立阶段失败：' + ', '.join(failures))
    results = aggregate(original_out)
    if identity != (h.sha(cli), h.sha(runtime)):
        raise RuntimeError('同轮工具身份变化')
    h.save('result.json', {'passed': True, 'stages': results, 'source_sha256': sources,
                          'aot': '原完整Legacy only；小caller不验证Instance class AOT'})


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
