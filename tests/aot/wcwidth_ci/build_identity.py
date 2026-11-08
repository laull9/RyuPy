"""只读采集既有fullcheck身份；不启动编译/测试，不把Debug与Release runtime混成同一个。"""
import hashlib
import json
import os
from pathlib import Path
import shutil
import sys

ROOT = Path(__file__).resolve().parents[3]
sys.path.insert(0, str(ROOT / 'tests'))
from run_projects_gate import candidate_identity


def file_identity(path):
    path = Path(path).resolve(strict=True)
    with path.open('rb') as stream:
        digest = hashlib.file_digest(stream, 'sha256').hexdigest()
    return {'path': str(path), 'sha256': digest}


def tool_identities():
    tools = {'python': file_identity(sys.executable)}
    for name in ('dotnet', 'cargo', 'rustc'):
        found = shutil.which(name)
        if found is None:
            raise RuntimeError('找不到本轮实际工具：' + name)
        tools[name] = file_identity(found)
    if os.environ.get('DOTNET') and file_identity(os.environ['DOTNET']) != tools['dotnet']:
        raise RuntimeError('Makefile实际dotnet与DOTNET选择不一致')
    return tools


def runtimes(repo):
    return {'managed-tests': file_identity(repo / 'runtime/RyuPy.Runtime.Tests/bin/Debug/net10.0/RyuPy.Runtime.dll'),
            'driver-e2e': file_identity(repo / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll')}


def write(output, value, exclusive=False):
    with (output / 'build-identity.json').open('x' if exclusive else 'w') as stream:
        stream.write(json.dumps(value, ensure_ascii=False, indent=2) + '\n')


def before(repo, output, mode, features):
    candidate = candidate_identity(repo)
    if candidate['head'] != os.environ['GITHUB_SHA'] or candidate['status']:
        raise RuntimeError('fullcheck不是同HEAD clean源码')
    write(output, {'schema': 1, 'mode': mode, 'cargo_features': features,
                  'candidate_before': candidate, 'tools_before': tool_identities(),
                  'state': 'before-make-check'}, exclusive=True)


def before_tests(repo, output):
    data = json.loads((output / 'build-identity.json').read_text())
    if data['state'] != 'before-make-check':
        raise RuntimeError('runtime测试前身份禁止重复/错序采集')
    data['runtimes_before_tests'] = runtimes(repo)
    data['state'] = 'before-existing-dotnet-test'
    write(output, data)


def after(repo, output):
    data = json.loads((output / 'build-identity.json').read_text())
    data['candidate_after'] = candidate_identity(repo)
    data['tools_after'] = tool_identities()
    # 构建阶段失败时不伪造runtime-before；仍先写出可得的前后源码/工具。
    data['state'] = 'make-check-finished'
    try:
        data['runtimes_after_tests'] = runtimes(repo)
    except OSError as error:
        data['runtime_after_error'] = str(error)
    write(output, data)
    if data['candidate_before'] != data['candidate_after'] or data['tools_before'] != data['tools_after']:
        raise RuntimeError('fullcheck源码/实际工具在运行中改变')
    if 'runtimes_before_tests' not in data or data.get('runtimes_after_tests') != data['runtimes_before_tests']:
        raise RuntimeError('fullcheck两个role的runtime未完整采集或前后改变')


if __name__ == '__main__':
    if sys.argv[1:] != ['runtimes-before'] or not os.environ.get('FULL_CHECK_EVIDENCE'):
        raise RuntimeError('仅允许既有Makefile测试前采集入口')
    before_tests(ROOT, Path(os.environ['FULL_CHECK_EVIDENCE']))
