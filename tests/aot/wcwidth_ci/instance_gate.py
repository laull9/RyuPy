"""显式 Instance AOT：真实产品资格/PE门先于两次 publish，原流和搬迁产物单独留存。"""
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import traceback
import run as h
from docker_image import prepare_image
from native_evidence import preserve_dist, snapshot

FIXTURES = h.HERE / 'instance_fixtures'
SELECTED = ['wcwidth.wcwidth', 'lambda_subject', 'cycle_left', 'cycle_right',
            'reentry_subject', 'retry_subject']


def strict(name, args, **kwargs):
    h.run(name, args, **kwargs)
    if (h.OUT / (name + '.stderr')).read_bytes():
        raise RuntimeError(f'{name} stderr 非空，保留原流')


class DockerRunner:
    """适配既有固定镜像核验，不重写镜像身份约束。"""
    @property
    def logs(self):
        return h.OUT

    def run(self, label, command, **kwargs):
        kwargs.pop('check', None)
        h.run(label, command, **kwargs)
        return subprocess.CompletedProcess(command, 0,
            (h.OUT / (label + '.stdout')).read_bytes(),
            (h.OUT / (label + '.stderr')).read_bytes())


def tested(name, args, required, **kwargs):
    """cargo 的0测试退出码仍是0；每个必验具名结果必须实际为ok。"""
    h.run(name, args, **kwargs)
    lines = (h.OUT / (name + '.stdout')).read_text().splitlines()
    for test in required:
        if not any(line.startswith('test ') and line.endswith(test + ' ... ok') for line in lines):
            raise RuntimeError(f'{name} 缺少实际具名通过结果：{test}')


FIXED_PYTHON_SHA256 = '021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7'


def verify_tools(cli, runtime, python, identities, old_inputs):
    if identities != (h.sha(cli), h.sha(runtime)):
        raise RuntimeError('CLI/runtime与旧Legacy阶段冻结身份不符')
    if (h.sha(python) != FIXED_PYTHON_SHA256
            or old_inputs['python_sha256'] != FIXED_PYTHON_SHA256
            or old_inputs['python_path'] != str(python)
            or python != Path(os.environ['PYTHON']).resolve(strict=True)
            or old_inputs['ci_commit'] != os.environ['GITHUB_SHA']):
        raise RuntimeError('CPython或提交与固定工具/旧阶段冻结身份不符')


def check_manifest(dist, selected):
    manifest = json.loads((dist / 'ryupy-artifact.json').read_text())
    modules = manifest['modules']
    names = {item['name'] for item in modules}
    if not set(selected) <= names:
        raise RuntimeError('缺少显式选择模块')
    locations = {item['module_name'] for item in manifest['module_locations']
                 if item['kind'] == 'executable_relative'}
    if names - {'__main__'} != locations:
        raise RuntimeError('非入口源码 kind2 闭包不完整')
    for item in modules:
        explicit = item['name'] in selected
        if (item['environment_reason'] == 'explicit-instance') != explicit:
            raise RuntimeError('实例实际选择集合改变')
        if explicit and (item['getter'] is not None or item['setter'] is not None or item['exports']):
            raise RuntimeError('实例模块静默退回 Legacy')
    if manifest['scope']['resources_included'] or manifest['resources']:
        raise RuntimeError('空 files 锚点不应部署资源或源码')
    if any(p.suffix.lower() in {'.py', '.pyc', '.pyo'} for p in dist.rglob('*')):
        raise RuntimeError('不允许部署 Python 源码/字节码')


