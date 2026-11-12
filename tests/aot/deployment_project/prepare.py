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
    inputs = json.loads((APP/'inputs.json').read_text(encoding='utf-8'))
    contract = json.loads((APP/'contract.json').read_text(encoding='utf-8'))
    # 失败也保实际文件摘要/换行计数，不能只记录推测的checkout原因。
    observed = {}
    for name, digest in contract['files'].items():
        path = APP/name; raw = path.read_bytes() if path.is_file() else None
        observed[name] = {'expected_sha256': digest,
                          'actual_sha256': hashlib.sha256(raw).hexdigest() if raw is not None else None,
                          'CRLF': raw.count(b'\r\n') if raw is not None else None,
                          'LF': raw.count(b'\n') if raw is not None else None}
    output.parent.mkdir(parents=True, exist_ok=True)
    evidence = output.with_name(output.name+'-input-check.json')
    if evidence.exists(): raise ValueError('输入核验原件已存在，不覆盖')
    evidence.write_text(json.dumps(observed, indent=2)+'\n', encoding='utf-8')
    for name, row in observed.items():
        if row['actual_sha256'] != row['expected_sha256']:
            raise ValueError('原合同改变：'+name+' expected='+row['expected_sha256']+' actual='+str(row['actual_sha256']))
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
        (output/name).write_text(json.dumps(value, indent=2)+'\n', encoding='utf-8')
    return output


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('output', type=Path)
    parser.add_argument('--package', type=Path)
    args = parser.parse_args()
    inputs = json.loads((APP/'inputs.json').read_text(encoding='utf-8'))
    prepare(args.output, args.package or REPO/inputs['source_root'])


if __name__ == '__main__': main()
