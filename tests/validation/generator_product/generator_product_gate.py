#!/usr/bin/env python3
"""同候选普通CLI无flag与显式V1整流门：复用冻结CP、PE decoder和Native归档。"""
import argparse
import importlib
import json
import os
from pathlib import Path
import shutil
import sys
import time
import traceback
import product_contract as c
import build_provenance
from selection_contract import stages

# 这些只是新门预算，不改变full_check、fixed20及任何已有安全负例。
LIMITS = {'total_seconds': 3600, 'compile_seconds': 300, 'publish_seconds': 1500,
          'run_seconds': 30, 'decode_seconds': 60, 'stream_bytes': 8 * 1024**2}
FLAG = '--trace-product-v1'


def save(path, value):
    path.write_text(json.dumps(value, ensure_ascii=False, indent=2) + '\n')


def load_reused(repo):
    sys.path.insert(0, str(repo / 'tests'))
    sys.path.insert(0, str(repo / 'tests/aot'))
    return importlib.import_module('run_projects_gate'), importlib.import_module('native_evidence')


def build_command(cli, source, output, enabled=True, native=False):
    return [str(cli), *([FLAG] if enabled else []), 'build', str(source),
            '--diagnostic-stages', '--out-dir', str(output), *(['--aot'] if native else [])]


def require_plan(path):
    plan = json.loads(path.read_text())
    modules = plan['modules']
    c.require(len(modules) == 1 and modules[0]['module'] == '__main__'
              and modules[0]['execution'] == 'LegacySingleton' and modules[0]['compiled'] is True,
              '必须为真实单源Legacy完整编译')
    return plan


def stage_plan():
    selected = [(name, mode, False) for name in (*c.CASES[:2], c.HOT) for mode in ('jit', 'native')]
    outside = [(name, 'jit', False) for name in sorted(c.REJECTED)]
    required = [(name, mode, True) for name in (*c.CASES, c.HOT) for mode in ('jit', 'native')]
    return selected + outside + required


