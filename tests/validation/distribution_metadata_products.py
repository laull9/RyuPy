"""显式metadata的有限产品域：共用一次工具，CP/JIT独立采集，一次合成Native及原Markdown边界。"""
import json
import os
from pathlib import Path
import shutil
import distribution_metadata_contract as contract
import typing_noreturn as old
from distribution_metadata_identity import isolated

require, sha, save = old.require, old.sha, old.save
FIXTURES = old.ROOT / 'tests/validation/distribution_metadata_fixtures'


def prepare(r):
    r.inputs_dir = r.inputs(r.python)
    r.fixture = r.out / 'fixtures'
    shutil.copytree(FIXTURES, r.fixture)
    (r.inputs_dir / 'markdown_plain_probe.py').write_bytes(old.plain_probe(r.inputs_dir / 'markdown_sample.py'))
    save(r.inputs_dir / 'metadata-request.json', {'schema_version': 1, 'mode': 'closed_snapshot',
         'roots': [{'id': 'markdown', 'path': 'markdown'}]})
    require((r.inputs_dir / 'markdown/Markdown.egg-info/entry_points.txt').is_file(), '真实完整egg-info未解包')
    r.preserve(r.inputs_dir, 'inputs')
    r.preserve(r.fixture, 'fixtures')
    r.audit(lambda: r.freeze_inputs(r.inputs_dir))
    r.audit(lambda: r.freeze_inputs(r.fixture))


def argv(r, entry, target, request=None, include=False, markdown=False, stdlib=False):
    args = old.shared.build_command(r.cli, entry, r.inputs_dir, target) if markdown else [
        r.cli, 'build', entry, '--diagnostic-stages', '--out-dir', target]
    if not markdown:
        args += ['-I', r.fixture]
        if stdlib: args += ['-I', r.inputs_dir / 'stdlib']
        if include: args += ['--include-module', 'probe_pkg.target']
    if request is not None: args += ['--distribution-metadata', request]
    return args


def oracle(r, name, entry, golden, paths):
    env = dict(PATH='/usr/bin:/bin', LANG='C.UTF-8', LC_ALL='C.UTF-8', PYTHONNOUSERSITE='1',
               PYTHONHASHSEED='0', PYTHONUTF8='1', PYTHONDONTWRITEBYTECODE='1',
               PYTHONPATH=os.pathsep.join(map(str, paths)))
    # -c不会把entry父目录自动当模块根；缺include域因此可与CP缺源码根逐项对应。
    script = 'import sys; p=sys.argv[1]; exec(compile(open(p,"rb").read(),p,"exec"),{"__name__":"__main__","__file__":p})'
    label = name + '-cp'
    require(label not in r.labels, 'CP禁止重复执行')
    r.labels.add(label)
    cwd = r.out / 'cwd' / label; cwd.mkdir(parents=True)
    save(r.out / (label + '-environment.json'), env)
    r.barrier(label + '-before-execute')
    record = old.shared.capture(r.out / label, [r.python, '-S', '-B', '-c', script, entry], cwd, 30, env,
                                limit=128 * 1024, memory_limit=512 * 1024**2)
    r.barrier(label + '-after-execute')
    require(old.positive(record, r.out / label, golden), name + ' CP完整原流不等于冻结oracle')


def checkpoint_pe(r, name, target, entry, request):
    return r.audit(lambda: _checkpoint_pe(r, name, target, entry, request))


def _checkpoint_pe(r, name, target, entry, request):
    pe = target / (entry.stem + '.dll')
    require(pe.is_file() and sha(target / 'RyuPy.Runtime.dll') == sha(r.runtime), 'PE/runtime身份不符')
    report = None
    if request is not None:
        report, payload = contract.audit_pe(r, name, pe, target / 'distribution-metadata.json', request)
        key = str(request)
        if key in r.snapshot_reports: require(report == r.snapshot_reports[key], '同次JIT snapshot改变')
        else: r.snapshot_reports[key] = report
        if name == 'synthetic': r.payload = payload
    else:
        require(not (target / 'distribution-metadata.json').exists(), '未配置却出现快照审计')
    require((target / 'module-environment.json').is_file(), '执行前缺模块环境')
    # 一份PE保存，不把137MiB CLI复制到各域；实际目录及hash保到前置checkpoint。
    r.preserve(target, name + '-pe')
    save(r.out / (name + '-before-execution.json'), {'pe': str(pe), 'sha256': sha(pe),
         'runtime_sha256': sha(r.runtime), 'module_plan': json.loads((target / 'module-environment.json').read_text()),
         'distribution_metadata': report})
    return pe


