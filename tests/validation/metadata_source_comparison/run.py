#!/usr/bin/env python3
"""两源metadata单次诊断；复用原采集器/输入清单，失败不重试，不改旧门口径。"""
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import traceback

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
sys.path.insert(0, str(HERE.parent))
import markdown_preflight as shared
from distribution_metadata_identity import inventory, IdentityError

TITLE = 'diagnostic-not-acceptance: metadata-source-comparison-001'
REF = 'refs/heads/ci/metadata-source-comparison-001'
FEATURE = 'ryupy-driver/ordinary-dynamic-module-pilot'
FROZEN = json.loads((HERE / 'frozen.json').read_text())
FIXTURE = Path('tests/validation/distribution_metadata_fixtures')
DOMAINS = {'synthetic': 'main.py', 'missing-include': 'missing_include.py'}
VERSIONS = {'python': 'Python 3.12.13', 'dotnet': '10.0.403',
            'rustc': 'rustc 1.99.1 (58cf8f9c3 2026-10-13)',
            'cargo': 'cargo 1.99.1 (5f94df478 2026-08-27)'}
require, sha, save = shared.require, shared.sha, shared.save


def context(env, event):
    head = event.get('head_commit') or {}
    require(env.get('GITHUB_ACTIONS') == 'true' and env.get('RUNNER_ENVIRONMENT') == 'github-hosted'
            and env.get('GITHUB_EVENT_NAME') == 'push' and env.get('GITHUB_RUN_ATTEMPT') == '1'
            and env.get('GITHUB_REF') == event.get('ref') == REF
            and head.get('message') == TITLE and head.get('id') == event.get('after') == env.get('GITHUB_SHA'),
            '仅限专用分支精确标题的首次hosted push')
    for key in ('RUSTFLAGS', 'RUSTC_WRAPPER', 'RUSTC_WORKSPACE_WRAPPER', 'CARGO_ENCODED_RUSTFLAGS',
                'RUSTUP_TOOLCHAIN', 'CARGO_TARGET_DIR', 'LD_PRELOAD', 'LD_LIBRARY_PATH'):
        require(not env.get(key), '未声明注入：' + key)


def git(root, *args):
    return subprocess.check_output(['git', '-C', str(root), *args], timeout=30)


def source_identity(root, expected):
    """核整个tree及每个工作文件，不用clean状态冒充内容；构建输出仅在忽略域。"""
    head = git(root, 'rev-parse', 'HEAD').decode().strip()
    tree = git(root, 'rev-parse', 'HEAD^{tree}').decode().strip()
    require(head == expected['head'] and tree == expected['tree'], '源码head/tree不符')
    require(not git(root, 'status', '--porcelain', '--untracked-files=all'), '源码工作树改变')
    rows = shared.tree_records(git(root, 'ls-tree', '-rz', 'HEAD'))
    for name, row in rows.items():
        path = root / name
        require(path.is_file() and not path.is_symlink(), '源码类型改变：' + name)
        raw = path.read_bytes()
        blob = hashlib.sha1(b'blob ' + str(len(raw)).encode() + b'\0' + raw).hexdigest()
        require(blob == row['blob'], '源码blob改变：' + name)
        require(bool(path.stat().st_mode & 0o111) == (row['mode'] == '100755'), '源码mode改变：' + name)
    return {'head': head, 'tree': tree, 'files': rows}


def complete(record):
    return record.get('raw_streams_complete') is True and record.get('failure') is None


def succeeded(record):
    return complete(record) and record.get('exit') == 0


def commands(cli, fixture, product, name):
    args = [cli, 'build', fixture / DOMAINS[name], '--diagnostic-stages', '--out-dir', product,
            '-I', fixture]
    if name == 'synthetic': args += ['--include-module', 'probe_pkg.target']
    return args + ['--distribution-metadata', fixture / 'request.json']


def cp_command(python, fixture, name):
    script = 'import sys; p=sys.argv[1]; exec(compile(open(p,"rb").read(),p,"exec"),{"__name__":"__main__","__file__":p})'
    env = dict(PATH='/usr/bin:/bin', LANG='C.UTF-8', LC_ALL='C.UTF-8', PYTHONNOUSERSITE='1',
               PYTHONHASHSEED='0', PYTHONUTF8='1', PYTHONDONTWRITEBYTECODE='1')
    paths = [fixture / 'root-a', fixture / 'root-b']
    if name == 'synthetic': paths.insert(0, fixture)
    env['PYTHONPATH'] = os.pathsep.join(map(str, paths))
    return [python, '-S', '-B', '-c', script, fixture / DOMAINS[name]], env


