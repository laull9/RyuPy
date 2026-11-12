"""将已锁定pyfiglet测试输入整理为自足普通项目；不构建、不运行oracle或修改上游。"""
import argparse
import hashlib
import json
from pathlib import Path
import shutil

HERE = Path(__file__).resolve().parent
APP = HERE.parent/'standalone_app'
REPO = HERE.parents[2]


def sha(path):
    return hashlib.sha256(Path(path).read_bytes()).hexdigest()


def configuration():
    return {'schema_version': 1, 'mode': 'aot', 'search_paths': ['package'],
            'include_modules': ['pyfiglet', 'pyfiglet.fonts'],
            'resources': 'resources.json', 'distribution_metadata': 'metadata-request.json',
            'targets': {'cli': {'entry': 'package/pyfiglet/__main__.py'},
                        'metadata': {'entry': 'console_entry.py'}}}


def prepare(output, package):
    """package可为下载恢复后的原包；只复制锁定文件，不依赖任何已生成编译计划。"""
    output = Path(output).resolve(); package = Path(package).resolve()
    if output.exists() or output.is_relative_to(REPO):
        raise ValueError('项目必须为仓库外新目录')
    inputs = json.loads((APP/'inputs.json').read_text())
    contract = json.loads((APP/'contract.json').read_text())
    for name, digest in contract['files'].items():
        if sha(APP/name) != digest: raise ValueError('原合同改变：'+name)
    for name, digest in inputs['files'].items():
        path = package/name
        if path.is_symlink() or not path.is_file() or sha(path) != digest:
            raise ValueError('原包锁定输入不符：'+name)
    output.mkdir(parents=True)
    for name in inputs['files']:
        target = output/'package'/name; target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copyfile(package/name, target)
    for name in ('console_entry.py', 'resources.json'):
        shutil.copyfile(APP/name, output/name)
    request = {'schema_version': 1, 'mode': 'closed_snapshot',
               'roots': [{'id': 'pyfiglet-1.0.2', 'path': 'package'}]}
    for name, value in [('metadata-request.json', request), ('ryupy.json', configuration())]:
        (output/name).write_text(json.dumps(value, indent=2)+'\n')
    return output


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    parser.add_argument('--package', type=Path)
    args = parser.parse_args()
    inputs = json.loads((APP/'inputs.json').read_text())
    prepare(args.output, args.package or REPO/inputs['source_root'])


if __name__ == '__main__': main()
