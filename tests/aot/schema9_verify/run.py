"""执行未改用户11步联合分发合同，另核schema9原包/搬迁包及资源坏包。"""
import argparse
import copy
import json
import os
from pathlib import Path
import runpy
import shutil
import subprocess
import sys
from observer import Observer, verify
from common import digest, require, save, sentinels, tree

HERE = Path(__file__).resolve().parent
USER = HERE.parent/'stdlib_distribution'


def negatives(cli, package, root, env, marker):
    manifest = json.loads((package/'ryupy-artifact.json').read_text())
    resource = manifest['resources'][0]['relative_path']; baseline = tree(package); rows = []
    def case(label, mutate):
        target = root/'raw'/'mutations'/label; target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copytree(package, target); current = copy.deepcopy(manifest); mutate(target, current)
        save(target/'ryupy-artifact.json', current)
        rows.append(verify(cli, target, label, root/'raw'/'verify-logs', root, env, marker, False))
        if label == 'illegal-resource-name':
            error = (root/'raw'/'verify-logs'/(label+'.stderr')).read_text(encoding='utf-8')
            require('资源' in error and 'ASCII' in error, '必须是portable ASCII资源名诊断，不能用缺文件替代')
            rows[-1]['diagnostic_contract'] = '资源 portable ASCII 名称拒绝'
        require(tree(package) == baseline, '负例改了保留原包')
    def tamper(target, _):
        path = target/resource
        with path.open('r+b') as stream:
            first = stream.read(1); require(first, '资源必须非空'); stream.seek(0); stream.write(bytes([first[0]^1]))
    case('resource-tamper', tamper)
    case('resource-missing', lambda p, _: (p/resource).unlink())
    case('extra-file', lambda p, _: (p/'unlisted.txt').write_bytes(b'extra'))
    def illegal_name(target, current):
        old = target/resource; new = old.with_name('非ASCII.txt'); old.rename(new)
        relative = new.relative_to(target).as_posix()
        next(a for a in current['artifacts'] if a['relative_path'] == resource)['relative_path'] = relative
        current['resources'][0]['relative_path'] = relative
        # 文件闭集、长度和摘要仍自洽：若只放宽schema版本却漏资源portable规则，此例会误过。
        require(digest(new) == current['resources'][0]['sha256'], '改名不能改资源字节')
    case('illegal-resource-name', illegal_name)
    case('unknown-schema', lambda _, m: m.update(schema_version=999))
    return rows


def main():
    parser = argparse.ArgumentParser()
    for name in ['ryupy','verify-cli','out']: parser.add_argument('--'+name, type=Path, required=True)
    parser.add_argument('--python', type=Path, required=True)
    parser.add_argument('--rid', choices=['linux-x64','osx-arm64','win-x64'], required=True)
    args = parser.parse_args(); root = args.out.resolve(); root.mkdir(exist_ok=False)
    (root/'raw').mkdir(); (root/'sdk').mkdir()
    cli = args.ryupy.resolve(); verifier = args.verify_cli.resolve()
    fixture_before = tree(USER); save(root/'raw'/'fixture-before.json', fixture_before)
    env, marker = sentinels(root/'sentinels', os.environ)
    observer = Observer(cli, verifier, root, args.rid, env, marker)
    result = {'passed': False, 'rid': args.rid, 'observation': '原用户命令与返回对象不变；增加同步verify/副本观察',
              'tools': {str(p): {'sha256': digest(p), 'bytes': p.stat().st_size} for p in [cli,verifier,args.python]},
              'scope': 'schema9真实联合包的文件完整性，不是签名/完整schema语义或无SDK干净机证明'}
    saved_argv = sys.argv[:]
    try:
        sys.argv = [str(USER/'run.py'), '--ryupy', str(cli), '--python', str(args.python), '--out', str(root/'raw'/'user')]
        subprocess.run = observer
        try:
            runpy.run_path(str(USER/'run.py'), run_name='__main__')
        finally:
            subprocess.run = observer.real_run
            sys.argv = saved_argv
        require(set(observer.observed) == {'original','relocated'}, '没有观察到两次真实包')
        user_results = list((root/'raw'/'user').glob('*/result.json')); require(len(user_results) == 1, '用户run不唯一')
        user_result = json.loads(user_results[0].read_text()); require(user_result['status'] == 'passed', '用户合同失败')
        expected = ['shared','object','archive','cpython','jit-build','jit-run','aot-build','native-original',
                    'missing-resource-build','native-relocated','native-missing-resource']
        require([s['step'] for s in user_result['steps']] == expected and len(observer.calls) == 11, '用户原11步不完整')
        require(all((s['exit'] != 0 if s['step'] in ['missing-resource-build','native-missing-resource'] else s['exit'] == 0)
                    for s in user_result['steps']), '用户真实退出码不同')
        original = root/'raw'/'original-package'; relocated = root/'raw'/'relocated-package'
        require(tree(original) == tree(relocated), '经过失败构建/搬迁后的完整字节权限不同')
        broken = Path(observer.observed['relocated']['actual_package'])
        result['final_user_missing_resource'] = verify(verifier, broken, 'user-final-resource-missing',
                            root/'raw'/'verify-logs', root, env, marker, False)
        result['negative_copies'] = negatives(verifier, relocated, root, env, marker)
        fixture_after = tree(USER); save(root/'raw'/'fixture-after.json', fixture_after)
        require(fixture_before == fixture_after, '修改了用户runner或fixture')
        require(all(digest(p) == v['sha256'] for p,v in result['tools'].items()), '工具变化')
        result.update(passed=True, user_result=user_result, observations=observer.observed)
    finally:
        subprocess.run = observer.real_run; sys.argv = saved_argv
        save(root/'raw'/'observer-calls.json', observer.calls)
        save(root/'raw'/'result.json', result)


if __name__ == '__main__': main()
