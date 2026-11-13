"""两个锁定真实应用的公开build、离线verify、搬迁运行及独立坏包验收。"""
import argparse
import json
import os
from pathlib import Path
import shutil
import sys
from common import capture, digest, require, save, sentinels, tree
from cases import mutation_cases, verify
sys.path.insert(0, str(Path(__file__).resolve().parents[1]))
from aot_layout import reported_aot

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]


def main():
    parser = argparse.ArgumentParser(); parser.add_argument('--base', type=Path, required=True)
    parser.add_argument('--rid', choices=['linux-x64', 'osx-arm64', 'win-x64'], required=True)
    args = parser.parse_args(); base = args.base.resolve()
    setup = json.loads((base/'setup.json').read_text()); require(setup['passed'], '准备未通过')
    cli, python, dotnet = map(Path, [setup['cli'], setup['python'], setup['dotnet']])
    env = dict(os.environ, DOTNET=str(dotnet), PYTHON=str(python), RYUPY_RUNTIME_DIR=str(cli.parent),
               PYTHONNOUSERSITE='1', PYTHONDONTWRITEBYTECODE='1', PYTHONOPTIMIZE='0')
    raw = base/'raw'; raw.mkdir(); cwd = base/'unrelated-cwd'; cwd.mkdir()
    verify_env, marker = sentinels(base/'sentinels', env)
    result = {'passed': False, 'commit': setup['commit'], 'rid': args.rid, 'apps': {},
              'scope': '文件闭集完整性、只读调用与同宿主搬迁；无签名、完整schema或执行兼容保证'}
    layouts = {}; goldens = {}; case_args = {}
    source_snapshots = {name: tree(base/name) for name in ['projects', 'sources']}
    save(raw/'source-trees-before.json', source_snapshots)
    try:
        for app in ['pyfiglet', 'binaryornot']:
            appraw = raw/app; appraw.mkdir(); logs = appraw/'logs'
            project = base/'projects'/app
            if app == 'pyfiglet':
                variants = {'standard': ['-f', 'standard', 'hi'], 'slant': ['-f', 'slant', 'hi'], 'fonts': ['--list_fonts']}
                entry = project/'console_entry.py'; target = 'metadata'; stem = 'console_entry'
            else:
                entry = project/'scanner.py'; target = 'scan'; stem = 'scanner'; variants = {}
                contract = json.loads((HERE.parent/'deployment_binaryornot/contract.json').read_text())
                for row in contract['cases']:
                    source = HERE.parent/'deployment_binaryornot'/row['input']; dest = appraw/row['input']
                    require(digest(source) == contract['files'][row['input']], '分类输入改变')
                    shutil.copyfile(source, dest); variants[row['id']] = [str(dest)]
            case_args[app] = variants; goldens[app] = {}
            cp_env = dict(env, PYTHONPATH=str(project/('package' if app == 'pyfiglet' else 'vendor')))
            for name, argv in variants.items():
                command = [python, '-B', *([] if app == 'pyfiglet' else ['-S']), entry, *argv]
                cp = capture(logs/('cp-'+name), command, cwd, cp_env)
                require(cp.returncode == 0 and cp.stderr == b'', 'CP原流失败')
                if app == 'pyfiglet':
                    filename = {'standard': 'stdout.txt', 'slant': 'slant.stdout.txt', 'fonts': 'fonts.stdout.txt'}[name]
                    expected = (HERE.parent/'standalone_app'/filename).read_bytes()
                    if name == 'fonts': expected = expected.replace(b'\n', os.linesep.encode())
                else:
                    expected = next(row['stdout'] for row in contract['cases'] if row['id'] == name)
                    expected = expected.replace('\n', os.linesep).encode()
                require(cp.stdout == expected, '冻结平台CP黄金不同')
                goldens[app][name] = cp.stdout
            output = base/'builds'/app
            build = capture(logs/'build', [cli, 'build', '--project', project/'ryupy.json', '--target', target, '-o', output], cwd, env, 1200)
            require(build.returncode == 0, '真实项目构建失败')
            layout = reported_aot(build.stderr, output, stem, args.rid); layouts[app] = layout
            require((layout.host/'sdk.version').read_text(encoding='utf-8-sig').strip() == '10.0.403', '实际SDK漂移')
            manifest = json.loads(layout.manifest.read_text())
            require(len(manifest.get('resources', [])) == (553 if app == 'pyfiglet' else 0), '真实资源集合数量不同')
            first = verify(cli, layout.dist, 'original', logs, cwd, verify_env, marker, True)
            moved = appraw/'moved'; shutil.copytree(layout.dist, moved)
            require(tree(moved) == tree(layout.dist), '搬迁复制字节/权限不同')
            result['apps'][app] = {'initial_verify': first, 'moved': str(moved), 'native_name': layout.native.name}
        # 同时移走全部源码根及SDK/build，不能仅改cwd而仍依赖原项目位置。
        for name in ['projects', 'sources', 'builds']:
            (base/name).rename(base/(name+'-hidden'))
        hidden_snapshots = {name: tree(base/(name+'-hidden')) for name in ['projects', 'sources']}
        save(raw/'source-trees-after.json', hidden_snapshots)
        require(hidden_snapshots == source_snapshots, '构建/原运行修改了项目或上游源')
        for app in ['pyfiglet', 'binaryornot']:
            row = result['apps'][app]; moved = Path(row['moved']); logs = raw/app/'logs'
            row['relocated_verify'] = verify(cli, moved, 'relocated', logs, cwd, verify_env, marker, True)
            row['native_runs'] = []
            for name, argv in case_args[app].items():
                before = tree(moved)
                actual = capture(logs/('native-'+name), [moved/row['native_name'], *argv], cwd, verify_env)
                require(actual.returncode == 0 and actual.stdout == goldens[app][name] and actual.stderr == b'', '搬迁Native不等于CP')
                require(tree(moved) == before and not marker.exists(), '运行写包或启动工具')
                row['native_runs'].append(name)
            row['mutations'] = mutation_cases(cli, moved, raw/app/'mutations', logs, cwd, verify_env, marker)
        require(all(digest(p) == identity['sha256'] for p, identity in setup['tools'].items()), '冻结工具改变')
        result['passed'] = True
    finally:
        save(raw/'result.json', result)


if __name__ == '__main__': main()