def main():
    if sys.version_info[:3] != (3, 12, 13):
        raise RuntimeError('必须使用固定CP3.12.13')
    h.OUT = h.REPO / 'target/instance-aot-evidence'
    h.WORK = h.REPO / 'target/instance-aot-work'
    h.OUT.mkdir(parents=True, exist_ok=False)
    h.WORK.mkdir(parents=True, exist_ok=False)
    source = Path(os.environ['WCWIDTH_SOURCE'])
    cli = h.REPO / 'target/debug/ryupy'
    runtime = h.REPO / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll'
    baseline = h.REPO / 'target/wcwidth-aot-evidence'
    old_tools = json.loads((baseline / 'compiler-identity.json').read_text())
    old_inputs = json.loads((baseline / 'input-identity.json').read_text())
    python = Path(sys.executable).resolve(strict=True)
    identities = (old_tools['cli_sha256'], old_tools['runtime_sha256'])
    verify_tools(cli, runtime, python, identities, old_inputs)
    h.save('cross-stage-identity.json', {'legacy_cli_sha256': identities[0],
        'legacy_runtime_sha256': identities[1], 'legacy_python_sha256': old_inputs['python_sha256'],
        'verified_before_guard': True})
    hashes = json.loads((h.HERE / 'source-hashes.json').read_text())
    if hashes != old_inputs['source_hashes']:
        raise RuntimeError('官方源与旧阶段冻结身份不符')
    if hashes != {name: h.sha(source / 'wcwidth' / name) for name in hashes}:
        raise RuntimeError('wcwidth六源变化')
    for name, expected in [('owner_contract.py', '0a5ee0d1035913e80b1f8c856f1cd18a0344d64e274a9aff546ffc3d652266ab'),
                           ('failure_contract.py', 'a11864004b947b60115e185412d59bdb848be5282af235f53d7d1cb8f90f239c')]:
        if h.sha(FIXTURES / name) != expected:
            raise RuntimeError('原合同hash变化')
    if h.sha(h.HERE / '01_original.py') != '2c537d4bd16d4c33316a28ac74155917ee986b91ab90060bee2875b3ac0c7a55':
        raise RuntimeError('原入口变化')
    h.save('inputs.json', {'sources': hashes, 'fixtures': {p.name: h.sha(p)
        for p in sorted(FIXTURES.iterdir()) if p.is_file()}, 'commit': os.environ['GITHUB_SHA'],
        'structure_feature': 'default (ExplicitOnly)',
        'cli_feature': 'ordinary-dynamic-module-pilot (显式AOT合同不变)',
        'note': '包/表/版本均Legacy；不同于旧JIT自动选择unicode_versions；锚点不承诺源码存在'})
    env = dict(os.environ, PYTHONPATH=str(source), PYTHONDONTWRITEBYTECODE='1',
               RYUPY_INSTANCE_AOT_STRUCTURE_OUT=str(h.OUT / 'structure'))
    h.run('rustfmt-check', ['cargo', 'fmt', '--all', '--check'], env=env, timeout=120)
    guards = [
        ('ryupy-codegen-cil', 'instance_pe_', [
            'instance_pe_duplicate_core_roundtrip_and_wrong_targets',
            'instance_pe_envelope_rejects_stale_truncated_and_trailing_inputs']),
        ('ryupy-hir', 'instance_aot', [
            'explicit_aot_accepts_relative_and_name_handlers',
            'explicit_aot_scans_dead_code_defaults_and_decorators',
            'explicit_aot_lambda_positive_domain_has_no_cells_or_free_names',
            'explicit_aot_lambda_negative_domain_is_position_independent']),
        ('ryupy-mir', 'aot_parameter_closed', [
            'aot_parameter_closed_lambdas_keep_owner_without_environment']),
        ('ryupy-driver', 'instance_aot', [
            'explicit_aot_contract_works_without_auto_policy',
            'explicit_aot_requires_entire_source_graph_before_compilation',
            'explicit_aot_rejects_graph_and_ast_boundaries_before_compilation',
            'explicit_aot_rejects_namespace_or_file_package_ancestors',
            'explicit_aot_resource_parse_errors_are_not_masked',
            'explicit_aot_rejects_missing_intermediate_package']),
    ]
    for crate, selection, required in guards:
        tested('guard-' + crate, ['cargo', 'test', '--locked', '-p', crate, selection],
               required, env=env, timeout=900)
    # 真实上游 AST、实际 driver 合同和 PE 发射必须在任何本阶段 publish 前成功。
    tested('real-source-pe-gate', ['cargo', 'test', '--locked', '-p', 'ryupy-driver',
        'explicit_aot_real_source_and_pe_gate', '--', '--ignored', '--nocapture'], ['explicit_aot_real_source_and_pe_gate'], env=env, timeout=900)
    # 独立 codegen 层实际解析 PE；driver 的 CIL 文本只作诊断。
    from instance_pe import envelope, unchanged
    structure = h.OUT / 'structure'
    identity = {}
    for case in ['original', 'combined']:
        identity.update(envelope(structure, case, os.environ['GITHUB_SHA']))
    h.save('actual-pe-inputs.json', identity)
    tested('actual-pe-gate', ['cargo', 'test', '--locked', '-p', 'ryupy-codegen-cil',
        'explicit_instance_actual_pe_gate', '--', '--ignored', '--nocapture'],
        ['explicit_instance_actual_pe_gate'], env=env, timeout=900)
    unchanged(structure, identity)
    image = prepare_image(DockerRunner(), 'docker', h.WORK)
    original_out = h.OUT
    for name in ['original', 'combined']:
        h.OUT = original_out / name
        h.OUT.mkdir()
        h.COMMANDS = []
        root = h.WORK / name
        shutil.copytree(FIXTURES, root)
        entry = root / ('01_original.py' if name == 'original' else 'combined.py')
        if name == 'original':
            shutil.copyfile(h.HERE / '01_original.py', entry)
        expected = h.EXPECTED if name == 'original' else (FIXTURES / 'combined.stdout').read_bytes()
        strict('cpython', [sys.executable, '-B', entry], env=env, expected=expected)
        selected = SELECTED[:1] if name == 'original' else SELECTED
        args = [cli]
        for module in selected:
            args += ['--instance-module', module]
        args += ['build', entry, '--aot', '--resources', root / (name + '-resources.json'), '-I', source]
        verify_tools(cli, runtime, python, identities, old_inputs)
        h.run('publish', args, timeout=1500)
        dist = entry.with_suffix('.dist')
        exe = dist / entry.stem
        if exe.read_bytes()[:4] != b'\x7fELF':
            raise RuntimeError('不是ELF')
        before = preserve_dist(dist, h.OUT)
        h.run('elf-header', ['readelf', '-h', exe])
        h.run('elf-dynamic', ['readelf', '-d', exe])
        h.run('dependencies', ['ldd', exe])
        check_manifest(dist, selected)
        strict('native', [exe], expected=expected)
        relocated = root / 'relocated.dist'
        shutil.move(dist, relocated)
        strict('relocated', [relocated / exe.name], cwd=relocated, expected=expected)
        script = ('for tool in dotnet python python3 python3.12; do '
                  'if command -v "$tool" >/dev/null 2>&1; then exit 97; fi; done; exec "$1"')
        strict('docker', ['docker', 'run', '--pull=never', '--rm', '--network', 'none',
            '--read-only', '--mount', f'type=bind,src={relocated},dst=/app,readonly',
            '--workdir', '/app', '--entrypoint', '/bin/sh', image, '-c', script, 'instance-aot',
            '/app/' + exe.name], expected=expected, timeout=300)
        after, _ = snapshot(relocated)
        verify_tools(cli, runtime, python, identities, old_inputs)
        if before != after or dist.exists():
            raise RuntimeError('搬迁或工具身份变化')
        h.save('result.json', {'passed': True, 'before': before, 'after': after})
    h.OUT = original_out
    h.save('result.json', {'passed': True, 'scope': '显式普通Instance AOT V2有限域，两产物'})


if __name__ == '__main__':
    try:
        main()
    except Exception as error:
        h.OUT.mkdir(parents=True, exist_ok=True)
        h.save('result.json', {'passed': False, 'error': str(error), 'traceback': traceback.format_exc()})
        raise
    finally:
        # 原始SDK流和宿主参数保留；不上传缓存/obj或工具链。
        for path in h.WORK.rglob('*'):
            if path.is_file() and path.name in {'publish.stdout.log', 'publish.stderr.log', 'Host.csproj', 'Program.cs'}:
                target = h.REPO / 'target/instance-aot-evidence/publish' / path.relative_to(h.WORK)
                target.parent.mkdir(parents=True, exist_ok=True)
                shutil.copyfile(path, target)
