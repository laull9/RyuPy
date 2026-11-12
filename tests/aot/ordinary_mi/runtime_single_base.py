"""真实动态单基旧Native能力：CP、实际调用PE、双PIC、原位及无源码搬迁分别记账。"""
import json
import re
import shutil
from pathlib import Path
from contract import complete, positive, require
from aot_layout import reported_aot
from native_evidence import preserve_dist, snapshot
import manifest_verify as manifest


def fixture_identity(root, python_sha, sha, files=None):
    data = json.loads((root/'SOURCE.json').read_text())
    require(data['python'] == 'CPython 3.12.13' and data['python_sha256'] == python_sha,
            '动态单基CP来源变化')
    files = files or {'entry.py', 'bases.py', 'exports.py', 'stdout.txt'}
    require({row['path'] for row in data['files']} == files,
            '动态单基源码集合改变')
    require(len(data['files']) == len(files), '动态单基源码重复')
    for row in data['files']:
        path = root/row['path']
        require(path.is_file() and not path.is_symlink() and path.stat().st_size == row['size']
                and sha(path) == row['sha256'], '动态单基原件变化')
    return (root/'stdout.txt').read_bytes()


def prepare_calls(decoded, digest, count=5):
    require(decoded['sha256'] == digest, '解码PE身份变化')
    calls = [i['target'] for m in decoded['methods'] if m['body']
             for i in m['body']['instructions'] if 'target' in i]
    expected = {'owner': 'RyuPy.Runtime.PyOps', 'name': 'PrepareRuntimeDerivedClass',
                'signature_hex': '00021c0e1d1c'}
    actual = [c for c in calls if c['name'] == expected['name']]
    require(actual == [expected]*count, '真实动态单基调用数量或签名改变')
    require(not any(c['name'] == 'PrepareOrdinaryMiClass' for c in calls), '旧单基样例混入MI准备')
    return {'actual_calls': len(actual), 'signature': expected, 'managed_sha256': digest}


def native_output(record, stdout, stderr, golden, rejected_native):
    if not rejected_native:
        positive(record, stdout, stderr, golden)
        return
    complete(record, 1)
    message = 'NotImplementedError: runtime MI inheritance requires dynamic code support'
    lines = stderr.decode('utf-8').splitlines()
    require(not stdout and lines and lines[-1] == message and lines.count(message) == 1,
            '真实Native不是精确动态码能力拒绝')
    require(all(line == 'Traceback (most recent call last):' or line.startswith('  File "')
                for line in lines[:-1]), 'Native拒绝含额外诊断或崩溃')
    require(not re.search(rb'(?i)panic|segmentation|core dumped|fatal|stack overflow', stderr),
            '崩溃不是Native能力拒绝')


def managed(r, name, rejected_native):
    from run import ROOT, HERE, old
    # 只复制独立新样例，不修改旧MI黄金或其预发射拒绝合同。
    prefix = 'runtime-single-'+name+'-'
    source = HERE/'runtime_single_base_fixtures'/name
    files = {'entry.py', 'stdout.txt'} if rejected_native else None
    count = 1 if rejected_native else 5
    golden = fixture_identity(source, old.sha(r.python), old.sha, files)
    inputs = r.out/(prefix+'inputs'); shutil.copytree(source, inputs)
    r.freeze([p for p in inputs.iterdir() if p.is_file()])
    entry = inputs/'entry.py'
    label = prefix+'cp'; record = r.command(label, [r.python, '-B', entry], 30)
    positive(record, *r.streams(label), golden)
    jit = r.out/(prefix+'jit')
    r.checked(prefix+'jit-build', [r.cli, 'build', entry, '--out-dir', jit], 180)
    pe = jit/'entry.dll'
    require(pe.is_file() and old.sha(jit/'RyuPy.Runtime.dll') == old.sha(r.runtime), 'JIT真实输入变化')
    r.freeze([pe, jit/'RyuPy.Runtime.dll'])
    decoder = ROOT/'tests/validation/source_provenance/ci/read_pe_decoder.py'
    for mode, product in [('jit', pe)]:
        label = prefix+mode+'-decode'
        r.checked(label, [r.python, '-B', decoder, product, 'old', label], 90)
        r.save(label+'-verified.json', prepare_calls(json.loads(r.streams(label)[0]), old.sha(product), count))
    for pic in ('1', '0'):
        label = prefix+'pic'+pic
        record = r.command(label, [r.dotnet, pe], 30, dict(r.env, RYUPY_PIC=pic))
        positive(record, *r.streams(label), golden)
    return inputs, entry, golden, count, prefix


