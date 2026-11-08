#!/usr/bin/env python3
"""同job显式V1门接线：复用已构建产品工具，仅构建一次只读PE decoder，不改baseline证据。"""
import json
import os
from pathlib import Path
import re
import shutil
import sys
import time
import traceback
import build_provenance
import generator_product_gate as gate
import product_contract as c

ROOT = Path(__file__).resolve().parents[3]
PREP_SECONDS = 420
DECODER_SECONDS = 300


def save(path, value):
    with path.open('x') as stream:
        stream.write(json.dumps(value, ensure_ascii=False, indent=2) + '\n')


def file_identity(path):
    path = Path(path).resolve(strict=True)
    return {'path': str(path), 'sha256': c.sha(path)}


def fixed_dotnet():
    configured = Path(os.environ['DOTNET']).resolve(strict=True)
    actual = Path(shutil.which('dotnet') or '/missing-dotnet').resolve(strict=True)
    c.require(configured == actual, 'DOTNET与实际PATH选择不一致')
    c.require(Path(os.environ['DOTNET_ROOT']).resolve(strict=True) == configured.parent,
              'DOTNET_ROOT与实际固定工具不一致')
    return configured


class Bootstrap:
    def __init__(self, repo=ROOT):
        self.repo = Path(repo).resolve(strict=True)
        self.here = self.repo / 'tests/validation/generator_product'
        self.out = self.repo / 'target/generator-product-bootstrap'
        self.out.mkdir(parents=True, exist_ok=False)  # 失败现场不覆盖，不自动重试。
        self.deadline = time.monotonic() + PREP_SECONDS
        self.p0, _ = gate.load_reused(self.repo)
        self.env = {key: value for key, value in os.environ.items()
                    if key in {'PATH', 'HOME', 'USER', 'SYSTEMROOT', 'DOTNET_ROOT', 'DOTNET_ROOT_X64', 'TMPDIR'}}
        self.dotnet = fixed_dotnet()
        self.env.update(DOTNET=str(self.dotnet), DOTNET_ROOT=str(self.dotnet.parent),
                        DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1',
                        LC_ALL='C.UTF-8', TZ='UTC', NO_COLOR='1', PYTHONOPTIMIZE='0')
        self.git = Path(shutil.which('git') or '/missing-git').resolve(strict=True)
        self.baseline = self.repo / 'target/wcwidth-aot-evidence'
        self.contract = None
        save(self.out / 'budget.json', {'preparation_seconds': PREP_SECONDS,
             'decoder_build_seconds': DECODER_SECONDS, 'product_seconds': gate.LIMITS['total_seconds'],
             'retries': 0, 'outer_job_timeout_also_applies': True})

    def command(self, name, argv, timeout=30):
        remaining = min(timeout, self.deadline - time.monotonic())
        c.require(remaining > 0, 'decoder准备总预算已尽，不补跑')
        prefix = self.out / name
        c.require(not prefix.with_suffix('.command.json').exists(), '禁止重复命令')
        save(prefix.with_suffix('.command.json'), {'argv': list(map(str, argv)), 'cwd': str(self.repo),
             'timeout_seconds': remaining, 'executable_sha256': c.sha(Path(argv[0])),
             'semantic_environment': {k: self.env[k] for k in ('DOTNET', 'DOTNET_ROOT', 'LC_ALL', 'TZ')}})
        # 只写本bootstrap目录；不使用run.py的全局OUT/COMMANDS，不覆盖baseline commands.json。
        self.p0.run_process(list(map(str, argv)), self.repo, self.env, prefix, remaining, self.out)
        return prefix.with_suffix('.stdout').read_bytes(), prefix.with_suffix('.stderr').read_bytes()

    def prepare(self):
        c.require(sys.implementation.name == 'cpython' and sys.version_info[:3] == (3, 12, 13),
                  'bootstrap必须由已固定CP3.12.13解释器启动；不运行任何CP fixture')
        raw, stderr = self.command('git-freeze', [self.git, 'rev-parse', 'HEAD', 'HEAD^{tree}'])
        rows = raw.decode().splitlines()
        c.require(not stderr and len(rows) == 2 and all(re.fullmatch('[0-9a-f]{40}', row) for row in rows),
                  '真实HEAD/tree原流不完整')
        head, tree = rows
        c.require(head == os.environ['GITHUB_SHA'], '实际checkout不是本次GITHUB_SHA')
        status, stderr = self.command('git-clean', [self.git, 'status', '--porcelain=v1', '--untracked-files=all'])
        c.require(status == stderr == b'', '候选不是最终clean提交')
        self.source_before = self.p0.candidate_identity(self.repo)
        c.require(self.source_before['head'] == head and not self.source_before['status'], '候选身份改变')
        cli = file_identity(self.repo / 'target/debug/ryupy')
        runtime = file_identity(self.repo / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll')
        dotnet = file_identity(self.dotnet)
        base = {'commit': head, 'tree': tree, 'cli': cli, 'runtime': runtime}
        self.provenance_before = build_provenance.compiler_evidence(self.baseline, base)
        save(self.out / 'baseline-provenance-before.json', self.provenance_before)
        save(self.out / 'source-before.json', self.source_before)
        save(self.out / 'tools-before.json', {'cli': cli, 'runtime': runtime, 'dotnet': dotnet,
                                              'bootstrap_python': file_identity(sys.executable)})
        # 固定参考只读验证；不重新执行CP，也不以预造expected替换原流。
        c.oracle(self.here / 'goldens', self.repo / 'tests/cases/generator_dynamic/frames/entry.py')
        c.hot_oracle(self.here / 'goldens')
        raw, stderr = self.command('sdk-version', [self.dotnet, '--version'])
        version = raw.decode().strip()
        c.require(not stderr and raw == (version + '\n').encode()
                  and re.fullmatch(r'\d+\.\d+\.\d+(?:-[A-Za-z0-9.-]+)?', version), '实际SDK版本原流非法')
        downloads = json.loads((self.repo / 'tests/aot/wcwidth_ci/downloads.json').read_text())
        selected = [item['version'] for item in downloads if item['kind'] == 'dotnet']
        c.require(selected == [version], '实际SDK不等于本轮已固定工具计划')
        build = self.out / 'decoder-build'
        self.command('decoder-build', [self.dotnet, 'build',
            self.repo / 'tests/validation/foreach_pe_audit/PeAudit.csproj', '-c', 'Release',
            '--artifacts-path', build, '-nologo', '-v', 'q'], DECODER_SECONDS)
        choices = list((build / 'bin').rglob('PeAudit.dll'))
        c.require(len(choices) == 1, '只读decoder产物缺失/不唯一')
        decoder_dir = self.out / 'decoder'
        shutil.copytree(choices[0].parent, decoder_dir)
        decoder = file_identity(decoder_dir / 'PeAudit.dll')
        save(self.out / 'decoder-identity.json', {'files': {str(p.relative_to(decoder_dir)): c.sha(p)
             for p in sorted(decoder_dir.rglob('*')) if p.is_file()}, 'kind': 'read-only-PE-decoder-not-product'})
        source_after = self.p0.candidate_identity(self.repo)
        save(self.out / 'source-after-preparation.json', source_after)
        c.require(source_after == self.source_before, 'decoder准备改变候选源码')
        for value in (cli, runtime, dotnet):
            c.require(file_identity(value['path']) == value, 'decoder准备改变已冻产品工具')
        provenance_after = build_provenance.compiler_evidence(self.baseline, base)
        save(self.out / 'baseline-provenance-after-preparation.json', provenance_after)
        save(self.out / 'tools-after-preparation.json', {name: file_identity(value['path'])
             for name, value in (('cli', cli), ('runtime', runtime), ('dotnet', dotnet), ('decoder', decoder))})
        c.require(provenance_after == self.provenance_before, 'baseline构建原件被改变')
        self.contract = {'frozen': True, 'repo': str(self.repo), 'commit': head, 'tree': tree,
            'flag': gate.FLAG, 'abi': 1, 'pic_modes': ['1', '0'], 'cli': cli, 'runtime': runtime,
            'dotnet': dotnet, 'decoder': decoder, 'rid': 'linux-x64', 'sdk_version': version,
            'compiler_evidence': str(self.baseline), 'goldens': str(self.here / 'goldens'), 'shared_artifacts': [],
            'output': str(self.repo / 'target/generator-product-evidence'),
            'work': str(self.repo / 'target/generator-product-work'), 'bootstrap_evidence': str(self.out)}
        save(self.out / 'contract.json', self.contract)
        save(self.out / 'preparation-result.json', {'passed': True, 'product_tools_rebuilt': False,
             'decoder_builds': 1, 'cp_fixture_executions': 0, 'contract_sha256': c.sha(self.out / 'contract.json')})
        return self.contract

    def execute(self):
        try:
            contract = self.prepare()
            save(self.out / 'product-started.json', {'attempt': 1, 'contract_sha256': c.sha(self.out / 'contract.json')})
            gate.Runner(contract).execute()  # 唯一产品窗口；旧Legacy红灯不改写为通过。
            c.require(self.p0.candidate_identity(self.repo) == self.source_before, '产品后源码身份改变')
            c.require(build_provenance.compiler_evidence(self.baseline, contract) == self.provenance_before,
                      '产品后baseline证据被改变')
            save(self.out / 'result.json', {'passed': True, 'scope': 'explicit V1 only; legacy/fullcheck/P0 statuses remain separate',
                 'product_result_sha256': c.sha(Path(contract['output']) / 'result.json')})
        except Exception as error:
            save(self.out / 'result.json', {'passed': False, 'error': str(error), 'traceback': traceback.format_exc(),
                 'product_started': (self.out / 'product-started.json').is_file(), 'retries': 0})
            raise


def main():
    bootstrap = None
    existed = (ROOT / 'target/generator-product-bootstrap').exists()
    try:
        bootstrap = Bootstrap()
        bootstrap.execute()
    except Exception as error:
        # 初始化失败也留独立原流/错误；若已有旧目录则不覆盖旧attempt。
        out = ROOT / 'target/generator-product-bootstrap'
        if bootstrap is None and not existed and out.is_dir() and not (out / 'result.json').exists():
            save(out / 'initialization-failure.json', {'error': str(error), 'traceback': traceback.format_exc()})
        raise


if __name__ == '__main__':
    main()
