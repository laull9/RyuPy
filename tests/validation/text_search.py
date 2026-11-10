#!/usr/bin/env python3
"""五搜索同候选有界验收；复用原保护与采集器，不改变旧模式或默认项目口径。"""
import collections
import json
import os
from pathlib import Path
import shutil
import sys
import traceback
import xml.etree.ElementTree as ET
import typing_noreturn as old
import text_search_native as product

shared = old.shared
ROOT = old.ROOT
require, sha, save = old.require, old.sha, old.save
DATA = json.loads(Path(__file__).with_suffix('.json').read_text())
CI_PATHS = {'.github/workflows/wcwidth-legacy-aot.yml', '.github/workflows/text-search.yml',
            'tests/validation/text_search.py', 'tests/validation/text_search_native.py',
            'tests/validation/text_search.json', 'tests/validation/test_text_search.py',
            'tests/validation/generator_product/test_diagnostic_once.py', 'tests/validation/markdown_preflight.py'}


def check_context(env, event):
    require(env.get('RUNNER_ENVIRONMENT') == 'github-hosted' and
            env.get('RYUPY_VALIDATION_MODE') == 'text-search', '只允许hosted新模式')
    require(event.get('head_commit', {}).get('message') == DATA['title'], '新精确标题不符')
    # 仅复用旧公共环境防注入检查；不修改旧事件/常量或执行旧模式。
    adjusted = dict(event, head_commit=dict(event['head_commit'], message=shared.TITLE))
    shared.check_context(env, adjusted)


def verify_trx(path):
    rows = ET.parse(path).findall('.//{http://microsoft.com/schemas/VisualStudio/TeamTest/2010}UnitTestResult')
    require(rows and all(x.get('outcome') == 'Passed' for x in rows), '搜索C#失败/跳过/零测试')
    actual = collections.Counter(x.attrib['testName'].removeprefix('RyuPy.Runtime.Tests.') for x in rows)
    require(actual == collections.Counter(DATA['facts']), '搜索14Fact缺失/重复/越界')
    return {'total': len(rows), 'all_passed': True}