class Runner:
    def __init__(self, contract):
        self.config = contract
        c.require(contract.get('frozen') is True, '接口/工具/候选尚未冻结，禁止启动产品')
        c.require(contract['flag'] == FLAG and contract['abi'] == 1, '接口合同改变，须writer确认')
        c.require(contract['pic_modes'] == ['1', '0'], 'PIC覆盖必须显式1/0且各一次')
        self.repo = Path(contract['repo']).resolve(strict=True)
        self.p0, self.native = load_reused(self.repo)
        self.manifest = importlib.import_module('manifest_verify')
        c.require(contract['rid'] == 'linux-x64' and isinstance(contract['sdk_version'], str)
                  and contract['sdk_version'], '必须冻结本次Linux RID及实际SDK版本')
        self.tools = {name: Path(contract[name]['path']).resolve(strict=True)
                      for name in ('cli', 'runtime', 'dotnet', 'decoder')}
        c.require(self.tools['runtime'].name == 'RyuPy.Runtime.dll', 'runtime参数不是实际DLL')
        for name, path in self.tools.items():
            c.require(c.sha(path) == contract[name]['sha256'], name + '工具hash不符')
        self.provenance = build_provenance.compiler_evidence(Path(contract['compiler_evidence']), contract)
        self.identity = self.p0.candidate_identity(self.repo)
        c.require(self.identity['head'] == contract['commit'] and not self.identity['status'],
                  '只接受最终clean提交，不把canonical WIP当受测HEAD')
        import subprocess
        tree = subprocess.check_output(['git', '-C', str(self.repo), 'rev-parse', 'HEAD^{tree}']).decode().strip()
        c.require(tree == contract['tree'], '候选tree不符')
        self.oracle = c.oracle(Path(contract['goldens']), self.repo / 'tests/cases/generator_dynamic/frames/entry.py')
        self.oracle[c.HOT] = c.hot_oracle(Path(contract['goldens']))
        self.out, self.work = Path(contract['output']), Path(contract['work'])
        c.require(not self.out.exists() and not self.work.exists(), '不覆盖旧结果/工作目录')
        self.out.mkdir(parents=True)
        self.work.mkdir(parents=True)
        self.env = {key: value for key, value in os.environ.items()
                    if key in {'PATH', 'HOME', 'USER', 'SYSTEMROOT', 'DOTNET_ROOT', 'DOTNET_ROOT_X64', 'TMPDIR'}}
        self.env.update(DOTNET=str(self.tools['dotnet']), RYUPY_RUNTIME_DIR=str(self.tools['runtime'].parent),
                        LC_ALL='C.UTF-8', TZ='UTC', NO_COLOR='1', PYTHONOPTIMIZE='0')
        self.deadline = time.monotonic() + LIMITS['total_seconds']
        shutil.copytree(Path(contract['goldens']), self.out / 'cp-reference')
        save(self.out / 'freeze.json', {'contract': contract, 'limits': LIMITS, 'candidate': self.identity,
                                       'compiler_provenance': self.provenance,
                                       'golden_manifest_sha256': c.MANIFEST_SHA, 'cp_execution': 'not-requested'})

    def command(self, stage, label, argv, expected, timeout, pic=None):
        env = dict(self.env)
        if pic is not None:
            env['RYUPY_PIC'] = pic
        remaining = min(timeout, self.deadline - time.monotonic())
        c.require(remaining > 0, '总预算已尽，不补跑')
        prefix = stage / label
        c.require(not prefix.with_suffix('.json').exists(), '同标签禁止重复执行')
        save(prefix.with_suffix('.command.json'), {'argv': list(map(str, argv)), 'cwd': str(self.repo),
             'timeout_seconds': remaining, 'semantic_environment': {k: env.get(k) for k in
             ('DOTNET', 'RYUPY_RUNTIME_DIR', 'RYUPY_PIC', 'LC_ALL', 'TZ', 'PYTHONOPTIMIZE')},
             'executable_sha256': c.sha(Path(argv[0]))})
        try:
            self.p0.run_process(list(map(str, argv)), self.repo, env, prefix, remaining, stage)
        except self.p0.GateError:
            # 只将完整流、准确exit=1当预期异常；timeout/信号/启动失败仍硬失败。
            if not prefix.with_suffix('.json').is_file():
                raise
        record = json.loads(prefix.with_suffix('.json').read_text())
        c.require(record.get('failure') is None and record.get('raw_streams_complete') is True,
                  label + '超时/超量/不完整')
        c.require(type(record.get('exit')) is int and record['exit'] == expected,
                  label + '退出码不符：' + str(record.get('exit')))
        return record, prefix.with_suffix('.stdout').read_bytes(), prefix.with_suffix('.stderr').read_bytes()

    def decode(self, stage, pe, enabled, hot=False):
        _, stdout, stderr = self.command(stage, 'pe-decode',
            [self.tools['dotnet'], self.tools['decoder'], pe], 0, LIMITS['decode_seconds'])
        c.require(stderr == b'', '只读PE decoder有stderr')
        decoded = json.loads(stdout)
        result = c.audit_pe(decoded, pe, enabled)
        if hot:
            result['hot_site'] = c.hot_site(decoded, self.repo)
        save(stage / 'pe-root-abi.json', result)

    def publish_inputs(self, root, stage, entry, dist, source_sha):
        versions = list(root.rglob('sdk.version'))
        c.require(len(versions) == 1
                  and self.manifest.read_sdk_version(versions[0]) == self.config['sdk_version'],
                  '实际publish SDK记录缺失/不唯一/不符')
        manifest = self.manifest.verify_manifest(dist, dist / 'entry', self.config['rid'], self.config['sdk_version'])
        inputs = {v['role']: v for v in manifest['build_inputs']}
        c.require(set(inputs) == {'compiled_managed_input', 'runtime_managed_input'}, 'Native输入角色缺失')
        c.require(inputs['runtime_managed_input']['sha256'] == c.sha(self.tools['runtime']), 'AOT runtime身份不符')
        candidates = [p for p in root.rglob(entry.stem + '.dll')
                      if c.sha(p) == inputs['compiled_managed_input']['sha256']]
        c.require(candidates, '找不到本次AOT真实托管输入')
        # manifest的hash已核，所有同hash副本等价；不依赖attempt随机目录名称。
        pe = candidates[0]
        c.require(c.sha(pe.parent / 'RyuPy.Runtime.dll') == c.sha(self.tools['runtime']), 'AOT实际复制runtime不符')
        require_plan(pe.parent / 'module-environment.json')
        sources = manifest['sources']
        c.require(len(sources) == 1 and sources[0]['compiled_text_sha256'] == source_sha, 'AOT不是原单源')
        c.require(len(manifest['modules']) == 1 and manifest['modules'][0]['name'] == '__main__'
                  and manifest['modules'][0]['environment_reason'] == 'aot-not-migrated', 'AOT意外扩展模式')
        saved = stage / 'managed-input'
        saved.mkdir()
        for path in pe.parent.iterdir():
            if path.is_file() and (path.suffix in ('.dll', '.json')):
                shutil.copy2(path, saved / path.name)
        return saved / pe.name

    def preserve_existing(self, root, evidence, primary_error=None, native=False):
        """已有原件先有界保存；归档另败单独留证，不能替换compile/run的首因。"""
        if not root.exists():
            return None
        original = root
        try:
            if native:
                dist = root / 'entry.dist'
                if dist.exists():
                    root = dist
                else:
                    # 未形成dist时仅保本次顶层托管输入，不递归打包AOT obj/nuget缓存。
                    files = [p for p in root.iterdir() if p.suffix.lower() in {'.dll', '.exe', '.json'} and p.is_file()]
                    if not files:
                        return None
                    c.require(not any(p.is_symlink() for p in files), '托管输入不接受链接')
                    c.require(len(files) <= self.native.MAX_FILES and sum(p.stat().st_size for p in files) <= self.native.MAX_BYTES,
                              '托管输入超过既有归档上限')
                    root = evidence / 'partial-managed-input'; root.mkdir()
                    for path in files:
                        shutil.copy2(path, root / path.name)
            before = self.native.preserve_dist(root, evidence)
            save(evidence / 'preservation-scope.json', {'original_root': str(original), 'saved_root': str(root), 'native_requested': native})
            return before
        except Exception as error:
            try:
                save(evidence / 'preservation-failure.json', {'error': str(error),
                    'primary_error': str(primary_error) if primary_error is not None else None, 'root': str(original)})
            except Exception:
                pass  # 记录自身失败也不能替换原始异常；成功路径仍以原归档异常硬失败。
            if primary_error is None:
                raise
            return None

    def stage(self, name, mode, enabled=True):
        label = name + '-' + mode + ('-default' if not enabled else '')
        evidence = self.out / label
        evidence.mkdir()
        work = self.work / label
        work.mkdir()
        entry = work / 'entry.py'
        shutil.copy2(self.oracle[name]['source'], entry)
        c.require(c.sha(entry) == c.sha(self.oracle[name]['source']), '源副本改变')
        output = work / 'output'
        native = mode == 'native'
        rejected = enabled and name in c.REJECTED
        protocol = name not in c.REJECTED
        source_sha = self.oracle[name]['source_sha256']
        try:
            record, stdout, stderr = self.command(evidence, 'compile',
                build_command(self.tools['cli'], entry, output, enabled, native),
                1 if rejected else 0, LIMITS['publish_seconds'] if native else LIMITS['compile_seconds'])
        finally:
            before = self.preserve_existing(output, evidence, sys.exception(), native)

        c.require(c.sha(entry) == source_sha and c.sha(self.oracle[name]['source']) == source_sha,
                  '编译阶段改变原源')
        if rejected:
            stages(stderr, False, rejected=True)
            c.rejection(record, stdout, stderr, output)
            save(evidence / 'result.json', {'passed': True, 'kind': 'compile-rejection',
                'source_execution': 'not-started', 'basis': 'empty compiler stdout; no PE/ELF; build only'})
            return
        if native:
            dist = output / 'entry.dist'
            exe = dist / 'entry'
            c.require(exe.read_bytes()[:4] == b'\x7fELF', '不是ELF')
            c.require(not any(p.suffix in {'.py', '.pyc', '.pyo'} for p in dist.rglob('*')), '交付混入源码')
            pe = self.publish_inputs(output, evidence, entry, dist, source_sha)
            argv = [exe]
        else:
            require_plan(output / 'module-environment.json')
            c.require(c.sha(output / 'RyuPy.Runtime.dll') == c.sha(self.tools['runtime']), 'JIT实际runtime不符')
            pe, argv = output / 'entry.dll', [self.tools['dotnet'], output / 'entry.dll']
        selection = stages(stderr, protocol)
        save(evidence / 'selection-stages.json', selection)
        self.decode(evidence, pe, protocol, name == c.HOT)
        if protocol:
            for pic in self.config['pic_modes']:
                result, out, err = self.command(evidence, 'run-pic-' + pic, argv, self.oracle[name]['exit'], LIMITS['run_seconds'], pic)
                c.require(c.sha(entry) == source_sha and c.sha(self.oracle[name]['source']) == source_sha,
                          '产品执行阶段改变原源')
                normalized = c.compare(self.oracle[name], result, out, err, entry)
                (evidence / ('run-pic-' + pic + '.normalized.stderr')).write_bytes(normalized)
        if not enabled and not native and name == 'b_target_failure':
            # 真实Run子命令另走正常构建/执行出口；不传trace flag，不剥诊断后冒充CP流。
            run_output = work / '__pycache__' / 'entry'
            run_evidence = evidence / 'cli-run-pe'; run_evidence.mkdir()
            try:
                result, out, err = self.command(evidence, 'cli-run-no-flag',
                    [self.tools['cli'], 'run', entry], self.oracle[name]['exit'],
                    LIMITS['compile_seconds'], '1')
            finally:
                self.preserve_existing(run_output, run_evidence, sys.exception())

            normalized = c.compare(self.oracle[name], result, out, err, entry)
            (evidence / 'cli-run-no-flag.normalized.stderr').write_bytes(normalized)
            c.require(c.sha(run_output / 'RyuPy.Runtime.dll') == c.sha(self.tools['runtime']), 'Run实际runtime不符')
            self.decode(run_evidence, run_output / 'entry.dll', True)
        c.require(before == self.native.snapshot(dist if native else output)[0], '执行/检查期间产物改变')
        save(evidence / 'result.json', {'passed': True, 'enabled': enabled, 'mode': mode,
             'selection_request': 'require-v1' if enabled else 'ordinary-cli-no-flag',
             'kind': 'product-raw-streams' if protocol else 'domain-outside-legacy-pe-only',
             'protocol': protocol, 'pic_modes': self.config['pic_modes'] if protocol else [],
             'getter_coverage': 'same-site repeated execution; separate C# cold/warm mechanism; no product hit counts' if name == c.HOT else 'original case'})

    def execute(self):
        planned = stage_plan()
        completed = []
        try:
            for name, mode, enabled in planned:
                self.stage(name, mode, enabled)
                completed.append([name, mode, enabled])
            c.require(self.identity == self.p0.candidate_identity(self.repo), '受测候选在执行中改变')
            for name, path in self.tools.items():
                c.require(c.sha(path) == self.config[name]['sha256'], '实际工具在执行中改变')
            c.oracle(Path(self.config['goldens']), self.repo / 'tests/cases/generator_dynamic/frames/entry.py')
            c.hot_oracle(Path(self.config['goldens']))
            c.require(self.provenance == build_provenance.compiler_evidence(Path(self.config['compiler_evidence']), self.config),
                      '构建provenance运行中改变')
            save(self.out / 'result.json', {'passed': True, 'completed': completed,
                 'scope': 'original A/B/HOT ordinary CLI no-flag 12 runs plus explicit 12 runs and original C/D four strict rejects; C/D no-flag PE Legacy only; one actual no-flag ryupy run; no performance claim',
                 'fullcheck_and_fixed20': 'separate same-HEAD existing jobs remain required'})
        except Exception as error:
            save(self.out / 'result.json', {'passed': False, 'completed': completed,
                 'not_completed': [list(v) for v in planned if list(v) not in completed],
                 'error': str(error), 'traceback': traceback.format_exc()})
            raise
        finally:
            # compile首败也保本次JIT原件；Native仅取dist/顶层托管输入，不归档整个obj。
            primary_error = sys.exception()
            for output in self.work.glob('*/output'):
                label = output.relative_to(self.work).parts[0]
                evidence = self.out / label
                if not (evidence / 'native-dist.tar.gz').exists() and not (evidence / 'preservation-failure.json').exists():
                    self.preserve_existing(output, evidence, primary_error, '-native' in label)
            # 失败publish也保留真实SDK输出及普通driver Host，不能替换成测试Host。
            for path in self.work.rglob('*'):
                if path.is_file() and path.name in {'publish.stdout.log', 'publish.stderr.log', 'Host.csproj', 'Program.cs', 'build-error.log', 'sdk.version'}:
                    target = self.out / 'publish' / path.relative_to(self.work)
                    try:
                        target.parent.mkdir(parents=True, exist_ok=True)
                        shutil.copy2(path, target)
                    except Exception:
                        if primary_error is None:
                            raise


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--contract', type=Path, required=True)
    parser.add_argument('--execute', action='store_true', help='显式运行；仅用于最终工具与输入已冻结的CI窗口')
    args = parser.parse_args()
    config = json.loads(args.contract.read_text())
    if not args.execute:
        print(json.dumps({'execution': 'not-started', 'contract': config, 'limits': LIMITS}, ensure_ascii=False, indent=2))
        return
    Runner(config).execute()


if __name__ == '__main__':
    main()
