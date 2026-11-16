"""新消费者Native草案：复用既有AOT layout/manifest/搬迁/隔离工具，唯一publish及三次运行。"""
import argparse
import json
import os
from pathlib import Path
import sys


def verify_pe_review(review, published):
    """真实执行前最后一门：固定审核域与同次PE/manifest身份必须随证据完整传递。"""
    if not (review.get('admitted') is True and review.get('pe_sha256') == published['managed_sha256']
            and review.get('unit_link_mapping_verified') is True and review.get('frame_carriers_verified') is True
            and review.get('role') == 'new' and review.get('case_id') == 'native_consumers'
            and review.get('location_mode') == 'native_consumers_aot'
            and review.get('native_manifest_sha256') == published['manifest_sha256']):
        raise ValueError('必须先完成同PE/manifest的Native固定来源/producer审查')


def main():
    p = argparse.ArgumentParser(description=__doc__)
    for name in ('repo', 'input', 'out', 'tools', 'oracle'):
        p.add_argument('--' + name, required=True, type=Path)
    p.add_argument('--phase', required=True, choices=['publish', 'run'])
    p.add_argument('--publish-evidence', type=Path)
    p.add_argument('--pe-review', type=Path)
    args = p.parse_args()
    sys.path.insert(0, str(args.repo / 'tests/aot/pyfiglet_ci'))
    sys.path.insert(1, str(args.repo / 'tests/aot'))
    from run import Recorder, base_environment
    from guards import require, sha, save
    from aot_layout import reported_aot
    from manifest_verify import read_sdk_version
    from manifest_files import check_artifacts
    import shutil
    from docker_image import prepare_image
    from native_evidence import preserve_dist, snapshot
    require(os.environ.get('GITHUB_ACTIONS') == 'true' and os.environ.get('RUNNER_ENVIRONMENT') == 'github-hosted', '只允许既有hosted路线')
    tools = json.loads(args.tools.read_text())
    require(tools['role'] == 'new', '旧工具不能当新消费者Native门')
    for row in tools['files'].values():
        require(sha(Path(row['path'])) == row['sha256'], '工具身份漂移')
    contract = json.loads((args.input / 'contract.json').read_text())
    for name, digest in contract['files'].items():
        require(sha(args.input / name) == digest, '完整fixture改变')
    require(tools['source_head'] == os.environ['GITHUB_SHA'], 'Native不是同HEAD候选')
    try:
        args.out.mkdir(parents=True, exist_ok=False)
        run = Recorder(args.out / 'logs')
        cli = Path(tools['files']['cli']['path'])
        dotnet = Path(tools['files']['dotnet']['path'])
        runtime = Path(tools['files']['runtime']['path'])
        env = base_environment(dotnet, runtime)
        root = args.input / 'fixtures/native_consumers'
        entry = root / 'entry.py'
        expected = (args.oracle / 'native_consumers/stdout').read_bytes()
        status = json.loads((args.oracle / 'native_consumers/result.json').read_text())
        require(status['exit'] == 0 and not (args.oracle / 'native_consumers/stderr').read_bytes(), 'CP Native完整输入不是正常成功')
        require(status['entry_sha256'] == sha(entry), 'CP入口不是同字节')
        if args.phase == 'publish':
            # 既有固定容器准备只在hosted，产品执行不pull、不联网、只挂真实dist。
            image = prepare_image(run, 'docker', args.out, env)
            target = args.out / 'product'
            built = run.run('publish', [cli, '--instance-module', 'sp_pkg.subject', 'build', entry,
                            '--aot', '--resources', root / 'resources.json', '--diagnostic-stages',
                            '--out-dir', target], cwd=args.out, env=env, check=True, timeout=1500)
            layout = reported_aot(built.stderr, target, entry.stem, 'linux-x64')
            before = preserve_dist(layout.dist, args.out)
            managed_inputs = args.out / 'managed-inputs'; managed_inputs.mkdir()
            for filename in (layout.managed.name, 'RyuPy.Runtime.dll'):
                shutil.copyfile(layout.managed.parent / filename, managed_inputs / filename)
                require(sha(managed_inputs / filename) == sha(layout.managed.parent / filename), 'Native managed复制漂移')
            sdk = read_sdk_version(layout.host / 'sdk.version')
            require(sdk == '10.0.403', '实际SDK不符')
            manifest = json.loads((layout.dist / 'ryupy-artifact.json').read_text())
            # 与既有instance_gate.check_manifest同域；通用Legacy validate_schema不接受Instance的null getter/setter。
            require(manifest['schema_version'] == 2 and manifest['target']['rid'] == 'linux-x64', 'Instance manifest schema/RID不符')
            check_artifacts(layout.dist, manifest['artifacts'], 'ryupy-artifact.json')
            require(sha(layout.managed.parent / 'RyuPy.Runtime.dll') == tools['files']['runtime']['sha256'], 'AOT runtime错配')
            sources = {'__main__': entry, 'sp_pkg': root / 'sp_pkg/__init__.py', 'sp_pkg.subject': root / 'sp_pkg/subject.py'}
            require({row['module_name'] for row in manifest['sources']} == set(sources), '实际Native源码图改变')
            require(all(row['compiled_text_sha256'] == sha(sources[row['module_name']]) for row in manifest['sources']), 'Native源码hash不符')
            modules = {row['name']: row for row in manifest['modules']}
            require(modules['sp_pkg.subject']['environment_reason'] == 'explicit-instance', 'subject不是显式Instance')
            require(modules['sp_pkg.subject']['getter'] is None and modules['sp_pkg.subject']['setter'] is None, 'Instance退回Legacy')
            require(modules['sp_pkg']['environment_reason'] != 'explicit-instance', '包被改为Instance')
            require(not manifest['resources'] and not manifest['scope']['resources_included'], '空锚点不能偷偷携带源码资源')
            require(not any(p.suffix.lower() in ('.py', '.pyc', '.pyo') for p in layout.dist.rglob('*')), 'dist含Python源/字节码')
            parsed = run.run('native-pe-read', [sys.executable, args.input / 'ci/read_pe.py',
                               layout.managed, 'new', 'native_consumers', '--native-manifest',
                               layout.dist / 'ryupy-artifact.json'], cwd=args.out, env=env, check=True, timeout=30)
            save(args.out / 'publish.json', {'managed': str(layout.managed), 'managed_sha256': sha(layout.managed),
                 'native': str(layout.native), 'native_sha256': sha(layout.native), 'dist': str(layout.dist),
                 'manifest_sha256': sha(layout.dist / 'ryupy-artifact.json'), 'image': image,
                 'before': before, 'contract_sha256': sha(args.input / 'contract.json'), 'source_head': tools['source_head'],
                 'status': 'published-awaiting-independent-pe-review'})
            return
        require(args.publish_evidence is not None and args.pe_review is not None, '缺真实publish或独立PE审查')
        published = json.loads((args.publish_evidence / 'publish.json').read_text())
        review = json.loads(args.pe_review.read_text())
        dist, native = Path(published['dist']), Path(published['native'])
        require(published['contract_sha256'] == sha(args.input / 'contract.json') and published['source_head'] == tools['source_head'], '发布与当前输入/HEAD不符')
        require(sha(Path(published['managed'])) == published['managed_sha256'] and sha(native) == published['native_sha256'] and
                sha(dist / 'ryupy-artifact.json') == published['manifest_sha256'], 'publish真实输入/交付漂移')
        verify_pe_review(review, published)
        image, before = published['image'], published['before']
        def positive(label, command, cwd):
            actual = run.run(label, command, cwd=cwd, env=env, timeout=30)
            require((actual.returncode, actual.stdout, actual.stderr) == (0, expected, b''), 'Native真实消费者不等于CP：' + label)
        positive('native-original', [native], args.out)
        moved = args.out / 'moved'
        shutil.move(dist, moved)
        binary = moved / native.name
        positive('native-moved', [binary], moved)
        script = ('for tool in dotnet python python3 python3.12; do '
                  'if command -v "$tool" >/dev/null 2>&1; then exit 97; fi; done; exec "$1"')
        positive('native-isolated', ['docker', 'run', '--pull=never', '--rm', '--network', 'none',
                 '--read-only', '--mount', f'type=bind,src={moved},dst=/app,readonly', '--workdir', '/app',
                 '--entrypoint', '/bin/sh', image, '-c', script, 'source-provenance', '/app/' + binary.name], args.out)
        after, _ = snapshot(moved)
        require(before == after, '搬迁改变交付物')
        require(all(sha(Path(row['path'])) == row['sha256'] for row in tools['files'].values()), 'Native前后工具改变')
        save(args.out / 'result.json', {'consumer_outputs_match_cp': True, 'publish': 1, 'native_runs': 3,
             'product_acceptance': False, 'independent_pe_and_original_ci_still_required': True})
    finally:
        # 只保实际SDK原流/生成输入，不上传obj/publish缓存全树。
        if args.phase == 'publish' and (args.out / 'product').exists():
            names = {'publish.stdout.log', 'publish.stderr.log', 'Host.csproj', 'Program.cs', 'sdk.version'}
            for path in (args.out / 'product').rglob('*'):
                if path.is_file() and path.name in names:
                    require(path.stat().st_size <= 16 * 1024 * 1024, 'SDK原流超过有界单文件预算')
                    target = args.out / 'sdk-evidence' / path.relative_to(args.out / 'product')
                    target.parent.mkdir(parents=True, exist_ok=True); shutil.copyfile(path, target)
        after_tools = {name: sha(Path(row['path'])) == row['sha256'] for name, row in tools['files'].items()}
        if args.out.exists():
            save(args.out / 'tools-after.json', after_tools)
        require(all(after_tools.values()), 'Native发布/运行前后工具改变')


if __name__ == '__main__':
    main()