class Runner(old.Runner):
    def __init__(self):
        self.data = DATA
        self.out = ROOT / 'target/text-search-evidence'
        self.out.mkdir(parents=True, exist_ok=False)
        self.labels, self.frozen = set(), {}
        names = ['identity', 'csharp-build', 'csharp-typing', 'csharp-expandtabs', 'csharp-search',
                 'tools', 'supported-cp', 'supported-jit', 'supported-native',
                 'full-source-deletion-boundary', 'sourceprep']
        names += ['expandtabs-' + n + '-' + mode for n in old.EXPAND_CONTRACTS for mode in ('cp', 'jit', 'native')]
        names += [n + '-' + mode for n in [*DATA['fixtures'], 'markdown-plain', 'markdown-full']
                  for mode in (('cp', 'jit') if n == 'core' else ('cp', 'jit', 'native'))]
        self.result = {'passed': False, 'supported_contract_passed': False, 'full_contract_supported': False,
                       'identity_preserved': False, 'default_project_delta': 0, 'retries': 0,
                       'scope': '五搜索及真实ParserBase；旧保护和显式Markdown；非性能/默认晋升',
                       'old_gates': '其它jobs仅依赖selector，默认20与default/pilot独立运行',
                       'stages': {n: {'passed': False, 'status': 'blocked'} for n in names}}
        self.before = None

    def preserve(self, root, label):
        result = super().preserve(root, label)
        archive = self.out / 'native-dist.tar.gz'
        if label == 'native-original' and archive.is_file():
            self.frozen[str(archive)] = sha(archive)
        return result

    def identities(self):
        check_context(os.environ, json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text()))
        self.before = self.identity('before')
        require(self.before['head'] == os.environ['GITHUB_SHA'], '实际HEAD不符')
        self.checked('parent', ['git', 'rev-parse', 'HEAD^', 'HEAD^^{tree}'], 20)
        require((self.out / 'parent.stdout').read_text().splitlines() == [DATA['parent'], DATA['parent_tree']],
                '直接远端父及tree不符')
        self.checked('legacy-parent', ['git', 'rev-parse', 'HEAD^^', 'HEAD^^^{tree}'], 20)
        require((self.out / 'legacy-parent.stdout').read_text().splitlines() ==
                [DATA['legacy_parent'], DATA['legacy_tree']], '固定历史祖父及tree不符')
        self.checked('base-files', ['git', 'ls-tree', '-r', '-z', DATA['parent_tree']], 20)
        base = shared.tree_records((self.out / 'base-files.stdout').read_bytes())
        changed = {p for p in base.keys() | self.before['files'].keys() if base.get(p) != self.before['files'].get(p)}
        require(changed <= set(DATA['frozen']) | CI_PATHS, '出现候选外差异')
        require(all(sha(ROOT / p) == h for p, h in DATA['frozen'].items()), '冻结源/旧保护改变')
        self.frozen.update({str(ROOT / p): h for p, h in DATA['frozen'].items()})
        save(self.out / 'source-identity.json', dict(self.before, direct_parent=DATA['parent'],
             parent_tree=DATA['parent_tree'], changed=sorted(changed), frozen=DATA['frozen']))
        self.python, self.dotnet = Path(os.environ['PYTHON']).resolve(), Path(os.environ['DOTNET']).resolve()
        require(sys.version_info[:3] == (3, 12, 13) and sha(self.python) == shared.PYTHON_SHA, 'CP身份不符')
        versions = {'dotnet': '10.0.403', 'rustc': 'rustc 1.99.1 (58cf8f9c3 2026-10-13)',
                    'cargo': 'cargo 1.99.1 (5f94df478 2026-08-27)'}
        tools = {'python': self.python, 'dotnet': self.dotnet}
        for name, expected in versions.items():
            path = self.dotnet if name == 'dotnet' else Path(shutil.which(name)).resolve()
            self.checked(name + '-version', [path, '--version'], 30)
            require((self.out / (name + '-version.stdout')).read_text().strip() == expected, '工具版本不符')
            tools[name] = path
        self.frozen.update({str(p): sha(p) for p in tools.values()})
        save(self.out / 'tool-identity.json', {'versions': versions, 'feature': shared.FEATURE,
             'tools': {name: {'path': str(p), 'sha256': sha(p)} for name, p in tools.items()}})
        saved = self.out / 'sources'; saved.mkdir()
        for name in [old.CONTRACT, old.SUPPORTED, *(row['source'] for row in DATA['fixtures'].values()),
                     *('tests/fixtures/text_expandtabs/' + n + '.py' for n in old.EXPAND_CONTRACTS)]:
            dest = saved / name; dest.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(ROOT / name, dest)
        self.preserve(saved, 'contract-sources')
        save(self.out / 'references.json', {'core-native': {'status': 'historical-reference-not-rerun', 'run': 42769351447,
             'exit': 1, 'relocated': False, 'supported': False,
             'reason': '原字面量输入在旧前端合流为U+FFFD；保留原count期望1'},
             'cp245': {'status': 'not-run', 'cases': 245,
             'script_sha256': '899fc6ea18adcf23c44e7de6d1dc4e4fea7ebe838bd2dc1bf00ebb33e72d20de'},
             'full-noreturn': {'status': 'historical-reference-not-rerun', 'run': 42754725069,
             'cp_stdout_bytes': 1482, 'cp_stdout_sha256': old.ORACLE_SHA,
             'native_previous_exit': 1, 'native_previous_relocated': False}})

    def managed_search(self):
        target = self.out / 'trx-search'
        self.checked('csharp-search', [self.dotnet, 'test', ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj',
                     '-c', 'Release', '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~PyTextSearch',
                     '--logger', 'trx;LogFileName=result.trx', '--results-directory', target], 300)
        detail = verify_trx(target / 'result.trx')
        save(self.out / 'csharp-search-results.json', detail)
        return detail

    def prepare_sources(self):
        self.source_inputs = self.inputs(self.python)
        for path, digest in DATA['allowed_inputs'].items():
            require(sha(self.source_inputs / path) == digest, '锁定来源映射不符：' + path)
        full = self.source_inputs / 'markdown_sample.py'
        (self.source_inputs / 'markdown_plain_probe.py').write_bytes(old.plain_probe(full))
        self.preserve(self.source_inputs, 'inputs')

    def oracle(self, name, entry, golden, inputs):
        env = dict(PATH='/usr/bin:/bin', LANG='C.UTF-8', LC_ALL='C.UTF-8', PYTHONNOUSERSITE='1',
                   PYTHONHASHSEED='0', PYTHONUTF8='1', PYTHONDONTWRITEBYTECODE='1')
        if inputs is not None:
            env['PYTHONPATH'] = os.pathsep.join(map(str, [inputs / 'stdlib', inputs / 'markdown']))
        label = name + '-cp'
        require(label not in self.labels, '禁止重复CP')
        self.labels.add(label)
        cwd = self.out / 'cwd' / label; cwd.mkdir(parents=True)
        before = sha(entry)
        record = shared.capture(self.out / label, [self.python, '-B', entry], cwd, 15, env,
                                limit=128 * 1024, memory_limit=512 * 1024**2)
        require(sha(entry) == before and sha(self.python) == shared.PYTHON_SHA, 'CP前后身份改变')
        require(old.positive(record, self.out / label, golden), '同源CP不等于完整冻结golden')

    def domain(self, name, entry, golden, inputs=None, markdown=False, native=True):
        if not self.phase(name + '-cp', lambda: self.oracle(name, entry, golden, inputs)):
            return
        self.product_timeout = 30 if markdown else 15
        self.phase(name + '-jit', lambda: product.jit(self, name, entry, golden, inputs, markdown))
        if native:
            self.phase(name + '-native', lambda: product.markdown_boundary(self, name, entry, inputs) if markdown
                   else product.native(self, name, entry, golden, inputs, False))
        self.product_timeout = 30

    def products(self):
        if self.phase('supported-cp', self.supported_oracle):
            golden = (self.out / 'supported-oracle.stdout').read_bytes()
            self.phase('supported-jit', lambda: self.jit('supported', ROOT / old.SUPPORTED, golden))
            self.phase('supported-native', lambda: self.native(ROOT / old.SUPPORTED, golden))
        self.phase('full-source-deletion-boundary',
                   lambda: self.jit('full-source-deletion-boundary', ROOT / old.CONTRACT, None))
        self.expand_products()
        ready = self.phase('sourceprep', self.prepare_sources)
        for name, row in DATA['fixtures'].items():
            inputs = self.source_inputs if ready and name == 'markupbase_updatepos' else None
            if name == 'markupbase_updatepos' and not ready: continue
            self.domain(name, ROOT / row['source'], bytes.fromhex(row['stdout_hex']), inputs, native=row.get('native', True))
        if ready:
            golden = (self.source_inputs / 'stdout.txt').read_bytes()
            require(len(golden) == 77, 'Markdown完整golden大小不符')
            for name, filename, expected in [('markdown-plain', 'markdown_plain_probe.py', golden[:48]),
                                             ('markdown-full', 'markdown_sample.py', golden)]:
                self.domain(name, self.source_inputs / filename, expected, self.source_inputs, True)

    def finish_identity(self):
        require(self.before is not None and self.identity('after') == self.before, '源码身份改变/未采集')
        require(all(sha(Path(p)) == h for p, h in self.frozen.items()), '源/工具/runtime/产物身份改变')
        self.result['identity_preserved'] = True

    def execute(self):
        try:
            if not self.phase('identity', self.identities): return 1
            if not self.phase('csharp-build', lambda: self.managed(build_only=True)): return 1
            self.phase('csharp-typing', lambda: self.managed_domain('typing'))
            self.phase('csharp-expandtabs', lambda: self.managed_domain('expandtabs'))
            self.phase('csharp-search', self.managed_search)
            if not self.phase('tools', self.build_tools): return 1
            self.products()
            self.result['supported_contract_passed'] = all(self.result['stages'][n]['passed'] for n in
                ('csharp-build', 'csharp-typing', 'tools', 'supported-cp', 'supported-jit', 'supported-native'))
            self.result['passed'] = all(x['passed'] for x in self.result['stages'].values())
        except Exception as error:
            self.result.update(passed=False, error=str(error), traceback=traceback.format_exc())
        finally:
            try:
                self.finish_identity()
            except Exception as error:
                self.result.update(passed=False, supported_contract_passed=False, identity_preserved=False,
                                   identity_error=str(error))
                for stage in self.result['stages'].values():
                    if stage['passed']: stage.update(passed=False, status='invalidated-identity')
                    if 'all_passed' in stage: stage['all_passed'] = False
            save(self.out / 'result.json', self.result)
        return 0 if self.result['passed'] else 1


if __name__ == '__main__':
    raise SystemExit(Runner().execute())
