"""同job串行完整门禁；默认和pilot由独立job隔离构建目录，不混工具。"""
import os
import sys
import run as h


def main():
    h.OUT = h.REPO / 'target/full-check-evidence'
    h.OUT.mkdir(parents=True, exist_ok=True)
    mode = os.environ['CHECK_MODE']
    if mode not in ('default', 'pilot'):
        raise RuntimeError('未知检查模式')
    feature = 'ryupy-driver/ordinary-dynamic-module-pilot' if mode == 'pilot' else ''
    args = ['make', 'check', 'CARGO_FEATURES=' + feature]
    h.save('mode.json', {'mode': mode, 'feature': feature, 'python': sys.version,
                       'commit': os.environ['GITHUB_SHA']})
    for name, cmd in [('git', ['git', 'rev-parse', 'HEAD', 'HEAD^{tree}']),
                      ('rustc', ['rustc', '--version']), ('cargo', ['cargo', '--version']),
                      ('dotnet', ['dotnet', '--info'])]:
        h.run(name, cmd)
    h.run('make-plan', ['make', '-n', 'check', 'CARGO_FEATURES=' + feature])
    h.run('make-check', args, timeout=4800)
    h.run('aot-script-tests', [sys.executable, '-B', '-m', 'unittest', 'discover',
        '-s', 'tests/aot', '-p', 'test_*.py'], timeout=300)
    h.run('archive-tests', [sys.executable, '-B', h.HERE / 'test_evidence.py'])
    h.run('project-plan-tests', [sys.executable, '-B', h.HERE / 'test_project_plans.py'])
    h.save('result.json', {'passed': True, 'mode': mode})


if __name__ == '__main__':
    main()