def jit(r, name, entry, golden, request=None, include=False, markdown=False, stdlib=False):
    target = r.out / 'products' / name
    r.barrier(name + '-before-compile')
    result = r.command(name + '-compile', argv(r, entry, target, request, include, markdown, stdlib), 180, r.env)
    r.barrier(name + '-after-compile')
    require(result['exit'] == 0 and not result['failure'], '编译失败；本域JIT未运行')
    pe = checkpoint_pe(r, name, target, entry, request)
    r.run_positive(name + '-jit', [r.dotnet, pe], golden)


def pair(r, name, filename, request, include=False, paths=None):
    entry = r.fixture / filename
    golden = r.data['goldens'][name].encode()
    paths = paths if paths is not None else [r.fixture, r.fixture / 'root-a', r.fixture / 'root-b']
    if r.phase(name + '-cp', lambda: oracle(r, name, entry, golden, paths)):
        r.phase(name + '-jit', lambda: jit(r, name, entry, golden, request, include))
    else: r.block(name + '-jit', '同域CP不等于冻结oracle')


def products(r):
    if not r.phase('sourceprep', lambda: prepare(r)): return
    request = r.fixture / 'request.json'
    for name, filename, include in [('synthetic', 'main.py', True), ('binding', 'binding.py', True),
                                   ('parameters', 'parameters.py', False)]:
        pair(r, name, filename, request, include)
    pair(r, 'missing-include', 'missing_include.py', request, paths=[r.fixture / 'root-a', r.fixture / 'root-b'])
    pair(r, 'empty', 'empty.py', r.fixture / 'empty.json', paths=[])
    # 新独立域先收件；不改旧unconfigured断言或其身份失效出口。
    import distribution_metadata_authority as authority
    authority.run(r)
    r.phase('unconfigured-jit', lambda: jit(r, 'unconfigured', r.fixture / 'unconfigured.py',
            r.data['goldens']['unconfigured'].encode()))
    real_entry = r.fixture / 'real.py'
    real_request = r.inputs_dir / 'metadata-request.json'
    real_golden = r.data['goldens']['real-discovery'].encode()
    if r.phase('real-discovery-cp', lambda: oracle(r, 'real-discovery', real_entry, real_golden, [r.inputs_dir / 'markdown'])):
        r.phase('real-discovery-jit', lambda: jit(r, 'real-discovery', real_entry, real_golden, real_request))
    else: r.block('real-discovery-jit', '真实19条CP oracle失败')
    # plain/full彼此独立；只加metadata选项，原入口/include/77字节golden不改。
    full = (r.inputs_dir / 'stdout.txt').read_bytes()
    require(len(full) == 77, '原77字节oracle改变')
    for name, filename, golden in [('markdown-plain', 'markdown_plain_probe.py', full[:48]),
                                   ('markdown-full', 'markdown_sample.py', full)]:
        entry = r.inputs_dir / filename
        if r.phase(name + '-cp', lambda: oracle(r, name, entry, golden, [r.inputs_dir / 'stdlib', r.inputs_dir / 'markdown'])):
            r.phase(name + '-jit', lambda: jit(r, name, entry, golden, real_request, markdown=True))
        else: r.block(name + '-jit', '原Markdown CP失败')
    for name, row in r.data['search'].items():
        entry = old.ROOT / row['source']; require(sha(entry) == row['source_sha256'], '旧搜索源改变')
        golden = bytes.fromhex(row['stdout_hex']); label = 'search-' + name
        paths = [r.inputs_dir / 'stdlib'] if name == 'markupbase_updatepos' else []
        if r.phase(label + '-cp', lambda: oracle(r, label, entry, golden, paths)):
            r.phase(label + '-jit', lambda: jit(r, label, entry, golden, stdlib=bool(paths)))
        else: r.block(label + '-jit', '搜索CP失败')
    r.phase('collection-rejections', lambda: rejections(r))


