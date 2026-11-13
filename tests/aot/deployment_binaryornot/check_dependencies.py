"""真实冻结分发的依赖预检反例；先于语法/SDK失败，不以非零退出本身冒充正确诊断。"""
import argparse
import json
import os
from pathlib import Path
import shlex
import shutil
import sys

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]
sys.path.insert(0, str(HERE.parent/'standalone_app/benchmark'))
from measure import capture, inventory, require, save, sha
from prepare import configuration, metadata_request
from runner import public_command

# 全部变体只在独立副本改动；正例的上游包和公开consumer始终保持原锁。
CASES = {
    'missing': 'chardet>=3.0.2',
    'version': 'chardet>=3.0.2',
    'marker': 'chardet>=3.0.2; python_version >= "3"',
    'extras': 'chardet[test]>=3.0.2',
    'url': 'chardet @ https://example.invalid/chardet.whl',
    'base-section': 'chardet>=3.0.2',
    'default-off': None,
}
INVALID_SOURCE = 'def :\n    pass\n'


def mutate(project, case):
    """返回本反例的原requirement；缺包删除仅限自建副本，绝不更改正例。"""
    requirement = CASES[case]
    if case in ('missing', 'default-off'):
        for name in ('chardet', 'chardet.egg-info'):
            shutil.rmtree(project/'vendor'/name)
    elif case == 'version':
        info = project/'vendor/chardet.egg-info/PKG-INFO'
        text = info.read_text(); require('Version: 5.2.0\n' in text, '版本反例未命中锁定版本')
        info.write_text(text.replace('Version: 5.2.0\n', 'Version: 2.0\n', 1))
    elif case == 'base-section':
        (project/'vendor/binaryornot.egg-info/requires.txt').write_text('[:python_version >= "3"]\nchardet>=3.0.2\n')
    else:
        (project/'vendor/binaryornot.egg-info/requires.txt').write_text(requirement+'\n')
    if case == 'default-off':
        path = project/'distribution.json'; request = json.loads(path.read_text())
        del request['dependency_check']; save(path, request)
    (project/'scanner.py').write_text(INVALID_SOURCE)
    return requirement


def successful_artifacts(output):
    """允许产品预留attempt目录；拒绝任何成功报告、托管入口或原生dist。"""
    if not output.exists(): return []
    return [p.relative_to(output).as_posix() for p in output.rglob('*')
            if p.name in ('scanner', 'ryupy-artifact.json', 'dependency-check.json')
            or p.suffix == '.dll' or (p.is_dir() and p.name.endswith('.dist'))]


def verify_rejection(case, record, prefix, output, sdk_marker):
    require(record['exit'] not in (0, None) and record['failure'] is None
            and record['raw_streams_complete'], '不是完整的正常拒绝退出')
    stdout = prefix.with_suffix('.stdout').read_bytes()
    stderr = prefix.with_suffix('.stderr').read_text()
    require(stdout == b'', '拒绝产生非空stdout')
    require(not sdk_marker.exists(), '依赖/语法拒绝之前调用了SDK')
    require(not successful_artifacts(output), '失败却留下成功产物')
    if case == 'default-off':
        require('依赖预检' not in stderr and ('E0001' in stderr or '语法' in stderr or 'syntax' in stderr.lower()),
                '默认关闭没有抵达原语法诊断')
    else:
        require('依赖预检' in stderr, '没有依赖预检诊断')
        require('E0001' not in stderr and 'invalid syntax' not in stderr.lower(), '语法抢先于依赖预检')
        if case == 'base-section':
            require('binaryornot' in stderr and 'python_version' in stderr, '诊断遗漏条件base段或父分发')
        else:
            require('chardet' in stderr and CASES[case] in stderr, '诊断遗漏依赖身份或原始requirement')
        category={'missing':'缺少分发','version':'版本不满足','marker':'环境marker',
                  'extras':'extras','url':'URL依赖','base-section':'base条件'}[case]
        require(category in stderr, '依赖诊断分类不符')
        if case in ('missing', 'version'):
            require('>=3.0.2' in stderr and 'binaryornot' in stderr, '诊断遗漏父分发或约束')
        if case == 'version': require('2.0' in stderr, '版本诊断遗漏已发现版本')
    return stderr


def check(cli, runtime, project, output):
    require(not output.exists(), '证据输出须不存在')
    require(not output.is_relative_to(project) and not project.is_relative_to(output), '证据与正例项目需分离')
    output.mkdir(parents=True)
    require(json.loads((project/'ryupy.json').read_text()) == configuration(), '正例配置改变')
    require(json.loads((project/'distribution.json').read_text()) == metadata_request(), '正例依赖请求改变')
    before = inventory(project); tools = {str(p): sha(p) for p in (cli, runtime)}
    save(output/'source-before.json', before); save(output/'tools.json', tools)
    result = {'passed': False, 'cases': [], 'timing_scope': '单次公开拒绝wall_ms；非性能比较'}
    try:
        for case, requirement in CASES.items():
            folder = output/case; folder.mkdir(); copy = folder/'project'
            shutil.copytree(project, copy); mutate(copy, case)
            snapshot = inventory(copy); save(folder/'source-before.json', snapshot)
            marker = folder/'sdk-invoked'; sdk = folder/'forbidden-dotnet'
            sdk.write_text('#!/bin/sh\nprintf invoked > '+shlex.quote(str(marker))+'\nexit 97\n'); sdk.chmod(0o755)
            sdk_hash = sha(sdk); out = folder/'build'; prefix = folder/'public-build'
            env = dict(os.environ, DOTNET=str(sdk), RYUPY_RUNTIME_DIR=str(runtime.parent))
            record = capture(prefix, public_command(cli, copy, out), folder, env, 120)
            verify_rejection(case, record, prefix, out, marker)
            require(inventory(copy) == snapshot and sha(sdk) == sdk_hash, '负例输入或SDK哨兵改变')
            require(inventory(project) == before and all(sha(p) == h for p,h in tools.items()), '正例或工具改变')
            result['cases'].append({'id': case, 'requirement': requirement, 'wall_ms': record['wall_ms'],
                                    'sdk_invoked': False, 'successful_artifacts': [], 'passed': True})
        require([row['id'] for row in result['cases']] == list(CASES), '反例漏跑或重复')
        result['passed'] = True
    finally: save(output/'result.json', result)
    return result


def main():
    parser = argparse.ArgumentParser()
    for key in ('cli', 'runtime', 'project', 'output'): parser.add_argument('--'+key, required=True, type=Path)
    args = parser.parse_args()
    check(*(getattr(args,key).resolve() for key in ('cli','runtime','project','output')))


if __name__ == '__main__': main()
