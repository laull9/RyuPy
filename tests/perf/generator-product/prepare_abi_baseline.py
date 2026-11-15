#!/usr/bin/env python3
"""冻结可信dev并实际构建旧Runtime；输出可供PE测试消费的DLL哈希manifest，不负责clone。"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess

COMMIT = '81758bb477729aee4636f2ca6d6b0815b949849a'
SDK = '10.0.403'


def output(command, cwd):
    return subprocess.check_output(command, cwd=cwd, text=True).strip()


def verify_checkout(source):
    assert output(['git', 'rev-parse', 'HEAD'], source) == COMMIT, '基线不是固定dev提交'
    assert not output(['git', 'status', '--porcelain', '--untracked-files=all'], source), '基线工作树非干净'
    assert (source / 'runtime/RyuPy.Runtime/RyuPy.Runtime.csproj').is_file()


def prepare(source, evidence):
    source, evidence = source.resolve(), evidence.resolve()
    verify_checkout(source)
    assert not evidence.exists(), '拒绝复用旧证据目录'
    assert not evidence.is_relative_to(source), '证据/构建输出必须在基线checkout外'
    evidence.mkdir(parents=True)
    # 在外部工作目录固定SDK，不修改基线任何已跟踪文件。
    (evidence / 'global.json').write_text(json.dumps({'sdk': {
        'version': SDK, 'rollForward': 'disable', 'allowPrerelease': False}}))
    actual = output(['dotnet', '--version'], evidence)
    (evidence / 'sdk.txt').write_text(actual + '\n')
    assert actual == SDK, '实际SDK不符'
    (evidence / 'commit.txt').write_text(COMMIT + '\n')
    command = ['dotnet', 'build', str(source / 'runtime/RyuPy.Runtime/RyuPy.Runtime.csproj'),
               '-c', 'Release', '--artifacts-path', str(evidence / 'build'),
               '-p:UseSharedCompilation=false', '-p:MSBuildEnableWorkloadResolver=false']
    with (evidence / 'build.log').open('w') as log:
        result = subprocess.run(command, cwd=evidence, stdout=log, stderr=subprocess.STDOUT, timeout=1800)
    assert result.returncode == 0, '基线真实构建失败，见build.log'
    # 不接受obj/ref/refint替代真正运行时输出，也不猜测另一构建配置。
    dll = evidence / 'build/bin/RyuPy.Runtime/release/RyuPy.Runtime.dll'
    assert dll.is_file() and dll.stat().st_size > 0, '缺少真实Release DLL'
    verify_checkout(source)
    data = dll.read_bytes()
    manifest = {'commit': COMMIT, 'sdk': actual, 'configuration': 'Release',
                'source': str(source), 'dll': str(dll), 'bytes': len(data),
                'sha256': hashlib.sha256(data).hexdigest(), 'command': command}
    path = evidence / 'manifest.json'
    path.write_text(json.dumps(manifest, ensure_ascii=False, indent=2) + '\n')
    print(path)
    return path


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--source', required=True, type=Path)
    parser.add_argument('--evidence', required=True, type=Path)
    args = parser.parse_args()
    prepare(args.source, args.evidence)
