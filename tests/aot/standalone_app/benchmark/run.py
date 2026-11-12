"""同一pyfiglet真实应用的Linux standalone对照；不安装工具、不自动接CI。"""
import argparse
import json
import os
from pathlib import Path
import platform
import re
import shutil
import statistics
import sys
import traceback
from measure import capture, elf, inventory, positive, require, save, sha

HERE = Path(__file__).resolve().parent
AOT = HERE.parents[1]
sys.path.insert(0, str(AOT))
from aot_layout import reported_aot
import manifest_verify


def nuitka_command(python, entry, package, output):
    return [python, '-B', '-m', 'nuitka', '--mode=standalone', '--jobs=1', '--disable-ccache',
            '--output-dir='+str(output), '--output-filename=pyfiglet-native',
            '--report='+str(output/'compilation-report.xml'), '--include-package=pyfiglet',
            '--include-distribution-metadata=pyfiglet',
            '--include-data-dir='+str(package/'pyfiglet/fonts')+'=pyfiglet/fonts',
            '--include-data-dir='+str(package/'pyfiglet.egg-info')+'=pyfiglet.egg-info', entry]


def ryupy_command(cli, entry, source, output):
    return [cli, '--distribution-metadata', source/'metadata-request.json', 'build', entry,
            '-I', source/'package', '--include-module', 'pyfiglet', '--include-module', 'pyfiglet.fonts',
            '--aot', '--resources', HERE.parent/'resources.json', '-o', output]


def validate_inputs(source):
    contract = json.loads((HERE.parent/'contract.json').read_text())
    for name, digest in contract['files'].items():
        require(sha(HERE.parent/name) == digest, '应用合同文件变化：'+name)
    config = json.loads((HERE.parent/'inputs.json').read_text())
    package = source/'package'
    files = {p.relative_to(package).as_posix(): sha(p) for p in package.rglob('*') if p.is_file()}
    require(files == config['files'] and not any(p.is_symlink() for p in package.rglob('*')), 'prepared不是固定源码/字体/完整egg-info')
    require(sha(source/'console_entry.py') == contract['files']['console_entry.py'], 'metadata入口改变')
    request = json.loads((source/'metadata-request.json').read_text())
    require(request == {'schema_version': 1, 'mode': 'closed_snapshot',
                       'roots': [{'id': 'pyfiglet-1.0.2', 'path': 'package'}]}, 'metadata根域不同')


def preserved_resources(package, dist):
    request = json.loads((HERE.parent/'resources.json').read_text())
    required = [package/Path(*module['module'].split('.'))/name
                for module in request['modules'] for name in module['files']]
    required += sorted((package/'pyfiglet.egg-info').glob('*'))
    require(required and all(p.is_file() and not p.is_symlink() for p in required), '字体/metadata输入非法')
    hashes = {sha(p) for p in dist.rglob('*') if p.is_file() and not p.is_symlink()}
    require(all(sha(p) in hashes for p in required), 'standalone未保完整原字体/egg-info字节')


def summarize(rows):
    # 四种次序轮转，30正式进程每入口每工具；冷指新进程，不是冷页缓存。
    require(len(rows) == 120, '样本不完整，不生成速度比')
    result = {}
    for case in ('cli', 'metadata'):
        arms = {arm: [r['wall_ms'] for r in rows if r['case'] == case and r['arm'] == arm]
                for arm in ('ryupy', 'nuitka')}
        require(all(len(v) == 30 for v in arms.values()), '分母不相等')
        result[case] = {arm: {'median_ms': statistics.median(v),
            'MAD_ms': statistics.median(abs(x-statistics.median(v)) for x in v), 'samples_ms': v}
            for arm, v in arms.items()}
        result[case]['ryupy_over_nuitka'] = statistics.median(arms['ryupy'])/statistics.median(arms['nuitka'])
    return result