class Runner:
    def __init__(self):
        self.out = ROOT / 'target/metadata-source-comparison'
        self.out.mkdir(parents=True, exist_ok=False)
        self.labels, self.frozen, self.inputs, self.sources = set(), {}, {}, {}
        self.result = {'scope': '只判断A已有或A→B变化；不足单独归因类工厂；不是兼容验收',
                       'accepted_delta': 0, 'retries': 0, 'stages': {}, 'windows': {}, 'comparisons': {}}
        self.invalid_sources, self.global_invalid = set(), False
        self.identity_errors = {}
        for key in ('A', 'B'):
            for name in DOMAINS:
                for mode in ('compile', 'jit'):
                    self.result['stages'][key + '-' + name + '-' + mode] = {'status': 'not-run'}
        self.env = {k: os.environ[k] for k in ('PATH', 'HOME', 'RUNNER_TEMP') if k in os.environ}
        self.env.update(DOTNET_ROOT=os.environ['DOTNET_ROOT'], DOTNET=os.environ['DOTNET'],
                        DOTNET_CLI_TELEMETRY_OPTOUT='1', DOTNET_NOLOGO='1', LC_ALL='C.UTF-8', TZ='UTC',
                        PYTHONDONTWRITEBYTECODE='1', PYTHONNOUSERSITE='1', PYTHONHASHSEED='0')

    def persist(self):
        save(self.out / 'result.json', self.result)

    def call(self, label, argv, cwd, timeout, env=None, cp=False):
        require(label not in self.labels, '禁止重复执行：' + label)
        self.labels.add(label)
        env = dict(self.env if env is None else env)
        save(self.out / (label + '-environment.json'), env)
        # 原capture先写argv再开原流；失败或采集异常不补跑。
        record = shared.capture(self.out / label, argv, cwd, timeout, env,
                                limit=128 * 1024 if cp else 32 * 1024**2,
                                memory_limit=512 * 1024**2 if cp else None)
        self.result['stages'][label] = record
        self.persist()
        return record

    def guard(self, source=None):
        try:
            require(not self.global_invalid and source not in self.invalid_sources, '先前身份失败，禁止恢复有效状态')
            self.check_identity(source)
        except Exception as error:
            self.identity_errors[source or 'global'] = str(error)
            if source: self.invalid_sources.add(source)
            else: self.global_invalid = True
            raise IdentityError(str(error)) from error

    def check_identity(self, source=None):
        for path, digest in self.frozen.items():
            if source and any(self.out / other in path.parents or self.sources[other] in path.parents
                              for other in ('A', 'B') if other != source): continue
            require(sha(path) == digest, '冻结文件变化：' + str(path))
        for path, expected in self.inputs.items():
            if source and any(self.out / other in path.parents or self.sources[other] in path.parents
                              for other in ('A', 'B') if other != source): continue
            require(inventory(path) == expected, '输入集合/类型变化')
        source_identity(ROOT, self.controller)
        if source: source_identity(self.sources[source], FROZEN['sources'][source])

    def stage(self, label, operation, source=None):
        try:
            self.guard(source)
            save(self.out / (label + '-identity-before.json'), {'source': source, 'verified': True,
                 'controller': self.controller, 'source_identity': FROZEN['sources'].get(source)})
            value = operation()
            self.guard(source)
            save(self.out / (label + '-identity-after.json'), {'source': source, 'verified': True})
            self.result['windows'][label] = 'valid'
            self.persist()
            return value
        except Exception as error:
            self.result['windows'][label] = 'invalidated' if isinstance(error, IdentityError) else 'failed'
            self.result['stages'][label] = {'status': 'failed', 'error': str(error),
                                           'traceback': traceback.format_exc()}
            self.persist()
            return None

    def fixture(self, target, source):
        shutil.copytree(source / FIXTURE, target)
        actual = shared.snapshot(target)
        require(actual == FROZEN['fixtures'], 'fixture不是冻结原字节')
        self.inputs[target] = inventory(target)
        require(self.inputs[target] == FROZEN['fixture_inventory'], 'fixture目录集合/类型不符')
        save(target.parent / (target.name + '-inventory.json'), self.inputs[target])
        return target

    def setup(self):
        context(os.environ, json.loads(Path(os.environ['GITHUB_EVENT_PATH']).read_text()))
        self.controller = {'head': os.environ['GITHUB_SHA'],
                           'tree': git(ROOT, 'rev-parse', 'HEAD^{tree}').decode().strip()}
        save(self.out / 'controller-source.json', source_identity(ROOT, self.controller))
        self.tools = {'python': Path(os.environ['PYTHON']).resolve(), 'dotnet': Path(os.environ['DOTNET']).resolve(),
                      'rustc': Path(shutil.which('rustc')).resolve(), 'cargo': Path(shutil.which('cargo')).resolve()}
        require(sys.version_info[:3] == (3, 12, 13) and sha(self.tools['python']) == shared.PYTHON_SHA, 'CP身份不符')
        for name, tool in self.tools.items():
            self.frozen[tool] = sha(tool)
            record = self.call(name + '-version', [tool, '--version'], ROOT, 30)
            require(succeeded(record) and (self.out / (name + '-version.stdout')).read_text().strip() == VERSIONS[name], '版本不符')
        save(self.out / 'tool-identity.json', {k: {'path': str(v), 'sha256': sha(v)} for k, v in self.tools.items()})
        download = Path(os.environ['RUNNER_TEMP']) / 'wcwidth-aot-tools/download-identity.json'
        declared = json.loads((ROOT / 'tests/aot/wcwidth_ci/downloads.json').read_text())
        rows = json.loads(download.read_text())
        require(len(rows) == len(declared) and all(r == dict(d, actual_digest=d['digest'], verified=True)
                for r, d in zip(rows, declared)), '固定下载身份不符')
        shutil.copy2(download, self.out / 'download-identity.json')
        self.frozen[download] = sha(download)
        for key, expected in FROZEN['sources'].items():
            source = ROOT.parent / ('source-' + key)
            identity = source_identity(source, expected)
            save(self.out / (key + '-source.json'), identity)
            if key == 'B':
                current = shared.tree_records(git(ROOT, 'ls-tree', '-rz', 'HEAD'))
                changed = {n for n in current.keys() | identity['files'].keys() if current.get(n) != identity['files'].get(n)}
                allowed = {'.github/workflows/metadata-source-comparison.yml',
                           *('tests/validation/metadata_source_comparison/' + n for n in ('run.py', 'test_run.py', 'frozen.json'))}
                require(changed <= allowed, '控制器改变旧源码/期望/工作流')
            for name, digest in expected['contracts'].items(): require(sha(source / name) == digest, '锁/合同变化')
            for name in ('Cargo.lock', 'tests/aot/wcwidth_ci/downloads.json'):
                require((source / name).read_bytes() == (ROOT / name).read_bytes(), '两源锁/工具清单漂移')
            self.sources[key] = source
            require(not (source / 'target').exists() and not list((source / 'runtime').glob('**/bin'))
                    and not list((source / 'runtime').glob('**/obj')), '源构建目录不是全新')
        self.golden = json.loads((self.sources['B'] / 'tests/validation/distribution_metadata.json').read_text())['goldens']
        self.cp_fixture = self.fixture(self.out / 'cp-fixtures', self.sources['B'])

    def cp(self, name):
        label = name + '-cp'
        cwd = self.out / 'cwd' / label; cwd.mkdir(parents=True)
        argv, env = cp_command(self.tools['python'], self.cp_fixture, name)
        return self.call(label, argv, cwd, 30, env, cp=True)

    def cache_locks(self):
        """共享缓存先验必须绑定两份真实锁，不以同名平台或历史成功替代。"""
        require(set(self.sources) == {'A', 'B'}, '共享fetch必须先核两源')
        receipts = {}
        for key in ('A', 'B'):
            lock = self.sources[key] / 'Cargo.lock'
            digest = sha(lock)
            require(digest == FROZEN['sources'][key]['contracts']['Cargo.lock'], 'fetch锁身份不符：' + key)
            receipts[key] = {'path': str(lock), 'sha256': digest}
        require(receipts['A']['sha256'] == receipts['B']['sha256'], '两源锁不等，不能共享fetch')
        return receipts

    def prepare_dependency_cache(self):
        # fetch不编译；不指定target才覆盖完整锁图。保locked，禁止命令重试和Cargo网络重试。
        # 后续metadata仍offline完整解析，不用no-deps或平台过滤掩盖缺件。
        before = self.cache_locks()
        for key in ('A', 'B'): self.guard(key)
        save(self.out / 'dependency-cache-before.json', before)
        env = dict(self.env, CARGO_NET_RETRY='0')
        record = self.call('dependency-cache-fetch', [self.tools['cargo'], 'fetch', '--locked',
                           '--manifest-path', self.sources['A'] / 'Cargo.toml'], self.sources['A'], 600, env)
        after = self.cache_locks()
        save(self.out / 'dependency-cache-after.json', after)
        require(before == after, 'fetch期间锁改变')
        for key in ('A', 'B'): self.guard(key)
        require(succeeded(record), '完整锁图缓存准备失败；不运行两源build/产品，不补取重试')
        return True

    def build(self, key):
        source = self.sources[key]; dest = self.out / key; dest.mkdir()
        env = dict(self.env, CARGO_TARGET_DIR=str(source / 'target'))
        cs = source / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj'
        record = self.call(key + '-csharp-build', [self.tools['dotnet'], 'build', cs, '-c', 'Release', '-nologo'], source, 600, env)
        require(succeeded(record), 'C#失败；该源不执行产品')
        self.guard(key)
        record = self.call(key + '-cargo-build', [self.tools['cargo'], 'build', '--locked', '-p', 'ryupy-cli', '--features', FEATURE], source, 1200, env)
        require(succeeded(record), 'cargo失败；该源不执行产品')
        self.guard(key)
        # metadata是工作区已解析库存，不代表每包实际参与CLI编译；offline/locked仍保完整门。
        dep = self.call(key + '-dependencies', [self.tools['cargo'], 'metadata', '--locked', '--offline', '--format-version', '1',
                        '--features', 'ryupy-driver/ordinary-dynamic-module-pilot'], source, 30, env)
        require(succeeded(dep), '完整解析依赖来源缺失')
        deps = json.loads((self.out / (key + '-dependencies.stdout')).read_text())
        save(self.out / (key + '-dependency-scope.json'), {
            'kind': 'workspace-resolved-inventory', 'all_packages_compiled': False,
            'features': FEATURE, 'locked': True, 'offline': True, 'no_deps': False})
        dep_inputs = {}
        for package in deps['packages']:
            manifest = Path(package['manifest_path'])
            self.frozen[manifest] = sha(manifest)
            checksum = manifest.parent / '.cargo-checksum.json'
            dep_inputs[package['id']] = {'source': package['source'], 'manifest': str(manifest),
                'manifest_sha256': sha(manifest), 'registry_checksum': json.loads(checksum.read_text()) if checksum.is_file() else None}
            if checksum.is_file(): self.frozen[checksum] = sha(checksum)
        save(self.out / (key + '-dependency-inputs.json'), dep_inputs)
        assets = {str(p.relative_to(source)): {'sha256': sha(p), 'content': json.loads(p.read_text())}
                  for p in (source / 'runtime').glob('**/obj/project.assets.json')}
        require(assets, 'NuGet实际解析来源缺失')
        save(self.out / (key + '-nuget-assets.json'), assets)
        sdk = self.call(key + '-sdk-info', [self.tools['dotnet'], '--info'], source, 30, env)
        require(succeeded(sdk), 'SDK解析证据缺失')
        tools = dest / 'tools'; tools.mkdir()
        shutil.copy2(source / 'target/debug/ryupy', tools / 'ryupy')
        shutil.copy2(source / 'runtime/RyuPy.Runtime/bin/Release/net10.0/RyuPy.Runtime.dll', tools / 'RyuPy.Runtime.dll')
        for path in tools.iterdir(): self.frozen[path] = sha(path)
        save(dest / 'tools.json', shared.snapshot(tools))
        self.fixture(dest / 'fixtures', source)
        return tools

    def product(self, key, name, tools):
        dest = self.out / key; fixture = dest / 'fixtures'; product = dest / 'products' / name
        require(not product.exists(), '拒绝残留产品')
        product.parent.mkdir(exist_ok=True)
        env = dict(self.env, RYUPY_RUNTIME_DIR=str(tools))
        label = key + '-' + name
        record = self.call(label + '-compile', commands(tools / 'ryupy', fixture, product, name), self.sources[key], 180, env)
        if not succeeded(record): return
        self.guard(key)
        pe = product / (Path(DOMAINS[name]).stem + '.dll')
        require(pe.is_file() and sha(product / 'RyuPy.Runtime.dll') == sha(tools / 'RyuPy.Runtime.dll'), 'PE/runtime缺失或混代')
        for filename in ('module-environment.json', 'optional-sources.json', 'distribution-metadata.json'):
            require((product / filename).is_file(), '计划缺失：' + filename)
        before = inventory(product); save(dest / (name + '-product-before.json'), before)
        cwd = self.out / 'cwd' / label; cwd.mkdir(parents=True)
        self.call(label + '-jit', [self.tools['dotnet'], pe], cwd, 30, env)
        require(inventory(product) == before, '执行改变产品集合/身份')

    def summarize(self):
        # 所有独立域采集结束才对golden判定，A失败不能阻止B或另一输入。
        for name in DOMAINS:
            observations, plans = {}, {}
            for variant in ('A', 'B'):
                product = self.out / variant / 'products' / name
                plans[variant] = {filename: json.loads((product / filename).read_text())
                    if (product / filename).is_file() else {'status': 'missing'}
                    for filename in ('module-environment.json', 'optional-sources.json', 'distribution-metadata.json')}
            for key in ('cp', 'A', 'B'):
                label = name + '-cp' if key == 'cp' else key + '-' + name + '-jit'
                row = self.result['stages'].get(label)
                window = name + '-cp-window' if key == 'cp' else key + '-' + name + '-window'
                if (row and complete(row) and self.result['windows'].get(window) == 'valid'
                        and not self.global_invalid and key not in self.invalid_sources):
                    out = (self.out / (label + '.stdout')).read_bytes()
                    err = (self.out / (label + '.stderr')).read_bytes()
                    observations[key] = {'exit': row['exit'], 'stdout_hex': out.hex(), 'stderr_hex': err.hex(),
                                         'golden_pass': row['exit'] == 0 and out == self.golden[name].encode() and not err}
                else: observations[key] = {'status': 'invalidated' if self.global_invalid or key in self.invalid_sources
                                           else self.result['windows'].get(window, 'not-run-or-incomplete'),
                                           'raw_record_retained': row is not None,
                                           'invalid_reason': self.identity_errors.get('global') or self.identity_errors.get(key)
                                           or self.result['stages'].get(window, {}).get('error')}
            self.result['comparisons'][name] = {'observations': observations, 'raw_plans_side_by_side': plans,
                'same_observation': observations['A'] == observations['B'] if 'exit' in observations['A'] and 'exit' in observations['B'] else None,
                'search_domains': 'CP missing-include不含fixture根，RyuPy原-I包含；不是相同搜索空间'}
        self.result['all_goldens_pass'] = all(o.get('golden_pass') for c in self.result['comparisons'].values() for o in c['observations'].values())

    def execute(self):
        try:
            self.setup()
            for name in DOMAINS: self.stage(name + '-cp-window', lambda n=name: self.cp(n))
            if self.stage('dependency-cache-window', self.prepare_dependency_cache):
                for key in self.sources:
                    tools = self.stage(key + '-build-window', lambda k=key: self.build(k), key)
                    if tools:
                        for name in DOMAINS:
                            self.stage(key + '-' + name + '-window', lambda k=key, n=name, t=tools: self.product(k, n, t), key)
            self.summarize()
        except Exception as error:
            self.result['fatal'] = {'error': str(error), 'traceback': traceback.format_exc()}
        finally:
            self.persist()
            save(self.out / 'evidence-files.json', shared.snapshot(self.out))
        return 0 if self.result.get('all_goldens_pass') and not self.result.get('fatal') and not any(
            x.get('status') == 'failed' for x in self.result['stages'].values()) else 1


if __name__ == '__main__':
    raise SystemExit(Runner().execute())
