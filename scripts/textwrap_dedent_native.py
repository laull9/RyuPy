"""dedent真实ILC发布与ELF运行：校验前先保存实际dist，原位及搬移均撤离全部编译输入根。"""
from pathlib import Path
import shutil
import textwrap_dedent_contract as c

old, require, sha, save = c.old, c.require, c.sha, c.save


def preserve_before_validation(runner, target, stem):
    """发布失败、清单错误或原位崩溃也必须留下SDK日志与已生成ELF，不能等验证成功才归档。"""
    runner.preserve_publish_evidence(target, stem)
    candidates = sorted(p for p in target.rglob('*.dist') if p.is_dir()) if target.exists() else []
    require(len(candidates) <= 4, '异常过多dist目录')
    records = []
    for index, dist in enumerate(candidates):
        destination = runner.out if len(candidates) == 1 else runner.out / ('raw-native-' + str(index))
        destination.mkdir(parents=True, exist_ok=True)
        old.preserve_dist(dist, destination)
        archive = destination / 'native-dist.tar.gz'
        runner.frozen[str(archive)] = sha(archive)
        records.append({'path': str(dist), 'archive': str(archive), 'sha256': sha(archive)})
    save(runner.out / 'raw-native-dist.json', records)
    return candidates


def native(runner, cases, contract=c):
    # 默认仍是原严格Legacy合同；新门必须显式提供自己的精确选型/清单合同。
    c = contract
    case = cases[0]
    previous, labels = runner.out, runner.labels
    runner.out = previous / 'native-domains' / case['id']
    runner.out.mkdir(parents=True, exist_ok=False)
    runner.labels = set()
    state = {'publish': 'not-run', 'validation': 'blocked', 'runs': {row['id']: {'original': 'blocked', 'relocated': 'blocked'} for row in cases}}
    try:
        require(shutil.disk_usage(runner.out).free >= 2 * 1024**3, 'Native磁盘不足2GiB，停止本域且不清资料')
        target = runner.out / 'native-build'; stem = Path(case['source']).stem
        runner.barrier('native-before-publish')
        state['publish'] = 'started'
        record = runner.command('native-publish', c.build_argv(runner, case, target, True), 600, runner.env)
        # 不以record成功与否控制原件保存；下方任何检查失败仍上传已经生成的ELF归档。
        raw = preserve_before_validation(runner, target, stem)
        runner.barrier('native-after-publish')
        require(c.complete(record), 'ILC publish失败/超时；本域Native未执行')
        state['publish'], state['validation'] = 'passed', 'started'
        layout = old.reported_aot((runner.out / 'native-publish.stderr').read_bytes(), target, stem, 'linux-x64')
        runner.audit(lambda: require(len(raw) == 1 and raw[0].samefile(layout.dist), '真实reported dist不等于已归档唯一产物'))
        runner.preserve(layout.dist, 'native-original')
        manifest = runner.audit(lambda: c.verify_native(runner, case, layout))
        save(runner.out / 'native-manifest.json', manifest)
        saved = runner.out / 'native-managed'; saved.mkdir()
        for path in layout.managed.parent.iterdir():
            if path.is_file(): shutil.copy2(path, saved / path.name)
        runner.preserve(saved, 'native-managed')
        runner.frozen[str(layout.managed)] = sha(layout.managed)
        # 新门可加真实PE语义证据；失败只拒本图，不把普通结构红误作全局换源。
        if hasattr(c, 'verify_native_structure'): c.verify_native_structure(runner, case, layout)
        # 空cwd、最小环境，无Python/DOTNET工具查找；repo原fixture仍保留，范围在独立记录里明示。
        env = {'PATH': '/usr/bin:/bin', 'LC_ALL': 'C.UTF-8', 'TZ': 'UTC'}
        state['validation'] = 'passed'
        moved = runner.out / 'relocated'
        binary, _ = old.relocate_manifest(layout.dist, layout.native, moved, 'linux-x64', runner.data['sdk_version'])
        runner.audit(lambda: require(old.snapshot(moved) == old.snapshot(layout.dist), '搬移全部文件不等'))
        runner.preserve(moved, 'native-relocated')
        with c.isolated(runner, [runner.inputs_dir], 'all-compiled-inputs'):
            require(all(not (runner.inputs_dir / p).exists() for p in case['modules'].values()), '实际编译来源原路径仍存在')
            for row in cases:
                current = state['runs'][row['id']]
                try:
                    current['original'] = 'started'
                    runner.run_positive(row['id'] + '-original', [layout.native, *row['argv']], row['stdout'].encode(), env)
                    current['original'], current['relocated'] = 'passed', 'started'
                    runner.run_positive(row['id'] + '-relocated', [binary, *row['argv']], row['stdout'].encode(), env)
                    current['relocated'] = 'passed'
                except c.IdentityError:
                    raise
                except Exception as error:
                    current['error'] = str(error)
                    for key in ('original', 'relocated'):
                        if current[key] == 'started': current[key] = 'failed'
        require(all(row['original'] == row['relocated'] == 'passed' for row in state['runs'].values()), '部分真实Native进程失败，保全部域原流')
        return {'members': [row['id'] for row in cases], 'cp_equivalent': case['mode'] == 'cp',
                'expected_ni': case['mode'] == 'ni', 'compiled_input_roots_removed': True, 'all_machine_sources_removed': False}
    finally:
        for name, value in state.items():
            if value == 'started': state[name] = 'failed'
        save(runner.out / 'native-stages.json', state)
        runner.out, runner.labels = previous, labels