def execute(r, name, rejected_native):
    from run import ROOT, old
    inputs, entry, golden, count, prefix = managed(r, name, rejected_native)
    decoder = ROOT/'tests/validation/source_provenance/ci/read_pe_decoder.py'
    target = r.out/(prefix+'native-build'); label = prefix+'publish'
    record = r.command(label, [r.cli, 'build', entry, '--out-dir', target, '--aot'], 600)
    # 失败也保存必要SDK输入及原流；不归档整个obj/bin缓存。
    saved = r.out/(prefix+'publish-inputs'); saved.mkdir()
    for p in target.rglob('*') if target.exists() else []:
        if not p.is_file() or p.is_symlink() or any(x in {'obj', 'bin', 'publish'} for x in p.relative_to(target).parts): continue
        if p.suffix in ('.dll', '.csproj', '.json', '.rsp') or p.name in ('Program.cs', 'sdk.version', 'publish.stdout.log', 'publish.stderr.log'):
            dest = saved/p.relative_to(target); dest.parent.mkdir(parents=True, exist_ok=True); shutil.copy2(p, dest)
    complete(record, 0)
    layout = reported_aot(r.streams(label)[1], target, entry.stem, 'linux-x64')
    archive = r.out/(prefix+'native-dist'); archive.mkdir(); preserve_dist(layout.dist, archive)
    manifest.image_header(layout.native, 'linux-x64')
    require(old.sha(layout.managed.parent/'RyuPy.Runtime.dll') == old.sha(r.runtime), 'Native真实runtime输入变化')
    data = json.loads(layout.manifest.read_text())
    actual = manifest.check_artifacts(layout.dist, data['artifacts'], manifest.MANIFEST_NAME)
    executables = [x for x in data['artifacts'] if x['role'] == 'native_executable']
    require(len(executables) == 1 and actual[executables[0]['relative_path']].samefile(layout.native), 'Native入口非唯一原件')
    require(manifest.read_sdk_version(layout.host/'sdk.version') == '10.0.403', 'SDK身份变化')
    build_inputs = {x['role']: x for x in data['build_inputs']}
    require(build_inputs['runtime_managed_input']['sha256'] == old.sha(r.runtime)
            and build_inputs['compiled_managed_input']['sha256'] == old.sha(layout.managed), 'Native清单输入身份变化')
    r.freeze([layout.native, layout.managed, layout.manifest])
    label = prefix+'native-decode'
    r.checked(label, [r.python, '-B', decoder, layout.managed, 'old', label], 90)
    r.save(label+'-verified.json', prepare_calls(json.loads(r.streams(label)[0]), old.sha(layout.managed), count))
    # 隔离仅本门复制的源码，禁止Native依赖源码位置；finally恢复冻结输入。
    hidden = inputs.with_name(inputs.name+'-hidden'); frozen = dict(r.frozen)
    require(not hidden.exists(), '隔离位置冲突'); inputs.rename(hidden)
    r.frozen = {str(hidden/Path(p).relative_to(inputs)) if Path(p).is_relative_to(inputs) else p: h
                for p, h in r.frozen.items()}
    r.save(prefix+'source-isolation.json', {'restored': False})
    try:
        before = snapshot(layout.dist)
        label = prefix+'native-original'
        record = r.command(label, [layout.native], 30, {'PATH': '/usr/bin:/bin', 'LC_ALL': 'C.UTF-8', 'TZ': 'UTC'})
        native_output(record, *r.streams(label), golden, rejected_native)
        require(snapshot(layout.dist) == before, '原位Native修改dist')
        moved = r.out/(prefix+'relocated'); shutil.copytree(layout.dist, moved)
        require(snapshot(moved) == before, '搬迁产物变化')
        label = prefix+'native-relocated'
        record = r.command(label, [moved/layout.native.name], 30, {'PATH': '/usr/bin:/bin', 'LC_ALL': 'C.UTF-8', 'TZ': 'UTC'})
        native_output(record, *r.streams(label), golden, rejected_native)
        require(snapshot(moved) == before, '搬迁Native修改dist')
    finally:
        require(not inputs.exists(), '产品重建了隔离源码'); hidden.rename(inputs); r.frozen = frozen
        r.barrier(); r.save(prefix+'source-isolation.json', {'restored': True})
    r.result.setdefault('runtime_single_base_cases', {})[name] = {
        'passed': True, 'jit_executions': 2, 'native_executions': 2,
        'native_expected': 'capability-rejection' if rejected_native else 'success'}


def run(r):
    execute(r, 'ordinary', False)
    execute(r, 'mi_rejected', True)
    r.result['runtime_single_base_native'] = {'passed': True, 'jit_executions': 4,
        'native_executions': 4, 'sources_isolated': True, 'new_mi_native_supported': False}
