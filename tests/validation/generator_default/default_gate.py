#!/usr/bin/env python3
"""普通CLI无flag域外保功能门；同候选裸Session独立API是Legacy对照，不是历史旧CLI。"""
import json
from pathlib import Path
import shutil
import sys
import time
import traceback

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE.parent / 'generator_product'))
import generator_product_gate as product
import product_contract as c
import build_provenance
from selection_contract import stages

MANIFEST_SHA = '8451d5ce17f11ca6163cb134188af4ca586191e098226c980affb63c0cf4e965'
SECONDS = 1200


def references(repo):
    root = repo / 'tests/validation/generator_default'
    directory = root / 'reference'
    c.require(c.sha(directory / 'manifest.json') == MANIFEST_SHA, '新CP清单身份改变')
    manifest = json.loads((directory / 'manifest.json').read_text())
    c.require(manifest['python']['sha256'] == '021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7'
              and manifest['python']['version'] == '3.12.13' and manifest['flags'] == ['-B', '-S']
              and manifest['repetitions_per_source'] == 1 and len(manifest['cases']) == 12,
              'CP工具或单次计划不符')
    c.require({p.name for p in directory.iterdir()} == set(manifest['files']) | {'manifest.json'}, 'CP原件集合改变')
    for name, digest in manifest['files'].items():
        c.require(c.sha(directory / name) == digest, 'CP原流改变：' + name)
    rows = {}
    for item in manifest['cases']:
        name = item['id']; source = root / 'sources' / (name + '.py')
        record = json.loads((directory / (name + '.result.json')).read_text())
        c.require(c.sha(source) == item['sha256'] == record['source_sha256_after'], '冻结源改变')
        c.require(record['raw_streams_complete'] and not record['timeout']
                  and record['python_sha256_after'] == manifest['python']['sha256'], 'CP参考未完整完成')
        stderr = (directory / (name + '.stderr')).read_bytes()
        normalized = c.normalize(stderr, source.read_bytes(), {item['source']})
        c.require(normalized == (directory / (name + '.normalized.stderr')).read_bytes(), 'CP派生流不符')
        c.require((directory / (name + '.exit')).read_text() == str(record['exit']) + '\n', 'CP exit原件不符')
        rows[name] = {'source': source, 'source_bytes': source.read_bytes(), 'source_sha256': c.sha(source),
                      'category': item['category'], 'exit': record['exit'], 'stdout': (directory / (name + '.stdout')).read_bytes(),
                      'normalized': normalized}
    return rows