def run(options):
    require(sys.platform == 'linux' and platform.machine() in ('x86_64', 'AMD64'), '首平台仅Linux x64')
    out = options.output.resolve(); out.mkdir(parents=True, exist_ok=False)
    source = options.prepared.resolve(); package = source/'package'
    require(not out.is_relative_to(source) and not source.is_relative_to(out), '输入/产物必须分离')
    lock = json.loads((HERE/'nuitka-lock.json').read_text())
    require(sha(options.nuitka_archive) == lock['sha256'], 'Nuitka官方归档hash不符')
    frozen = {str(p.resolve()): sha(p) for p in [options.python, options.cli, options.runtime,
              options.dotnet, options.nuitka_archive, HERE/'nuitka-lock.json', HERE.parent/'resources.json']}
    validate_inputs(source)
    installed = json.loads(options.nuitka_tree_receipt.read_text())
    require(installed and all(sha(p) == h for p, h in installed.items()), 'Nuitka安装树身份不符')
    frozen.update(installed)
    frozen.update({str(p.resolve()):sha(p) for p in options.oracle_root.rglob('*') if p.is_file()})
    frozen.update({str(p.resolve()):sha(p) for p in HERE.parent.rglob('*') if p.is_file() and '__pycache__' not in p.parts})
    source_before = inventory(source); save(out/'source-before.json', source_before)
    save(out/'tools.json', frozen)
    result = {'passed': False, 'compatible': False, 'performance': 'not-measured', 'builds': {}}
    env = {'PATH': os.environ['PATH'], 'HOME': str(out/'home'), 'LC_ALL': 'C.UTF-8', 'TZ': 'UTC',
           'PYTHONDONTWRITEBYTECODE': '1', 'PYTHONOPTIMIZE': '0', 'PYTHONHASHSEED': '0', 'PYTHONNOUSERSITE': '1',
           'DOTNET': str(options.dotnet), 'DOTNET_ROOT': str(options.dotnet.parent),
           'RYUPY_RUNTIME_DIR': str(options.runtime.parent), 'DOTNET_PROCESSOR_COUNT': '1',
           'DOTNET_CLI_DO_NOT_USE_MSBUILD_SERVER': '1', 'MSBUILDDISABLENODEREUSE': '1', 'UseSharedCompilation': 'false',
           'PYTHONPATH': str(package), 'NUITKA_CACHE_DIR': str(out/'nuitka-cache')}
    Path(env['HOME']).mkdir(); save(out/'environment.json', {'env': env, 'platform': platform.platform(),
        'cpuinfo': Path('/proc/cpuinfo').read_text(), 'build_policy': 'fresh output/cache; Nuitka jobs=1; DOTNET_PROCESSOR_COUNT=1; not proof of identical compiler parallelism'})
    entries = {'cli': package/'pyfiglet/__main__.py', 'metadata': source/'console_entry.py'}
    golden = (HERE.parent/'stdout.txt').read_bytes(); bundles = {}; paths = {}; rows = []
    contract = json.loads((HERE.parent/'contract.json').read_text())
    require(contract['additional_argv'] == [['-f', 'slant', 'hi'], ['--list_fonts']], '新增应用语义需显式纳入对照')
    variants = [('standard', contract['argv'], golden),
                ('slant', contract['additional_argv'][0], (HERE.parent/'slant.stdout.txt').read_bytes()),
                ('fonts', contract['additional_argv'][1], (HERE.parent/'fonts.stdout.txt').read_bytes())]
    def barrier():
        require(all(sha(p) == h for p, h in frozen.items()), '工具/输入发生变化')
        require(inventory(source) == source_before, '应用输入变化')
    def checked(label, argv, timeout=120, build=False, native_env=None, expected=None):
        record = capture(out/label, argv, out, env if native_env is None else native_env, timeout, build)
        if expected is not None: positive(record, out/label, expected)
        else: require(record['exit'] == 0 and record['failure'] is None, '命令失败：'+label)
        return record
    try:
        # 预恢复Native SDK依赖是setup；编译wall不混首轮NuGet下载。
        restore = out/'setup-restore'; restore.mkdir()
        project = restore/'dependencies.csproj'
        project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><PublishAot>true</PublishAot></PropertyGroup></Project>')
        checked('setup-native-restore', [options.dotnet, 'restore', project, '-r', 'linux-x64'], 300)
        checked('python-version', [options.python, '-B', '-c',
                'import sys; print(sys.version.split()[0], sys.flags.optimize, sys.flags.no_site, sys.flags.no_user_site, __debug__)'], expected=b'3.12.13 0 0 1 True\n')
        checked('nuitka-version', [options.python, '-B', '-m', 'nuitka', '--version'])
        require((out/'nuitka-version.stdout').read_text().splitlines()[0].strip() == lock['version'], '实际Nuitka版本不匹配')
        for case, entry in entries.items():
            for variant, args, expected in variants:
                checked(case+'-'+variant+'-cpython', [options.python, '-B', entry]+args, expected=expected)
        for case, entry in entries.items():
            for arm in (('ryupy', 'nuitka') if case == 'cli' else ('nuitka', 'ryupy')):
                barrier(); target = out/(case+'-'+arm+'-build'); target.mkdir()
                argv = ryupy_command(options.cli, entry, source, target) if arm == 'ryupy' else nuitka_command(options.python, entry, package, target)
                record = checked(case+'-'+arm+'-compile', argv, 600, True,
                                 native_env=dict(env, NUITKA_CACHE_DIR=str(target/'nuitka-cache')))
                if arm == 'ryupy':
                    layout = reported_aot((out/(case+'-'+arm+'-compile.stderr')).read_bytes(), target, entry.stem, 'linux-x64')
                    dist, executable = layout.dist, layout.native
                    data = json.loads(layout.manifest.read_text())
                    require(data['schema_version'] == 4 and data['resources'] and data['scope']['declared_resources_complete'] is True, '组合发布清单不是完整schema4资源合同')
                    manifest_verify.check_artifacts(dist, data['artifacts'], manifest_verify.MANIFEST_NAME)
                    require(sha(layout.managed.parent/'RyuPy.Runtime.dll') == sha(options.runtime), '实际Native runtime输入不同')
                    sys.path.insert(0, str(HERE.parent))
                    from validate_artifact import validate_app_artifact
                    receipt = validate_app_artifact(layout, source, frozen, case=case,
                        python=options.python, dotnet=options.dotnet, cli=options.cli, runtime=options.runtime, oracle_dir=options.oracle_root/('upstream' if case == 'cli' else 'console'),
                        output=out/(case+'-ryupy-product-audit'))
                    require(receipt.get('structure_validated') is True, '独立产品验收未签有效结构receipt')
                    save(out/(case+'-ryupy-product-receipt.json'), receipt)
                else:
                    dists = list(target.glob('*.dist')); require(len(dists) == 1, 'Nuitka standalone目录非唯一')
                    dist = dists[0]; executable = dist/'pyfiglet-native'
                    require((target/'compilation-report.xml').is_file(), 'Nuitka无编译来源报告')
                elf(executable); preserved_resources(package, dist)
                key = case+'-'+arm; bundles[key] = dist; paths[key] = executable
                snapshot = inventory(dist); save(out/(key+'-dist.json'), snapshot)
                result['builds'][key] = {'measurement': record, 'dist': snapshot,
                                        'executable_sha256': sha(executable)}
                save(out/'result.json', result)
        require(bool(re.fullmatch(r'sha256:[0-9a-f]{64}', options.runtime_image)), '必须固定同一个实际runtime镜像ID')
        checked('runtime-image-inspect', ['docker', 'image', 'inspect', options.runtime_image])
        checked('runtime-image-no-hosts', ['docker', 'run', '--rm', '--network', 'none', '--read-only',
                options.runtime_image, 'sh', '-c',
                'for p in python python3 dotnet; do if command -v "$p" >/dev/null 2>&1; then exit 9; fi; done'], expected=b'')
        barrier()
        hidden = source.with_name(source.name+'-benchmark-hidden'); require(not hidden.exists(), '隔离路径已有数据')
        source.rename(hidden); save(out/'source-isolation.json', {'restored': False})
        clean = {'PATH': '/usr/bin:/bin', 'LC_ALL': 'C.UTF-8', 'TZ': 'UTC'}
        try:
            for key, dist in bundles.items():
                before = result['builds'][key]['dist']
                for variant, args, expected in variants:
                    checked(key+'-'+variant+'-original', [paths[key]]+args, native_env=clean, expected=expected)
                require(inventory(dist) == before, '原位执行改变dist')
                moved = out/(key+'-relocated'); shutil.copytree(dist, moved, symlinks=True)
                require(inventory(moved) == before, '搬迁字节改变')
                paths[key] = moved/paths[key].name
                for variant, args, expected in variants:
                    checked(key+'-'+variant+'-relocated', [paths[key]]+args, native_env=clean, expected=expected)
                    checked(key+'-'+variant+'-clean-container', ['docker', 'run', '--rm', '--network', 'none',
                        '--read-only', '--cap-drop=ALL', '--security-opt=no-new-privileges', '--tmpfs', '/tmp',
                        '--mount', 'type=bind,src='+str(moved)+',dst=/app,readonly', '--workdir', '/tmp',
                        '--env', 'LC_ALL=C.UTF-8', '--env', 'TZ=UTC', options.runtime_image,
                        '/app/'+paths[key].name]+args, native_env=clean, expected=expected)
                require(inventory(moved) == before, '搬迁执行改变dist')
            result['compatible'] = True
            result['clean_container'] = {'validated': True, 'runtime_image': options.runtime_image, 'network': 'none', 'mounted': 'only dist read-only', 'semantic_runs': 12}
            save(out/'result.json', result)
            keys = list(paths)
            for index in range(32):
                order = keys[index % 4:]+keys[:index % 4]
                for key in order:
                    rec = checked(f'startup-{index:02d}-'+key, [paths[key], '-f', 'standard', 'hi'], native_env=clean, expected=golden)
                    if index >= 2:
                        case, arm = key.split('-', 1); rows.append(dict(case=case, arm=arm, round=index, wall_ms=rec['wall_ms']))
                save(out/'startup-samples.json', rows)
            result['startup'] = summarize(rows); result['performance'] = 'process startup only; not cold filesystem cache'
            result['steady_state'] = 'not-measured; short pyfiglet CLI exits, not a steady-state benchmark'
            for key in keys: require(inventory(paths[key].parent) == result['builds'][key]['dist'], '测量修改产物')
        finally:
            require(not source.exists(), '产品重建了输入目录'); hidden.rename(source)
            save(out/'source-isolation.json', {'restored': True})
        barrier(); result['passed'] = True
    except Exception:
        result['error'] = traceback.format_exc(); raise
    finally: save(out/'result.json', result)


def main():
    parser = argparse.ArgumentParser()
    for name in ('prepared', 'python', 'cli', 'runtime', 'dotnet', 'nuitka-archive', 'nuitka-tree-receipt', 'oracle-root', 'output'):
        parser.add_argument('--'+name, type=Path, required=True)
    parser.add_argument('--runtime-image', required=True)
    options = parser.parse_args()
    for name in ('prepared', 'python', 'cli', 'runtime', 'dotnet', 'nuitka_archive', 'nuitka_tree_receipt', 'oracle_root'):
        setattr(options, name, getattr(options, name).resolve(strict=True))
    run(options)


if __name__ == '__main__': main()
