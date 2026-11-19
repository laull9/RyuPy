#!/usr/bin/env python3
"""两种独立目录布局的声明资源产品门；验证同次 CPython、三种交付和删除输入搬迁。"""
import argparse
import json
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

sys.path.insert(0, str(Path(__file__).resolve().parents[2] / 'cpython'))
from support import ROOT, built_product, environment, generated_entry, provider_data


# 一种由入口往父目录找数据，另一种由包内模块定位兄弟目录；名称、层数和数据格式独立。
CASES = (
    ('shop', 'source/tools/entry.py', {
        'source/tools/entry.py': """from http import HTTPMethod
from pathlib import Path
meta = Path(__file__).resolve().parents[1] / 'order.txt'
print('|'.join([meta.read_text(encoding='utf-8').strip(), HTTPMethod.GET]))
""",
        'source/order.txt': '货单 27\n',
    }, {'source': 'order.txt'}),
    ('observatory', 'source/python files/entry.py', {
        'source/python files/entry.py': """from archive import records
print(records.read())
""",
        'source/python files/archive/__init__.py': '',
        'source/python files/archive/records.py': """import json
from pathlib import Path
def read():
    config = Path(__file__).resolve().parents[2] / 'shared' / 'sky.json'
    return json.loads(config.read_text(encoding='utf-8'))['label']
""",
        'source/shared/sky.json': '{"label":"观测站 β"}\n',
    }, {'source': 'shared/sky.json'}),
)


def invoke(command, cwd, env):
    process = subprocess.run(list(map(str, command)), cwd=cwd, env=env, capture_output=True, timeout=600)
    if process.returncode:
        raise RuntimeError(f'{command!r} exit={process.returncode}\n{process.stderr.decode(errors="replace")}')
    return process


def verify(cli, python, modes):
    env = environment()
    with tempfile.TemporaryDirectory(prefix='ryupy-bundle-data-') as temporary:
        root = Path(temporary)
        assert not root.resolve().is_relative_to(ROOT)
        for name, relative_entry, files, declaration in CASES:
            for mode in modes:
                work = root / f'{name}-{mode}'
                inputs, output = work / 'inputs', work / 'output'
                for relative, content in files.items():
                    file = inputs / relative
                    file.parent.mkdir(parents=True, exist_ok=True)
                    file.write_text(content, encoding='utf-8')
                entry = inputs / relative_entry
                manifest = inputs / 'data.json'
                manifest.write_text(json.dumps({
                    'schema_version': 1, 'root': 'source', 'files': [declaration],
                }), encoding='utf-8')
                oracle = invoke([python, '-B', entry], work, env)
                invoke(['uv', 'venv', '--python', python, inputs / '.venv'], work, env)
                built = invoke([cli, entry, '--bundle-data', manifest, '--mode', mode, '-o', output],
                               work, dict(env, VIRTUAL_ENV=str(inputs / '.venv')))
                executable = generated_entry(built.stderr)
                data = provider_data(output, entry.stem, mode)
                report = json.loads((data / 'cpython-provider.json').read_text(encoding='utf-8'))
                destination = Path('ryupy_compiled') / declaration['source']
                assert report['bundle_data'] == [destination.as_posix()]
                assert not report['delegated_local_modules'] and report['entry_provider'] == 'ryupy_compiled_source'
                assert (data / 'python' / destination).read_bytes() == (inputs / 'source' / declaration['source']).read_bytes()
                config = json.loads((data / 'ryupy-cpython.json').read_text(encoding='utf-8'))
                assert all('ryupy_compiled' not in path for path in config['search_paths'])
                moved = work / '已搬迁 产品'
                if mode == 'onefile':
                    moved.mkdir()
                    shutil.copy2(executable, moved / executable.name)
                else:
                    shutil.copytree(built_product(built.stderr, output, entry.stem, mode).root, moved)
                shutil.rmtree(inputs)
                shutil.rmtree(output)
                actual = invoke([moved / executable.name], work,
                                dict(env, PATH='', PYTHONHOME='/missing-home', PYTHONPATH='/missing-path'))
                assert (actual.stdout, actual.stderr) == (oracle.stdout, oracle.stderr), (name, mode, actual, oracle)
                print(f'PASS {name} {mode}', flush=True)


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--cli', type=Path, required=True)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--mode', choices=('jit', 'standalone', 'onefile'), action='append')
    args = parser.parse_args()
    verify(args.cli.resolve(), args.python.absolute(), args.mode or ('jit', 'standalone', 'onefile'))