class Runner(product.Runner):
    def __init__(self, contract):
        super().__init__(contract)
        self.deadline = time.monotonic() + SECONDS
        self.reference = references(self.repo)
        shutil.copytree(HERE / 'reference', self.out / 'new-cp-reference')

    def prepare_legacy(self):
        stage = self.out / 'legacy-api-build'; stage.mkdir()
        cargo = Path(shutil.which('cargo') or '/missing-cargo').resolve(strict=True)
        self.command(stage, 'build', [cargo, 'build', '--locked', '-p', 'ryupy-driver',
            '--features', 'ordinary-dynamic-module-pilot', '--example', 'trace_legacy_control'], 0, 300)
        binary = self.repo / 'target/debug/examples/trace_legacy_control'
        self.legacy = binary.resolve(strict=True)
        self.legacy_sha = c.sha(self.legacy)
        shutil.copy2(self.legacy, stage / self.legacy.name)
        product.save(stage / 'identity.json', {'path': str(self.legacy), 'sha256': self.legacy_sha,
            'source': 'crates/ryupy-driver/examples/trace_legacy_control.rs',
            'source_sha256': c.sha(self.repo / 'crates/ryupy-driver/examples/trace_legacy_control.rs'),
            'boundary': 'same candidate explicit Legacy API; not historical CLI; not production trace switch'})

    def control(self, name, reference):
        evidence = self.out / name; evidence.mkdir()
        work = self.work / name; work.mkdir()
        entry = work / ('renamed_control.py' if name == 'renamed_alias' else 'entry.py'); shutil.copyfile(reference['source'], entry)
        selected = reference['category'] == 'positive-v1'
        flows = {}
        for mode in ('legacy-api', 'ordinary-cli'):
            stage = evidence / mode; stage.mkdir()
            output = work / mode
            argv = [self.legacy, entry, output] if mode == 'legacy-api' else product.build_command(self.tools['cli'], entry, output, False)
            try:
                record, stdout, stderr = self.command(stage, 'compile', argv, 0, 90)
            finally:
                before = self.preserve_existing(output, stage, sys.exception())
            c.require(c.sha(entry) == reference['source_sha256'], '编译改变源副本')
            c.require(stdout == b'', '编译不应执行用户输出')
            product.require_plan(output / 'module-environment.json')
            c.require(c.sha(output / 'RyuPy.Runtime.dll') == c.sha(self.tools['runtime']), '实际runtime改变')
            self.decode(stage, output / (entry.stem + '.dll'), selected and mode == 'ordinary-cli')
            observed = stages(stderr, selected, reference['category'] == 'no-prover-legacy-success') if mode == 'ordinary-cli' else None
            if mode == 'legacy-api':
                c.require(b'stage=trace-v1-' not in stderr, '裸Session对照意外触发V1分析')
            product.save(stage / 'selection.json', {'request': mode, 'v1': selected and mode == 'ordinary-cli',
                'stages': observed, 'memory': 'not-measured', 'benchmark': False})
            for pic in ('1', '0'):
                record, out, err = self.command(stage, 'run-' + pic, [self.tools['dotnet'], output / (entry.stem + '.dll')],
                    reference['exit'], 30, pic)
                normalized = c.normalize(err, reference['source_bytes'], {str(entry), entry.name})
                (stage / ('run-' + pic + '.normalized.stderr')).write_bytes(normalized)
                flows[(mode, pic)] = (record['exit'], out, normalized)
                if reference['category'].endswith('success') or (selected and mode == 'ordinary-cli'):
                    c.compare(reference, record, out, err, entry)
            c.require(before == self.native.snapshot(output)[0], '运行后产物改变')
        if not selected:
            for pic in ('1', '0'):
                c.require(flows[('ordinary-cli', pic)] == flows[('legacy-api', pic)], '合法域外改变了同候选Legacy实际行为')
        cp_pass = reference['category'] != 'legacy-known-trace-debt'
        product.save(evidence / 'result.json', {'passed': True, 'category': reference['category'],
            'python_semantics_pass': cp_pass,
            'cp_required_modes': [] if not cp_pass else ['ordinary-cli'] if selected else ['legacy-api', 'ordinary-cli'],
            'legacy_comparison': 'same candidate explicit API, not historical CLI',
            'cp_equal': {mode + '-' + pic: flow == (reference['exit'], reference['stdout'], reference['normalized'])
                         for (mode, pic), flow in flows.items()},
            'scope': 'unused identity preserves old trace debt only' if not cp_pass else 'full CP stdout/stderr/exit'})

    def execute(self):
        completed = []
        try:
            self.prepare_legacy()
            for name, reference in self.reference.items():
                self.control(name, reference); completed.append(name)
            c.require(self.p0.candidate_identity(self.repo) == self.identity, '候选源码改变')
            c.require(c.sha(self.legacy) == self.legacy_sha, 'Legacy API工具改变')
            for name, path in self.tools.items():
                c.require(c.sha(path) == self.config[name]['sha256'], '原产品工具改变')
            c.require(self.provenance == build_provenance.compiler_evidence(Path(self.config['compiler_evidence']), self.config), '构建来源改变')
            references(self.repo)
            product.save(self.out / 'result.json', {'passed': True, 'completed': completed,
                'oracle_executions': 0, 'memory_and_performance': 'not-measured',
                'scope': 'real ordinary CLI no-flag vs same-candidate Legacy API; no project/e2e Auto claim'})
        except Exception as error:
            product.save(self.out / 'result.json', {'passed': False, 'completed': completed,
                'error': str(error), 'traceback': traceback.format_exc(), 'retries': 0})
            raise


def main():
    path = ROOT / 'target/generator-product-bootstrap/contract.json'
    contract = json.loads(path.read_text())
    c.require(json.loads((path.parent / 'preparation-result.json').read_text())['passed'], '原产品工具准备未通过')
    contract.update(output=str(ROOT / 'target/generator-default-evidence'), work=str(ROOT / 'target/generator-default-work'))
    Runner(contract).execute()


if __name__ == '__main__':
    main()
