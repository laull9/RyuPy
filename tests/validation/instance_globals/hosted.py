# A1真实公共编译API与静态Host的单窗接线；先轻量JIT，后两次必要ILC。
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
from hosted_common import (sha, save, require, snapshot, capture, read_artifacts,
                           verify_five_trx, negative_result, aot_origin)
from metadata_contract import expected_for_origin

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[2]
OUT = ROOT / 'target/instance-globals-evidence'
FEATURE = 'ordinary-dynamic-module-pilot'


class Window:
    def __init__(self):
        self.env = os.environ.copy()
        self.env.update(LC_ALL='C.UTF-8', TZ='UTC', PYTHONDONTWRITEBYTECODE='1', PYTHONHASHSEED='0')
        self.dotnet = Path(self.env['DOTNET']).resolve(strict=True)
        self.python = Path(self.env['PYTHON']).resolve(strict=True)
        self.runtime = OUT / 'managed/RyuPy.Runtime.dll'
        self.driver = ROOT / 'target/debug/examples/instance_globals_validation'
        self.env['RYUPY_RUNTIME_DIR'] = str(self.runtime.parent)
        self.env['NUGET_PACKAGES'] = str(Path(self.env['RUNNER_TEMP']) / 'a1-nuget')

    def run(self, label, argv, timeout=300, expected=0):
        return capture(OUT, label, argv, self.env, ROOT, timeout, expected)

    def build(self, label, source, mode='jit', selected=('subject', 'indirect_subject', 'module_body'),
              cap=True, policy='pilot', resources=None, negative=False):
        folder = OUT / label
        require(not folder.exists(), '不重复已有编译')
        record = self.run(label, [self.driver, source, folder, source.parent, mode,
            resources or '-', ','.join(selected) or '-', str(cap).lower(), policy],
            900 if mode == 'aot' else 120, None if negative else 0)
        if negative:
            negative_result(folder, record)
            return None
        require((folder / 'result-kind.txt').read_text().strip() == 'success', 'driver未成功')
        artifact = read_artifacts(folder)
        save(OUT / (label + '-artifacts.json'), {k: {'path': str(v), 'sha256': sha(v)} for k, v in artifact.items()})
        require(sha(artifact['dll'].parent / 'RyuPy.Runtime.dll') == sha(self.runtime), 'driver混入旧Runtime')
        return artifact

    def selected_plan(self, label):
        plan = json.loads((OUT / label / 'module-environment.json').read_text())
        actual = {m['module'] for m in plan['modules'] if m.get('direct_globals', False)}
        require(actual == {'subject', 'indirect_subject', 'module_body'}, '新能力实际模块集合不符')
        require({m['module'] for m in plan['modules']} ==
                {'__main__', 'subject', 'indirect_subject', 'module_body', 'legacy_callback'}, '业务模块图变化')

    def pe(self, label, dll):
        self.run(label + '-decode', [self.python, ROOT / 'tests/validation/source_provenance/ci/read_pe_decoder.py', dll, 'new', 'a1'])
        self.run(label + '-check', [self.python, HERE / 'check_actual_pe.py', OUT / (label + '-decode.stdout'), '--hosted'])
        audit_env = self.env.copy()
        audit_env.update(A1_DECODED_PE=str(OUT / (label + '-decode.stdout')), A1_EXPECTED_HOSTED='1')
        capture(OUT, label + '-mutations', [self.python, '-B', '-m', 'unittest', 'discover',
                '-s', HERE, '-p', 'test_actual_pe.py'], audit_env, ROOT, 60)
        self.run(label + '-entry', [self.python, HERE / 'read_entry_identity.py', dll])
        value = json.loads((OUT / (label + '-entry.stdout')).read_text())
        require(value['method_name'] == 'Main' and value['signature_blob'] == '0001081d0e'
                and value['owner']['name'] == 'Module' and value['owner']['namespace'] == 'RyuPy.Generated',
                '静态Program入口不符')

    def host(self, label, dll, native=False):
        hostroot = OUT / label
        hostroot.mkdir()
        for name in ('Host.csproj', 'Host.cs', 'Program.cs'):
            shutil.copy2(HERE / name, hostroot / name)
        args = [self.dotnet, 'publish' if native else 'build', hostroot / 'Host.csproj',
                '-c', 'Release', '-o', hostroot / 'out', '-p:ProductAssembly=' + str(dll),
                '-p:RuntimeAssembly=' + str(self.runtime)]
        if native:
            args += ['-r', 'linux-x64', '-p:PublishAot=true']
        self.run(label + '-build', args, 900 if native else 180)
        actual = hostroot / 'out' / ('Host' if native else 'Host.dll')
        require(actual.is_file(), 'Host固定项目名产物缺失')
        if native:
            require(actual.read_bytes()[:4] == b'\x7fELF' and actual.stat().st_mode & 0o111, '非可执行ELF')
        else:
            require(sha(actual.parent / 'RyuPy.Runtime.dll') == sha(self.runtime), 'Host Runtime不一致')
        save(OUT / (label + '-identity.json'), {'path': str(actual), 'sha256': sha(actual),
             'product_pe': str(dll), 'product_sha256': sha(dll), 'runtime_sha256': sha(self.runtime)})
        return actual

    def execute(self, phase, host, origin, extra):
        files = [HERE / 'cp/EXPECTED.json', HERE / 'product_runner.py', HERE / 'metadata_contract.py',
                 self.runtime, host, *extra]
        files += [p for p in host.parent.rglob('*') if p.is_file()]
        config = {'files': [{'path': str(p), 'sha256': sha(p)} for p in sorted(set(files))],
                  'cp_expected': str(HERE / 'cp/EXPECTED.json'),
                  'env': {k: self.env[k] for k in ('PATH', 'DOTNET_ROOT', 'LC_ALL', 'TZ', 'PYTHONHASHSEED') if k in self.env},
                  'backends': [{'name': phase, 'argv': [str(self.dotnet), str(host)] if phase == 'jit' else [str(host)],
                                'cwd': str(host.parent), 'metadata_expected': expected_for_origin(origin)}]}
        config_path = OUT / (phase + '-config.json')
        save(config_path, config)
        self.run(phase + '-runner', [self.python, HERE / 'product_runner.py', '--execute', config_path, OUT / phase, phase], 240)

    def jit(self):
        OUT.mkdir(parents=True, exist_ok=False)
        sources = snapshot(ROOT)
        save(OUT / 'source-before.json', sources)
        self.run('head', ['git', 'rev-parse', 'HEAD', 'HEAD^{tree}'])
        require((OUT / 'head.stdout').read_text().splitlines()[0] == self.env['GITHUB_SHA'], 'HEAD不等事件SHA')
        self.run('clean-before', ['git', 'status', '--porcelain', '--untracked-files=normal'])
        require((OUT / 'clean-before.stdout').read_bytes() == b'', '初始源码不clean')
        tools = {'python': self.python, 'dotnet': self.dotnet,
                 'rustc': Path(shutil.which('rustc')).resolve(), 'cargo': Path(shutil.which('cargo')).resolve()}
        versions = {'python': 'Python 3.12.13', 'dotnet': '10.0.403',
                    'rustc': 'rustc 1.99.1 (58cf8f9c3 2026-10-13)', 'cargo': 'cargo 1.99.1 (5f94df478 2026-08-27)'}
        for name, path in tools.items():
            self.run(name + '-version', [path, '--version'], 30)
            require((OUT / (name + '-version.stdout')).read_text().strip() == versions[name], '工具版本不符')
        require(sha(self.python) == '021044895e95be79dc2f110367607e684119afbc8ce75f6f0eec94844e0acec7', '固定CP字节不符')
        save(OUT / 'tools.json', {k: {'path': str(p), 'sha256': sha(p)} for k, p in tools.items()})
        self.run('runner-mock', [self.python, '-B', '-m', 'unittest', 'discover', '-s', HERE, '-p', 'test_product_runner.py'], 60)
        tests = ROOT / 'runtime/RyuPy.Runtime.Tests/RyuPy.Runtime.Tests.csproj'
        self.run('managed-build', [self.dotnet, 'build', tests, '-c', 'Release', '-o', self.runtime.parent], 600)
        self.run('managed-two', [self.dotnet, 'test', tests, '-c', 'Release', '-o', self.runtime.parent,
            '--no-build', '--no-restore', '--filter', 'FullyQualifiedName~PyDirectGlobalsTests|FullyQualifiedName~PyDirectGlobalsAbiContractTests',
            '--logger', 'trx;LogFileName=direct.trx', '--results-directory', OUT / 'trx'], 180)
        verify_five_trx(OUT / 'trx/direct.trx')
        self.run('driver-build', ['cargo', 'build', '--locked', '-p', 'ryupy-driver', '--features', FEATURE,
                                '--example', 'instance_globals_validation'], 1200)
        built = {'runtime': sha(self.runtime), 'driver': sha(self.driver)}
        save(OUT / 'built-before-products.json', built)
        cases = json.loads((HERE / 'negative/CASES.json').read_text())['cases']
        require(len(cases) == 13, '负例集合变更')
        for case in cases:
            source = (HERE / 'negative' / case['input']).resolve()
            require(source.is_relative_to(HERE / 'negative'), '负例输入越界')
            self.build('reject-' + case['id'], source, selected=case['selected'],
                       cap=case['instance_direct_globals'], policy='pilot', negative=True)
        for policy in ('explicit', 'pilot'):
            plain = self.build('policy-' + policy, HERE / 'policy-inputs/entry.py', selected=(), policy=policy)
            plan = json.loads((OUT / ('policy-' + policy) / 'module-environment.json').read_text())
            require(all(not m.get('direct_globals', False) for m in plan['modules']), '未显式选择却获得能力')
            self.run('policy-' + policy + '-decode', [self.python,
                ROOT / 'tests/validation/source_provenance/ci/read_pe_decoder.py', plain['dll'], 'new', 'a1-policy'])
            decoded = json.loads((OUT / ('policy-' + policy + '-decode.stdout')).read_text())
            require(not any(i.get('target', {}).get('name') == 'CallDirectGlobals'
                for m in decoded['methods'] for i in m['body']['instructions']
                if i['opcode'] in ('0x28', '0x6f', '0xfe06', '0xfe07')), '未选择PE实际引用helper')
        art = self.build('jit-product', HERE / 'inputs/entry.py')
        self.selected_plan('jit-product')
        self.pe('jit-pe', art['dll'])
        host = self.host('jit-host', art['dll'])
        self.execute('jit', host, str((HERE / 'inputs/subject.py').resolve()), [art['dll']])
        require(snapshot(ROOT) == sources, '源码窗口变化')
        require(sha(self.runtime) == built['runtime'] and sha(self.driver) == built['driver'], 'JIT Runtime/driver变化')
        require(all(sha(p) == json.loads((OUT / 'tools.json').read_text())[k]['sha256'] for k, p in tools.items()), 'JIT工具变化')
        self.run('clean-after-jit', ['git', 'status', '--porcelain', '--untracked-files=normal'])
        require((OUT / 'clean-after-jit.stdout').read_bytes() == b'', 'JIT后源码不clean')
        save(OUT / 'state.json', {'source': sources, 'tools': json.loads((OUT / 'tools.json').read_text()),
             'runtime': sha(self.runtime), 'driver': sha(self.driver), 'jit_complete': True})

    def native(self):
        state = json.loads((OUT / 'state.json').read_text())
        require(state['jit_complete'] and snapshot(ROOT) == state['source'], 'JIT前置/源码不符')
        for item in state['tools'].values():
            require(sha(item['path']) == item['sha256'], '工具变化')
        require(sha(self.runtime) == state['runtime'] and sha(self.driver) == state['driver'], 'Runtime/driver变化')
        resources = HERE / 'resources.json'
        art = self.build('aot-product', HERE / 'inputs/entry.py', mode='aot', resources=resources)
        require('native' in art and 'manifest' in art, '原公共AOT未完成')
        self.selected_plan('aot-product')
        self.pe('aot-pe', art['dll'])
        host = self.host('native-host', art['dll'], native=True)
        # 空files仍须保来源manifest；若以后新增资源不可静默漏复制。
        manifest = json.loads(art['manifest'].read_text())
        require({x['module_name'] for x in manifest['module_locations']} ==
                {'subject', 'indirect_subject', 'module_body', 'legacy_callback'}, 'AOT虚拟来源锚不全')
        require(manifest['resources'] == [], '当前合同仅emptyfiles，新增资源需明确部署')
        shutil.copy2(art['manifest'], host.parent / 'product-artifact.json')
        self.execute('native', host, aot_origin(art['manifest'], host), [art['dll'], art['manifest']])
        moved = OUT / 'native-moved-dist'
        shutil.copytree(host.parent, moved)
        moved_host = moved / host.name
        require(sha(moved_host) == sha(host), '搬移ELF变化')
        self.execute('native-moved', moved_host, aot_origin(art['manifest'], moved_host), [art['dll'], art['manifest']])
        self.run('final-runner', [self.python, HERE / 'product_runner.py', '--final', OUT], 60)
        require(snapshot(ROOT) == state['source'] and sha(self.runtime) == state['runtime']
                and sha(self.driver) == state['driver'], '结束身份不符')
        require(all(sha(x['path']) == x['sha256'] for x in state['tools'].values()), '结束工具变化')
        self.run('clean-after-native', ['git', 'status', '--porcelain', '--untracked-files=normal'])
        require((OUT / 'clean-after-native.stdout').read_bytes() == b'', 'Native后源码不clean')
        save(OUT / 'HOSTED-FINAL.json', {'nine_processes': 'verified by runner', 'source_unchanged': True,
             'scope': '显式有限域；无项目gain/性能/推导式/trace修复', 'public_aot_then_host_ilc': True})


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--phase', required=True, choices=('jit', 'native'))
    args = parser.parse_args()
    try:
        getattr(Window(), args.phase)()
    except Exception as error:
        OUT.mkdir(parents=True, exist_ok=True)
        save(OUT / (args.phase + '-FAILED.json'), {'error_type': type(error).__name__, 'message': str(error)})
        raise


if __name__ == '__main__':
    main()
