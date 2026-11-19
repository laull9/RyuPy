"""固定CP与默认/feature两编译臂的普通语义、已知差异和真实批量委托证据。"""
import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile

ROOT = Path(__file__).resolve().parents[3]
sys.path[:0] = [str(ROOT / 'tests'), str(ROOT / 'tests/perf')]
from benchlib import measure, suite
from aot_pair_evidence import save_manifest, require_space, cleanup_case


def compare(expected, actual, known=False):
    if known:
        assert expected == (0, b'False\n', b''), '既有CP差异发生变化'
        assert actual == (0, b'True\n', b''), '默认/候选出现新增差异或已修复，须重分类'
    else:
        assert actual == expected, '普通完整双流差分失败'


def roles(manifest, candidate, cross=False):
    data = json.loads(manifest.read_text())
    references = data['emitted_delegate_references']
    hits = [r for r in references if r['role'] == 'generator_sum_resume']
    assert bool(hits) == candidate, '真实批量委托发射与feature状态不符'
    if cross:
        assert len(hits) == (2 if candidate else 0), '跨模块批量入口必须精确两项或零项'
        assert all(r['target'] is not None for r in hits)
        assert len({json.dumps(r['target'], sort_keys=True) for r in hits}) == len(hits), '批量入口不可相互别名'
        sources = data['sources']; modules = {m['id']:m for m in data['modules']}
        assert len(sources) == len(data['modules']) == len(modules) == 3, '模块清单必须精确三项且ID唯一'
        source_ids = []
        for filename in ('main.py','producer.py','consumer.py'):
            matches = [x for x in sources if Path(x['logical_source']).name == filename]
            assert len(matches) == 1, '缺失或重复真实源模块'
            item = matches[0]
            assert item['compiled_text_sha256'] == '', '当前清单文本hash合同应为空，输入身份另行外部封存'
            logical = 'entry/main.py' if filename == 'main.py' else 'modules/'+filename
            module = '__main__' if filename == 'main.py' else filename[:-3]
            assert item['logical_source'] == logical and item['module_name'] == module, '源码与逻辑模块映射错误'
            assert modules[item['module_id']]['name'] == item['module_name'], '源模块与链接模块身份不符'
            source_ids.append(item['module_id'])
        assert len(set(source_ids)) == 3, '不同源码模块不可复用身份'
        producer = next(x['module_id'] for x in sources if x['module_name'] == 'producer')
        cross_targets(references, producer, candidate)
    return len(hits)


def cross_targets(references, producer, candidate):
    # 固定三工厂源：negative含DynUnary，保守不融合；不能只减少role数量。
    normal = [r for r in references if r['role'] == 'generator_resume']
    batch = [r for r in references if r['role'] == 'generator_sum_resume']
    prefix = f'm{producer}_<genexpr>'
    expected = [prefix+'$state_resume'+str(i) for i in (4, 5, 6)]
    assert len(normal) == 3 and {r['target']['name'] for r in normal} == set(expected)
    by_name = {r['target']['name']: r for r in normal}
    for r in normal:
        assert r['site']['source_method']['name'] == prefix
        assert r['target']['owner'] == r['site']['source_method']['owner']
        assert r['target']['signature'] == 'static [System.Runtime]System.Object ([System.Runtime]System.Object)'
    selected = [expected[0], expected[2]] if candidate else []
    assert len(batch) == len(selected)
    assert {r['target']['name'] for r in batch} == {n+'$sum_batch' for n in selected}
    for r in batch:
        ordinary = by_name[r['target']['name'].removesuffix('$sum_batch')]
        assert r['site']['source_method'] == ordinary['site']['source_method']
        assert r['target']['owner'] == ordinary['target']['owner']
        assert r['target']['signature'] == ordinary['site']['source_method']['signature']


CROSS_FILES = ('main.py', 'producer.py', 'consumer.py')


def seal_inputs(folder, cli, backend):
    """外部输入证据，不能称为编译器自证；固定git字节并在构建后复核。"""
    folder.mkdir(parents=True, exist_ok=False)
    head = subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip()
    tree = subprocess.check_output(['git', 'rev-parse', 'HEAD^{tree}'], cwd=ROOT, text=True).strip()
    records = []
    for name in CROSS_FILES:
        source = ROOT/'tests/fixtures/generator_sum/cross_module'/name
        data = source.read_bytes()
        assert data == subprocess.check_output(['git', 'show', head+':'+source.relative_to(ROOT).as_posix()], cwd=ROOT)
        (folder/name).write_bytes(data)
        records.append({'path': str(source), 'archive': name, 'sha256': hashlib.sha256(data).hexdigest(), 'bytes': len(data)})
    data = {'evidence_kind': 'external-input-not-compiler-self-attestation', 'head': head, 'tree': tree,
            'backend': backend, 'entry': records[0]['path'], 'sources': records,
            'cli': str(cli.resolve()), 'cli_sha256': hashlib.sha256(cli.read_bytes()).hexdigest()}
    (folder/'inputs.json').write_text(json.dumps(data, indent=2))
    return data


