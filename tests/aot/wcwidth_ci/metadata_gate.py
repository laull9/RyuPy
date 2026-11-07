"""两个实际Native发布：无资源最小入口与kind0/kind2聚合模块合同；复用本轮冻结CLI/runtime。"""
from pathlib import Path
import shutil
from native_evidence import preserve_dist, snapshot


def execute(harness, cli, runtime):
    original_out, original_commands = harness.OUT, harness.COMMANDS
    fixtures = Path(__file__).with_name('metadata_fixtures')
    root = harness.WORK / 'metadata'
    shutil.copytree(fixtures, root)
    try:
        for name, expected in [('hello', b'hello\n'), ('main', b'metadata ok\n')]:
            harness.OUT = original_out / ('metadata-' + name)
            harness.OUT.mkdir()
            harness.COMMANDS = []
            harness.save('input.json', {'files': {p.relative_to(fixtures).as_posix(): harness.sha(p)
                for p in sorted(fixtures.rglob('*')) if p.is_file()}, 'expected_stdout_hex': expected.hex(),
                'contract': '无来源AOT登记偏差，不以CPython文件模块元数据作oracle',
                'cli_sha256': harness.sha(cli), 'runtime_sha256': harness.sha(runtime)})
            args = [cli, 'build', root / (name + '.py'), '--aot']
            if name == 'main':
                args += ['--resources', root / 'resources.json']
            harness.run('publish', args, timeout=1200)
            dist = root / (name + '.dist')
            exe = dist / name
            if exe.read_bytes()[:4] != b'\x7fELF' or not (dist / 'ryupy-artifact.json').is_file():
                raise RuntimeError('缺少ELF或manifest')
            before = preserve_dist(dist, harness.OUT)
            harness.run('elf-header', ['readelf', '-h', exe])
            harness.run('native', [exe], expected=expected)
            relocated = root / (name + '-relocated.dist')
            shutil.move(dist, relocated)
            harness.run('relocated', [relocated / name], cwd=relocated, expected=expected)
            after, _ = snapshot(relocated)
            if before != after or dist.exists():
                raise RuntimeError('搬移文件身份不一致')
            harness.save('result.json', {'passed': True, 'before': before, 'after': after,
                                         'original_dist_exists': dist.exists()})
    finally:
        harness.OUT, harness.COMMANDS = original_out, original_commands