REJECTIONS = {
    'unknown-field': ('分发请求schema无效:', 'unknown field `extra`'),
    'duplicate-id': ('分发根id重复',),
    'parent-path': ('分发路径不接受..越根组件',),
    'missing-root': ('No such file or directory (os error 2)',),
    'archive': ('封闭分发根不支持zip/egg archive：unsupported.egg',),
    'symlink': ('分发路径不接受链接',),
    'bad-identity': ('分发身份字段Name缺失或歧义',),
    'oversize': ('分发文件类型或尺寸超限',),
}


def rejection_diagnostic(case, record, stdout, stderr):
    text = stderr.decode('utf-8', errors='strict')
    require(record['exit'] == 1 and not record['failure'] and record['raw_streams_complete']
            and not stdout and len(text.splitlines()) == 1 and text.startswith('error: ')
            and all(token in text for token in REJECTIONS[case]) and '[ryupy-stage]' not in text,
            case + '不是对应采集错误；无关语法/启动/超时不得通过')
    # missing-root的OS错误没有路径上下文；结合已存在且冻结的入口/请求、缺失root、
    # 无graph-load事件与无产物，证明本有限调用在collect先于图加载处停止。
    if case == 'missing-root': require(text == 'error: No such file or directory (os error 2)\n', '缺root错误类别不符')


def rejections(r):
    # 每项真实CLI、独立输入/输出；拒绝错误不得静默退化成空快照。
    cases = list(REJECTIONS)
    for case in cases:
        root = r.out / 'rejections' / case; root.mkdir(parents=True)
        shutil.copytree(r.fixture / 'root-a', root / 'root')
        request = {'schema_version': 1, 'mode': 'closed_snapshot', 'roots': [{'id': 'x', 'path': 'root'}]}
        if case == 'unknown-field': request['extra'] = 1
        if case == 'duplicate-id': request['roots'] *= 2
        if case == 'parent-path': request['roots'][0]['path'] = '../root'
        if case == 'missing-root': request['roots'][0]['path'] = 'missing'
        if case == 'archive': (root / 'root/unsupported.egg').write_bytes(b'archive')
        if case == 'symlink': (root / 'root/link').symlink_to(root / 'root/Alpha_Pkg-1.dist-info/METADATA')
        if case == 'bad-identity': (root / 'root/Alpha_Pkg-1.dist-info/METADATA').write_text('Name: A\nName: B\nVersion: 1\n')
        if case == 'oversize': (root / 'root/Alpha_Pkg-1.dist-info/large').write_bytes(b'x' * (1024 * 1024 + 1))
        save(root / 'request.json', request)
        label = 'reject-' + case
        source = r.fixture / 'empty.py'; req = root / 'request.json'
        before = {'entry_sha256': sha(source), 'request_sha256': sha(req)}
        require(source.is_file() and req.is_file(), '采集拒绝前入口/请求不存在')
        if case == 'missing-root': require(not (root / 'missing').exists(), '缺根用例前提失效')
        r.barrier(label + '-before-compile')
        result = r.command(label, argv(r, source, root / 'output', req), 60, r.env)
        r.barrier(label + '-after-compile')
        r.audit(lambda: require(before == {'entry_sha256': sha(source), 'request_sha256': sha(req)}, '负例输入改变'))
        rejection_diagnostic(case, result, (r.out / (label + '.stdout')).read_bytes(), (r.out / (label + '.stderr')).read_bytes())
        require(not list((root / 'output').rglob('*.dll')), '采集拒绝却有PE产物')
        save(root / 'classification.json', dict(before, category=case, diagnostic_tokens=REJECTIONS[case],
             scope='固定请求采集先于graph-load；不接受其它编译失败'))