def verify_inputs(folder, data):
    assert subprocess.check_output(['git', 'rev-parse', 'HEAD'], cwd=ROOT, text=True).strip() == data['head']
    assert subprocess.check_output(['git', 'rev-parse', 'HEAD^{tree}'], cwd=ROOT, text=True).strip() == data['tree']
    assert hashlib.sha256(Path(data['cli']).read_bytes()).hexdigest() == data['cli_sha256']
    for item in data['sources']:
        archived = (folder/item['archive']).read_bytes()
        assert Path(item['path']).read_bytes() == archived, '构建期间源输入改变'
        assert len(archived) == item['bytes'] and hashlib.sha256(archived).hexdigest() == item['sha256']
    command = json.loads((folder/'command.json').read_text())
    argv = command['command']
    assert command['cwd'] == str(ROOT) and Path(argv[0]).resolve() == Path(data['cli'])
    assert argv[1:3] == ['build', data['entry']] and argv[3] == '-o'
    assert argv[5:] == (['--aot', '--native'] if data['backend'] == 'aot' else ['--native'])
    (folder/'verified-after-build.json').write_text(json.dumps(data, indent=2))


def main():
    p = argparse.ArgumentParser(); p.add_argument('--output', type=Path, required=True)
    for key in ('python', 'old', 'new'): p.add_argument('--'+key, type=Path, required=True)
    args = p.parse_args(); output = args.output.resolve()
    assert not output.is_relative_to(ROOT)
    output.mkdir(parents=True, exist_ok=False)
    version = subprocess.check_output([str(args.python), '-c', 'import platform;print(platform.python_version())'], text=True).strip()
    assert version == '3.12.13'
    os.environ.pop('RYUPY_RUNTIME_DIR', None)
    env = measure._base_env(); env.pop('RYUPY_RUNTIME_DIR', None)
    rows = []

    def run(label, command, cwd=ROOT, environment=env):
        r = subprocess.run(command, cwd=cwd, env=environment, capture_output=True, timeout=300)
        for name, value in [('stdout', r.stdout), ('stderr', r.stderr), ('exit', str(r.returncode).encode())]:
            (output/(label+'.'+name)).write_bytes(value)
        return r.returncode, r.stdout, r.stderr

    with tempfile.TemporaryDirectory(prefix='generator-product-') as temp:
        owned = Path(temp)
        for name, relative, known in [
            ('ordinary', 'tests/e2e/phase3/m3_generator_sum_fusion.py', False),
            ('known', 'tests/perf/generator_known/empty_start_identity.py', True),
            ('cross', 'tests/fixtures/generator_sum/cross_module/main.py', False),
        ]:
            source = ROOT/relative; bench = suite.Benchmark(name=source.stem, path=source)
            expected = run(name+'-CP', [str(args.python), '-B', str(source)])
            assert expected[0] == 0
            for label, cli in [('off', args.old), ('on', args.new)]:
                for backend in ('jit', 'aot'):
                    arm = name+'-'+label+'-'+backend; work = owned/arm; work.mkdir()
                    require_space(owned, 4*1024**3)
                    sealed = output/'inputs'/arm
                    inputs = seal_inputs(sealed, cli, backend) if name == 'cross' else None
                    exe, _, error, status = measure.build_case(ROOT, cli, bench, work, backend=backend,
                        command_evidence=sealed/'command.json' if inputs else None)
                    (output/(arm+'-build.log')).write_text(error)
                    if inputs:
                        verify_inputs(sealed, inputs)
                    assert exe is not None, (arm, status, error)
                    count = None
                    if backend == 'aot':
                        manifest = save_manifest(exe, output/'manifests', label, name)
                        count = roles(manifest, label == 'on', name == 'cross')
                    command = [str(exe)] if backend == 'aot' else ['dotnet', str(exe)]
                    compare(expected, run(arm, command), known)
                    if backend == 'aot':
                        moved = work/'中文 空格搬迁'; shutil.copytree(exe.parent, moved)
                        exe.parent.rename(exe.parent.with_name(exe.parent.name+'.hidden'))
                        compare(expected, run(arm+'-moved', [str(moved/exe.name)], moved,
                                              env | {'PATH': '/nonexistent'}), known)
                    rows.append({'arm': arm, 'known_incompatibility': known, 'batch_delegate_references': count})
                    (output/'completed.json').write_text(json.dumps(rows, indent=2))
                    cleanup_case(work, owned)
    assert len(rows) == 12
    (output/'complete.txt').write_text('12 semantic arms; known difference separately classified\n')


if __name__ == '__main__': main()
