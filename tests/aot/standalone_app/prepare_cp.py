"""在指定仓库外目录复制锁定原源码/字体/egg-info，保存两个真实入口完整 CP 原流。"""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]

def sha(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()

def main():
    out = Path(sys.argv[1]).resolve()
    python = Path(sys.argv[2]).resolve()
    if out.is_relative_to(REPO):
        raise ValueError('证据目录必须在仓库外')
    out.mkdir(parents=True, exist_ok=False)
    config = json.loads((HERE / 'inputs.json').read_text())
    package = out / 'package'
    for name, digest in config['files'].items():
        source = REPO / config['source_root'] / name
        if sha(source) != digest:
            raise ValueError('锁定输入不符：' + name)
        target = package / name
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(source, target)
    shutil.copyfile(HERE / 'console_entry.py', out / 'console_entry.py')
    shutil.copyfile(HERE / 'resources.json', out / 'resources.json')
    request = {'schema_version': 1, 'mode': 'closed_snapshot',
               'roots': [{'id': 'pyfiglet-1.0.2', 'path': 'package'}]}
    (out / 'metadata-request.json').write_text(json.dumps(request, indent=2) + '\n')
    env = {k:v for k,v in os.environ.items() if not k.startswith('PYTHON')}
    home = out / 'home'; home.mkdir()
    env.update(HOME=str(home), PYTHONPATH=str(package), PYTHONDONTWRITEBYTECODE='1',
               PYTHONNOUSERSITE='1')
    rows = []
    streams = []
    for font in config['fonts'] + ['list-fonts']:
        pair = []
        for name, entry in [('upstream', package / config['upstream_entry']),
                            ('console', out / 'console_entry.py')]:
            label = font + '-' + name
            cwd = out / (label + '-cwd')
            cwd.mkdir()
            arguments = ['--list_fonts'] if font == 'list-fonts' else ['-f', font, 'hi']
            cmd = [str(python), '-B', str(entry)] + arguments
            result = subprocess.run(cmd, cwd=cwd, env=env, capture_output=True, timeout=60)
            (out / (label + '.stdout')).write_bytes(result.stdout)
            (out / (label + '.stderr')).write_bytes(result.stderr)
            rows.append({'entry': name, 'font': font, 'command': cmd, 'exit': result.returncode,
                         'stdout_sha256': hashlib.sha256(result.stdout).hexdigest(),
                         'stderr_sha256': hashlib.sha256(result.stderr).hexdigest()})
            pair.append((result.returncode, result.stdout, result.stderr))
        streams.append(pair)
    passed = all(a == b and a[0] == 0 and not a[2] for a, b in streams)
    (out / 'result.json').write_text(json.dumps({'python_version': subprocess.check_output(
        [str(python), '--version']).decode().strip(), 'python_sha256': sha(python),
        'inputs_sha256': sha(HERE / 'inputs.json'), 'runs': rows, 'passed': passed}, indent=2) + '\n')
    if not passed:
        raise RuntimeError('真实 CP 两入口失败或完整原流不一致')

if __name__ == '__main__':
    main()
