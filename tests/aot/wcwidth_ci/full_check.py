"""同job串行完整门禁；默认和pilot由独立job隔离构建目录，不混工具。"""
import os
import sys
import xml.etree.ElementTree as ET
from pathlib import Path
import run as h


def main():
    h.OUT = h.REPO / 'target/full-check-evidence'
    h.OUT.mkdir(parents=True, exist_ok=True)
    python = Path(os.environ['PYTHON']).resolve(strict=True)
    if python != Path(sys.executable).resolve() or sys.version_info[:3] != (3, 12, 13) or sys.implementation.name != 'cpython':
        raise RuntimeError('full_check必须由固定CPython3.12.13启动')
    # e2e/driver大多数测试读取PYTHON；ForEach专测独立读取RYUPY_FOREACH_PYTHON。
    env = dict(os.environ, PYTHON=str(python), RYUPY_FOREACH_PYTHON=str(python))
    mode = os.environ['CHECK_MODE']
    if mode not in ('default', 'pilot'):
        raise RuntimeError('未知检查模式')
    feature = 'ryupy-driver/ordinary-dynamic-module-pilot' if mode == 'pilot' else ''
    args = ['make', 'check', 'CARGO_FEATURES=' + feature]
    h.save('mode.json', {'mode': mode, 'feature': feature, 'python': sys.version,
                       'commit': os.environ['GITHUB_SHA'], 'python_sha256': h.sha(python),
                       'interpreter_environment': {key: env[key] for key in ('PYTHON', 'RYUPY_FOREACH_PYTHON')}})
    h.run('python-child-version', [python, '--version'], env=env, expected=b'Python 3.12.13\n')
    for name, cmd in [('git', ['git', 'rev-parse', 'HEAD', 'HEAD^{tree}']),
                      ('rustc', ['rustc', '--version']), ('cargo', ['cargo', '--version']),
                      ('dotnet', ['dotnet', '--info'])]:
        h.run(name, cmd)
    h.run('metadata-cp-oracle', [python, '-B', h.HERE / 'metadata_oracle.py'], env=env)
    h.run('make-plan', ['make', '-n', 'check', 'CARGO_FEATURES=' + feature], env=env)
    h.run('make-check', args, env=env, timeout=4800)
    # 正常完整门已运行PIC默认配置；另开进程核直接runtime PIC禁用，不以0测试冒通过。
    pic_results = h.OUT / 'explicit-super-pic-off'
    h.run('explicit-super-pic-off', [os.environ['DOTNET'], 'test', 'runtime/RyuPy.Runtime.Tests',
        '--no-build', '--filter', 'FullyQualifiedName~PyExplicitSuperTests',
        '--logger', 'trx;LogFileName=results.trx', '--results-directory', pic_results],
        env=dict(env, RYUPY_PIC='0'), timeout=300)
    counters = ET.parse(pic_results / 'results.trx').find('.//{*}Counters')
    if counters is None or int(counters.attrib['passed']) < 1 or int(counters.attrib['failed']) != 0:
        raise RuntimeError('显式super禁用PIC未得到实际测试通过结果')
    h.run('aot-script-tests', [sys.executable, '-B', '-m', 'unittest', 'discover',
        '-s', 'tests/aot', '-p', 'test_*.py'], timeout=300)
    h.run('instance-aot-script-tests', [python, '-B', h.HERE / 'test_instance_gate.py'], env=env)
    h.run('instance-pe-script-tests', [python, '-B', h.HERE / 'test_instance_pe.py'], env=env)
    h.run('explicit-super-script-tests', [python, '-B', h.HERE / 'test_explicit_super_gate.py'], env=env)
    h.run('archive-tests', [sys.executable, '-B', h.HERE / 'test_evidence.py'])
    h.run('project-plan-tests', [sys.executable, '-B', h.HERE / 'test_project_plans.py'])
    h.run('interpreter-routing-tests', [python, '-B', h.HERE / 'test_full_check.py'], env=env)
    h.save('result.json', {'passed': True, 'mode': mode})


if __name__ == '__main__':
    main()
