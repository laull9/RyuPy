"""固定CP与默认/feature两编译臂的普通语义、已知差异和真实批量委托证据。"""
import argparse
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


def roles(manifest, candidate):
    references = json.loads(manifest.read_text())['emitted_delegate_references']
    hits = [r for r in references if r['role'] == 'generator_sum_resume']
    assert bool(hits) == candidate, '真实批量委托发射与feature状态不符'
    return len(hits)


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
        ]:
            source = ROOT/relative; bench = suite.Benchmark(name=source.stem, path=source)
            expected = run(name+'-CP', [str(args.python), '-B', str(source)])
            assert expected[0] == 0
            for label, cli in [('off', args.old), ('on', args.new)]:
                for backend in ('jit', 'aot'):
                    arm = name+'-'+label+'-'+backend; work = owned/arm; work.mkdir()
                    require_space(owned, 4*1024**3)
                    exe, _, error, status = measure.build_case(ROOT, cli, bench, work, backend=backend)
                    (output/(arm+'-build.log')).write_text(error)
                    assert exe is not None, (arm, status, error)
                    count = None
                    if backend == 'aot':
                        manifest = save_manifest(exe, output/'manifests', label, name)
                        count = roles(manifest, label == 'on')
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
    assert len(rows) == 8
    (output/'complete.txt').write_text('8 semantic arms; known difference separately classified\n')


if __name__ == '__main__': main()
