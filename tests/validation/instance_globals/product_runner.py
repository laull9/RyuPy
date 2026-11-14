# A1产物运行草稿：仅消费已核静态Host与配置；不构建、不下载、不重试。
import hashlib
import json
from pathlib import Path
import subprocess
import sys


def digest(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def verify_metadata(prefix, mode, contract):
    count = 4 if mode == 'functional' else 1 if mode == 'default' else 0
    paths = sorted(prefix.parent.glob(prefix.name + '.*.json'))
    if len(paths) != count:
        return False
    expected = contract[mode]
    if len(expected) != count:
        return False
    for index, target in enumerate(expected):
        path = prefix.parent / (prefix.name + '.' + str(index) + '.json')
        if not path.is_file():
            return False
        value = json.loads(path.read_text())
        if value != target:
            return False
        if (not isinstance(value, list) or any(not isinstance(x, list) or len(x) != 2
                or not isinstance(x[0], str) or not isinstance(x[1], list) for x in value)
                or len({x[0] for x in value}) != len(value)):
            return False
    return True


def final(root):
    labels = []
    for backend in ('jit', 'native', 'native-moved'):
        records = json.loads((root / backend / 'RUN.json').read_text())
        expected = [backend + '-' + mode for mode in ('functional', 'default', 'indirect')]
        if [r['label'] for r in records] != expected or any(not r['ok'] for r in records):
            raise RuntimeError('阶段未完成或失败: ' + backend)
        for row in records:
            for stream in ('stdout', 'stderr'):
                path = root / backend / (row['label'] + '.' + stream)
                if not path.is_file() or digest(path) != row[stream + '_sha256']: raise RuntimeError('原流缺失或变化')
            for item in row['metadata_files']:
                path = root / backend / item['name']
                if path.stat().st_size != item['size'] or digest(path) != item['sha256']:
                    raise RuntimeError('metadata原件变化')
        labels += expected
    (root / 'FINAL.json').write_text(json.dumps({'completed': labels, 'count': 9}, indent=2))


def main():
    if len(sys.argv) == 3 and sys.argv[1] == '--final':
        final(Path(sys.argv[2])); return
    if len(sys.argv) != 5 or sys.argv[1] != '--execute':
        raise SystemExit('需 --execute 已冻结配置 新输出目录 jit|native|native-moved')
    config_path = Path(sys.argv[2]).resolve()
    config = json.loads(config_path.read_text())
    def verify():
        for item in config['files']:
            if digest(item['path']) != item['sha256']:
                raise RuntimeError('输入/产物身份变化')
    verify()
    output = Path(sys.argv[3]).resolve()
    output.mkdir(exist_ok=False)
    expected = json.loads(Path(config['cp_expected']).read_text())
    results = []
    # 配置须独审精确锁JIT、Native原地、Native搬移三个真实命令及对应产物，不自行寻找。
    phase = sys.argv[4]
    if phase not in ('jit', 'native', 'native-moved') or [x['name'] for x in config['backends']] != [phase]:
        raise RuntimeError('后端名单不符')
    for backend in config['backends']:
        for mode in ('functional', 'default', 'indirect'):
            verify()
            label = backend['name'] + '-' + mode
            metadata = output / (label + '.metadata')
            argv = backend['argv'] + [mode, str(metadata)]
            (output / (label + '.command.json')).write_text(json.dumps({'argv': argv, 'cwd': backend['cwd'], 'env': config['env']}, indent=2))
            record = {'label': label, 'exit': None, 'timeout': False}
            try:
                proc = subprocess.run(argv, cwd=backend['cwd'], env=config['env'], capture_output=True, timeout=60)
                stdout, stderr = proc.stdout, proc.stderr
                record['exit'] = proc.returncode
            except subprocess.TimeoutExpired as error:
                stdout, stderr = error.stdout or b'', error.stderr or b''
                record['timeout'] = True
            except OSError as error:
                stdout, stderr = b'', str(error).encode()
                record['launch_error'] = type(error).__name__
            (output / (label + '.stdout')).write_bytes(stdout)
            (output / (label + '.stderr')).write_bytes(stderr)
            try:
                values = [json.loads(line) for line in stdout.decode().splitlines()]
            except (UnicodeError, ValueError):
                values = None
            wanted = [['product-indirect-canonical', 'NotImplementedError']] if mode == 'indirect' else expected[mode]
            record['stdout_sha256'] = hashlib.sha256(stdout).hexdigest()
            record['stderr_sha256'] = hashlib.sha256(stderr).hexdigest()
            record['metadata_files'] = [{'name': p.name, 'size': p.stat().st_size, 'sha256': digest(p)} for p in sorted(output.glob(metadata.name + '.*.json'))]
            try:
                record['metadata_ok'] = verify_metadata(metadata, mode, backend['metadata_expected'])
            except (ValueError, UnicodeError, OSError, KeyError, TypeError) as error:
                record['metadata_ok'] = False
                record['metadata_error'] = {'type': type(error).__name__, 'message': str(error)}
            record['ok'] = record['exit'] == 0 and not stderr and values == wanted and record['metadata_ok']
            results.append(record)
            (output / 'RUN.json').write_text(json.dumps(results, indent=2))
            verify()
            if not record['ok']:
                raise SystemExit(1)

if __name__ == '__main__':
    main()
