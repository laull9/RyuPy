"""锁定tabulate/wcwidth原包与metadata-only项目；不改变上游、不构建、不下载隐式依赖。"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]

def sha(path): return hashlib.sha256(Path(path).read_bytes()).hexdigest()
def require(value, message):
    if not value: raise ValueError(message)

def configuration():
    return {'schema_version': 1, 'mode': 'aot',
            'search_paths': ['vendor/table', 'vendor/width'],
            'include_modules': ['tabulate', 'wcwidth'],
            'distribution_metadata': 'distribution.json',
            'targets': {'table': {'entry': 'tools/tabulate_console.py'}}}

def prepare(output, sources):
    output = Path(output).resolve()
    require(not output.exists() and not output.is_relative_to(REPO), '项目必须在仓库外新目录')
    contract = json.loads((HERE/'contract.json').read_text())
    for name, digest in contract['files'].items(): require(sha(HERE/name) == digest, '合同输入变化: '+name)
    packages = json.loads((HERE/'inputs.json').read_text())['packages']
    for name, row in packages.items():
        source = Path(sources[name]).resolve()
        for relative, digest in row['files'].items():
            path = source/relative
            require(path.is_file() and not path.is_symlink() and sha(path) == digest, '锁定原包不符: '+relative)
    output.mkdir(parents=True)
    for name, folder in [('tabulate', 'table'), ('wcwidth', 'width')]:
        for relative in packages[name]['files']:
            target = output/'vendor'/folder/relative; target.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(Path(sources[name])/relative, target)
    (output/'tools').mkdir(); shutil.copyfile(HERE/'console_entry.py', output/'tools/tabulate_console.py')
    metadata = {'schema_version': 1, 'mode': 'closed_snapshot', 'roots': [
        {'id': 'tabulate-0.9.0', 'path': 'vendor/table'}, {'id': 'wcwidth-0.2.13', 'path': 'vendor/width'}]}
    for name, value in [('ryupy.json', configuration()), ('distribution.json', metadata)]:
        (output/name).write_text(json.dumps(value, indent=2)+'\n')
    return output

def main():
    parser = argparse.ArgumentParser(); parser.add_argument('output', type=Path)
    parser.add_argument('--tabulate', type=Path); parser.add_argument('--wcwidth', type=Path)
    args = parser.parse_args(); packages = json.loads((HERE/'inputs.json').read_text())['packages']
    prepare(args.output, {name: getattr(args, name) or REPO/row['source_root'] for name, row in packages.items()})

if __name__ == '__main__': main()