def native(r, markdown=False):
    name = 'markdown-aot' if markdown else 'synthetic-native'
    entry = r.inputs_dir / 'markdown_sample.py' if markdown else r.fixture / 'main.py'
    request = r.inputs_dir / 'metadata-request.json' if markdown else r.fixture / 'request.json'
    target = r.out / 'native-build' / name
    command = argv(r, entry, target, request, include=not markdown, markdown=markdown) + ['--aot']
    r.barrier(name + '-before-publish')
    record = r.command(name + '-publish', command, 1200, r.env)
    r.barrier(name + '-after-publish')
    # 原AOT早期E0100保持红；若通过，才消耗第二个publish并保产物，不追修下游。
    saved = r.out / 'native-evidence' / name; saved.mkdir(parents=True)
    for p in target.rglob('*') if target.exists() else []:
        if p.is_file() and (p.name in {'publish.stdout.log', 'publish.stderr.log', 'sdk.version', 'Program.cs',
                'module-environment.json', 'distribution-metadata.json', 'ryupy-artifact.json'} or p.suffix in {'.csproj', '.dll'}):
            if any(x in {'obj', 'bin', 'publish'} for x in p.relative_to(target).parts): continue
            dest = saved / p.relative_to(target); dest.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(p, dest)
    if record['exit'] != 0 or record['failure']:
        stderr = (r.out / (name + '-publish.stderr')).read_bytes()
        save(saved / 'boundary.json', {'passed': False, 'known_E0100': b'E0100' in stderr,
             'status': 'failed', 'native_execution': 'not-run', 'record': record})
        raise ValueError(name + '编译/publish真实失败；不是expected-fail通过')
    layout = r.audit(lambda: old.reported_aot((r.out / (name + '-publish.stderr')).read_bytes(), target, entry.stem, 'linux-x64'))
    r.audit(lambda: old.preserve_dist(layout.dist, saved))
    r.preserve(layout.dist, name + '-dist')
    report, _ = r.audit(lambda: contract.audit_pe(r, name, layout.managed, layout.managed.parent / 'distribution-metadata.json', request))
    if str(request) in r.snapshot_reports:
        r.audit(lambda: require(report == r.snapshot_reports[str(request)], 'AOT/JIT不是同一已验证snapshot'))
    else:
        save(saved / 'jit-comparison.json', {'status': 'blocked', 'reason': 'JIT未验证report；仅冻结输入的独立Native域'})
    sdk = r.audit(lambda: old.read_sdk_version(layout.host / 'sdk.version'))
    r.audit(lambda: require(sdk == '10.0.403', 'SDK不符'))
    r.audit(lambda: require(sha(layout.managed.parent / 'RyuPy.Runtime.dll') == sha(r.runtime), 'AOT runtime不符'))
    source_map = {'__main__': sha(entry)}
    if markdown:
        sources = json.loads((old.ROOT / 'tests/validation/text_search.json').read_text())['markdown_sources']
        source_map.update({key: sha(r.inputs_dir / row['path']) for key, row in sources.items()})
    else:
        source_map.update({'probe_pkg': sha(r.fixture / 'probe_pkg/__init__.py'),
                           'probe_pkg.target': sha(r.fixture / 'probe_pkg/target.py')})
    manifest = r.audit(lambda: contract.verify_native(layout.dist, layout.native, sdk, report, r.runtime, layout.managed, source_map))
    save(saved / 'validated-manifest.json', manifest)
    managed_saved = saved / 'managed'; managed_saved.mkdir()
    for source in layout.managed.parent.iterdir():
        if source.is_file(): shutil.copy2(source, managed_saved / source.name)
    r.preserve(managed_saved, name + '-managed')
    r.preserve(saved, name + '-before-execution')
    r.checked(name + '-elf', ['readelf', '-h', layout.native], 20)
    golden = (r.inputs_dir / 'stdout.txt').read_bytes() if markdown else r.data['goldens']['synthetic'].encode()
    env = {'PATH': '/usr/bin:/bin', 'LC_ALL': 'C.UTF-8', 'TZ': 'UTC'}
    # 原位和搬迁都不允许访问build时metadata目录；finally还原只为末次输入hash核验。
    roots = [request.parent / row['path'] for row in json.loads(request.read_text())['roots']]
    with isolated(r, roots, name):
        r.run_positive(name + '-original', [layout.native], golden, env)
        moved = r.out / 'relocated' / name; moved.parent.mkdir(exist_ok=True)
        binary = r.audit(lambda: contract.relocate(layout.dist, layout.native, moved))
        r.audit(lambda: contract.verify_native(moved, binary, sdk, report, r.runtime, layout.managed, source_map))
        r.preserve(moved, name + '-relocated')
        r.run_positive(name + '-relocated', [binary], golden, env)
