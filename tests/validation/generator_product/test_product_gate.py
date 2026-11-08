"""纯Python离线合成/原件解析自测；不启动CP fixture、Rust、.NET、产品或CI。"""
import copy
import json
from pathlib import Path
import struct
import sys
import tempfile
import unittest
from unittest.mock import patch
import product_contract as c
import generator_product_gate as gate

HERE = Path(__file__).resolve().parent
REPO = HERE.parents[2]
ORACLE = HERE / 'goldens'


def fake_pe():
    data = bytearray(1024)
    data[:2] = b'MZ'
    struct.pack_into('<I', data, 0x3c, 0x80)
    data[0x80:0x84] = b'PE\0\0'
    struct.pack_into('<H', data, 0x86, 1)
    struct.pack_into('<H', data, 0x94, 224)
    struct.pack_into('<H', data, 0x98, 0x10b)
    struct.pack_into('<I', data, 0x98 + 96 + 14 * 8, 0x2000)
    struct.pack_into('<IIII', data, 0x98 + 224 + 8, 128, 0x2000, 128, 512)
    struct.pack_into('<II', data, 512 + 16, 1, 0x06000001)
    return bytes(data)


def ins(op, arg=None):
    return {'Op': op, 'Arg': arg, 'Offset': 0}


def decoded(pe):
    return {'Path': str(pe), 'Sha256': c.sha(pe).upper(), 'Methods': [
        {'Name': 'RyuPy.Generated.Module::Main[0;0;1](String[])->Int32', 'IL': [
            ins('call', '[RyuPy.Runtime]RyuPy.Runtime.PySystem::Initialize[0;0;2](String,String[])->Void'),
            ins('ldc.i8', '1'), ins('call', c.PREFIX + 'Enter' + c.SIGNATURES['Enter']),
            ins('call', 'RyuPy.Generated.Module::body[0;0;0]()->Void')]},
        {'Name': 'RyuPy.Generated.Module::body[0;0;0]()->Void', 'IL': [
            ins('call', c.PREFIX + 'Explicit' + c.SIGNATURES['Explicit']),
            ins('call', c.PREFIX + 'GeneratorCatch' + c.SIGNATURES['GeneratorCatch'])]}]}


class ContractTests(unittest.TestCase):
    @classmethod
    def setUpClass(cls):
        cls.reference = c.oracle(ORACLE, REPO / 'tests/cases/generator_dynamic/frames/entry.py')

    def temporary(self):
        return tempfile.TemporaryDirectory(dir=HERE)

    def test_actual_frozen_four_oracles_and_b_source(self):
        self.assertEqual(tuple(self.reference), c.CASES)
        subset = c.oracle(HERE / 'goldens', REPO / 'tests/cases/generator_dynamic/frames/entry.py')
        self.assertEqual({name: row['normalized'] for name, row in subset.items()},
                         {name: row['normalized'] for name, row in self.reference.items()})
        self.assertEqual(c.sha(self.reference[c.CASES[1]]['source']), c.B_SHA)
        b = self.reference[c.CASES[1]]['normalized']
        self.assertIn(b'During handling of the above exception, another exception occurred:', b)
        self.assertEqual([int(n) for n in c.re.findall(rb'line ([0-9]+)', b)], [11, 20, 12, 5])

    def test_full_stream_normalization_does_not_swallow_noise_or_extra_frames(self):
        reference = self.reference[c.CASES[1]]
        with self.temporary() as directory:
            entry = Path(directory) / 'entry.py'
            entry.write_bytes(reference['source'].read_bytes())
            raw = reference['normalized'].replace(b'<SOURCE>', str(entry).encode())
            record = {'exit': 1, 'failure': None, 'raw_streams_complete': True}
            self.assertEqual(c.compare(reference, record, b'ready\n', raw, entry), reference['normalized'])
            variants = [raw + b'noise\n', b'noise\n' + raw,
                        raw.replace(b'line 12', b'line 11'), raw.replace(b'ValueError: original', b'ValueError: other'),
                        raw.replace(b'During handling', b'The above exception was the direct cause; During handling'),
                        raw.replace(str(entry).encode(), b'/different/entry.py')]
            for text in variants:
                with self.assertRaises(ValueError):
                    c.compare(reference, record, b'ready\n', text, entry)
            for bad in (0, 2, 124, -9, True):
                with self.assertRaises(ValueError):
                    c.compare(reference, dict(record, exit=bad), b'ready\n', raw, entry)
            with self.assertRaises(ValueError):
                c.compare(reference, dict(record, raw_streams_complete=False), b'ready\n', raw, entry)
            with self.assertRaises(ValueError):
                c.compare(reference, record, b'ready\nnoise', raw, entry)

    def test_only_exact_source_display_and_caret_can_disappear(self):
        source = b'raise ValueError("x")\n'
        raw = b'  File "entry.py", line 1, in f\n    raise ValueError("x")\n    ^^^^^\nValueError: x\n'
        clean = c.normalize(raw, source, {'entry.py'})
        self.assertNotIn(b'^^^^', clean)
        wrong = c.normalize(raw.replace(b'raise ValueError("x")', b'not actual source'), source, {'entry.py'})
        self.assertNotEqual(clean, wrong)
        with self.assertRaises(ValueError):
            c.normalize(raw.replace(b'line 1', b'line 2'), source, {'entry.py'})

    def test_rejection_is_compile_only_and_no_emit(self):
        record = {'exit': 1, 'failure': None, 'raw_streams_complete': True}
        error = '[E0310] trace V1 整产物拒绝：例子\n'.encode()
        with self.temporary() as directory:
            output = Path(directory)
            c.rejection(record, b'', error, output)
            for stdout, stderr in ((b'ready\n', error), (b'', b'TypeError: wrong\n'),
                                   (b'', error + b'Traceback (most recent call last):\n')):
                with self.assertRaises(ValueError):
                    c.rejection(record, stdout, stderr, output)
            for name, data in (('entry.dll', b'MZ'), ('entry', b'\x7fELF')):
                path = output / name
                path.write_bytes(data)
                with self.assertRaises(ValueError):
                    c.rejection(record, b'', error, output)
                path.unlink()

    def test_real_entry_token_root_signature_and_default_controls(self):
        with self.temporary() as directory:
            pe = Path(directory) / 'entry.dll'
            pe.write_bytes(fake_pe())
            data = decoded(pe)
            self.assertEqual(c.audit_pe(data, pe, True)['abi'], 1)
            for mutation in ('hash', 'abi', 'root', 'signature', 'detached', 'before-init'):
                changed = copy.deepcopy(data)
                il = changed['Methods'][0]['IL']
                if mutation == 'hash':
                    changed['Sha256'] = '0' * 64
                elif mutation == 'abi':
                    il[1]['Arg'] = '2'
                elif mutation == 'root':
                    changed['Methods'].reverse()
                elif mutation == 'signature':
                    il[2]['Arg'] = il[2]['Arg'].replace('Int64', 'Object')
                elif mutation == 'detached':
                    il[-1]['Arg'] = 'missing-target'
                else:
                    il[:3] = [il[1], il[2], il[0]]
                with self.assertRaises(ValueError):
                    c.audit_pe(changed, pe, True)
            with self.assertRaises(ValueError):
                c.audit_pe(data, pe, False)
            data['Methods'] = [{'Name': 'Main', 'IL': []}]
            self.assertFalse(c.audit_pe(data, pe, False)['enabled'])
            pe.write_bytes(fake_pe() + b'PyTraceProduct\0')
            data['Sha256'] = c.sha(pe)
            with self.assertRaises(ValueError):
                c.audit_pe(data, pe, False)

    def test_original_sources_same_mode_and_explicit_default(self):
        common = ['cli', '--trace-product-v1', 'build', 'entry.py', '--diagnostic-stages', '--out-dir', 'output']
        self.assertEqual(gate.build_command('cli', 'entry.py', 'output'), common)
        self.assertEqual(gate.build_command('cli', 'entry.py', 'output', native=True), common + ['--aot'])
        self.assertEqual(gate.build_command('cli', 'entry.py', 'output', enabled=False), [common[0]] + common[2:])
        with self.assertRaises(ValueError):
            gate.Runner(json.loads((HERE / 'contract.template.json').read_text()))

    def test_failed_native_execution_still_has_full_archive(self):
        _, native = gate.load_reused(REPO)
        with self.temporary() as directory:
            root = Path(directory)
            dist, evidence = root / 'entry.dist', root / 'evidence'
            dist.mkdir(); evidence.mkdir()
            (dist / 'entry').write_bytes(b'\x7fELFsynthetic-not-executed')
            (dist / 'ryupy-artifact.json').write_text('{}')
            before = native.preserve_dist(dist, evidence)
            with self.assertRaises(ValueError):
                c.require(False, '合成的运行后失败')
            self.assertEqual(before, native.snapshot(dist)[0])
            self.assertTrue((evidence / 'native-dist.tar.gz').is_file())
            self.assertTrue((evidence / 'dist-identity.json').is_file())

    def test_runner_archives_native_before_a_failing_product_run(self):
        _, native = gate.load_reused(REPO)
        with self.temporary() as directory:
            root = Path(directory)
            runner = gate.Runner.__new__(gate.Runner)
            runner.out, runner.work = root / 'evidence', root / 'work'
            runner.out.mkdir(); runner.work.mkdir()
            runner.native, runner.oracle = native, self.reference
            runner.config = {'pic_modes': ['1', '0']}
            runner.tools = {'cli': root / 'not-executed'}
            def fake_command(stage, label, argv, expected, timeout, pic=None):
                if label != 'compile':
                    raise ValueError('合成的目标退出失败，未启动任何子进程')
                output = Path(argv[argv.index('--out-dir') + 1])
                dist = output / 'entry.dist'
                dist.mkdir(parents=True)
                (dist / 'entry').write_bytes(b'\x7fELFsynthetic')
                (dist / 'ryupy-artifact.json').write_text('{}')
                return {}, b'', b''
            runner.command = fake_command
            runner.publish_inputs = lambda *args: root / 'not-executed.dll'
            runner.decode = lambda *args: None
            with self.assertRaises(ValueError):
                runner.stage(c.CASES[0], 'native')
            evidence = runner.out / (c.CASES[0] + '-native')
            self.assertTrue((evidence / 'native-dist.tar.gz').is_file())
            self.assertEqual(json.loads((evidence / 'dist-identity.json').read_text())['phase'], 'before-native-execution')

    def test_fixed20_and_ten_successes_are_per_id_not_counts(self):
        gate.load_reused(REPO)
        import project_gate_contract as old
        contract, _ = old.load_inputs(REPO)
        protected = set(contract['protected_native']) | set(contract['promotion_requires'])
        self.assertEqual(protected, {'tomli', 'docopt', 'tabulate', 'texttable', 'chardet',
                                    'wcwidth', 'colorama', 'termcolor', 'binaryornot', 'progress'})
        rows = [{'id': name, 'status': 'native' if name in protected else 'unsupported'}
                for name in contract['project_ids']]
        self.assertTrue(old.evaluate(contract, rows)['promotion_pass'])
        for name in protected:
            changed = copy.deepcopy(rows)
            next(r for r in changed if r['id'] == name)['status'] = 'failed'
            self.assertFalse(old.evaluate(contract, changed)['promotion_pass'])
        for bad in (rows[:-1], rows + [rows[0]], rows[:-1] + [{'id': 'replacement', 'status': 'native'}]):
            with self.assertRaises(old.GateError):
                old.evaluate(contract, bad)

    def test_fullcheck_evidence_requires_raws_identity_and_correct_scope(self):
        import regression_contract as regressions
        with self.temporary() as directory:
            root = Path(directory)
            candidate = {'head': 'head', 'status': '', 'files': {'source.rs': 'source-sha'}}
            product = {'contract': {'frozen': True, 'commit': 'head', 'tree': 'tree',
                                   'runtime': {'sha256': 'a' * 64}}, 'candidate': candidate,
                       'compiler_provenance': {'files': {'raw': 'raw-sha'}, 'runtime_sha256': 'a' * 64}}
            frozen = root / 'freeze.json'
            frozen.write_text(json.dumps(product))
            (root / 'mode.json').write_text(json.dumps({'commit': 'head', 'mode': 'default', 'python_sha256': 'c' * 64}))
            (root / 'result.json').write_text(json.dumps({'passed': True, 'mode': 'default'}))
            commands = [{'name': 'make-check', 'exit': 0, 'argv': ['make', 'check', 'CARGO_FEATURES=']},
                        {'name': 'git', 'exit': 0, 'argv': ['git', 'rev-parse', 'HEAD', 'HEAD^{tree}']}]
            (root / 'commands.json').write_text(json.dumps(commands))
            # 只有三个摘要JSON时绝不能过。
            with self.assertRaises((ValueError, FileNotFoundError)):
                regressions.fullcheck(root, frozen, 'default')
            for name in ('make-check', 'git'):
                for suffix in ('.stdout', '.stderr'):
                    (root / (name + suffix)).write_text('')
            (root / 'git.stdout').write_text('head\ntree\n')
            tools = {name: {'path': '/synthetic/' + name, 'sha256': 'c' * 64}
                     for name in ('python', 'dotnet', 'cargo', 'rustc')}
            identity = {'mode': 'default', 'cargo_features': '', 'candidate_before': candidate,
                        'candidate_after': candidate, 'tools_before': tools, 'tools_after': tools,
                        'runtimes_before_tests': {role: {'path': '/own/' + role, 'sha256': 'b' * 64}
                                                  for role in ('managed-tests', 'driver-e2e')},
                        'runtimes_after_tests': {role: {'path': '/own/' + role, 'sha256': 'b' * 64}
                                                 for role in ('managed-tests', 'driver-e2e')}}
            (root / 'build-identity.json').write_text(json.dumps(identity))
            result = regressions.fullcheck(root, frozen, 'default')
            self.assertFalse(any(result['runtime_matches_product'].values()))  # 独立模式允许实际字节不同。
            product['contract']['shared_artifacts'] = ['fullcheck-default-driver-e2e']
            frozen.write_text(json.dumps(product))
            with self.assertRaises(ValueError):
                regressions.fullcheck(root, frozen, 'default')
            product['contract']['shared_artifacts'] = []
            frozen.write_text(json.dumps(product))
            commands[0]['argv'] = ['cargo', 'test', 'focused']
            (root / 'commands.json').write_text(json.dumps(commands))
            with self.assertRaises(ValueError):
                regressions.fullcheck(root, frozen, 'default')

    def test_compiler_provenance_requires_real_streams_and_matching_tool_identity(self):
        import build_provenance
        with self.temporary() as directory:
            root = Path(directory)
            contract = {'commit': 'head', 'tree': 'tree', 'cli': {'sha256': 'a' * 64},
                        'runtime': {'sha256': 'b' * 64}}
            argv = {'git': ['git', 'rev-parse', 'HEAD', 'HEAD^{tree}'],
                    'runtime-build': ['make', 'runtime'],
                    'compiler-build': ['cargo', 'build', '--locked', '-p', 'ryupy-cli',
                                       '--features', 'ryupy-driver/ordinary-dynamic-module-pilot'],
                    'rustc': ['rustc', '--version'], 'cargo': ['cargo', '--version'], 'dotnet': ['dotnet', '--info']}
            (root / 'commands.json').write_text(json.dumps([{'name': name, 'argv': command, 'exit': 0}
                                                           for name, command in argv.items()]))
            for name in argv:
                (root / (name + '.stdout')).write_text('head\ntree\n' if name == 'git' else '')
                (root / (name + '.stderr')).write_text('')
            identity = {'cli_sha256': 'a' * 64, 'runtime_sha256': 'b' * 64,
                        'feature': 'ryupy-driver/ordinary-dynamic-module-pilot', 'configuration': 'debug'}
            (root / 'compiler-identity.json').write_text(json.dumps(identity))
            build_provenance.compiler_evidence(root, contract)
            (root / 'runtime-build.stdout').unlink()
            with self.assertRaises(ValueError):
                build_provenance.compiler_evidence(root, contract)
            (root / 'runtime-build.stdout').write_text('')
            identity['runtime_sha256'] = 'c' * 64
            (root / 'compiler-identity.json').write_text(json.dumps(identity))
            with self.assertRaises(ValueError):
                build_provenance.compiler_evidence(root, contract)

    def test_pic_trx_requires_exact_cold_and_warm_rows(self):
        import regression_contract
        import xml.etree.ElementTree as ET
        with self.temporary() as directory:
            trx = Path(directory) / 'synthetic.trx'
            root = ET.Element('TestRun')
            results = ET.SubElement(root, 'Results')
            prefix = 'RyuPy.Runtime.Tests.PyTraceProductPicTests.ColdAndHotGetterFailureForwardSameOwnerWithoutLegacyAttach'
            for n, warm, site in ((1, 'False', 3801), (2, 'True', 3802)):
                ET.SubElement(results, 'UnitTestResult', testId=str(n), outcome='Passed',
                              testName=prefix + f'(warm: {warm}, site: {site})')
            ET.ElementTree(root).write(trx)
            self.assertEqual(len(regression_contract.pic_mechanism(trx)['tests']), 2)
            results[1].set('outcome', 'NotExecuted')
            ET.ElementTree(root).write(trx)
            with self.assertRaises(ValueError):
                regression_contract.pic_mechanism(trx)
            results.remove(results[1])
            ET.ElementTree(root).write(trx)
            with self.assertRaises(ValueError):
                regression_contract.pic_mechanism(trx)

    def test_fullcheck_identity_collector_is_ordered_and_stable_without_processes(self):
        import importlib.util
        gate.load_reused(REPO)
        spec = importlib.util.spec_from_file_location('identity_draft', REPO / 'tests/aot/wcwidth_ci/build_identity.py')
        module = importlib.util.module_from_spec(spec)
        spec.loader.exec_module(module)
        candidate = {'head': 'head', 'status': '', 'files': {'source': 'same'}}
        tools = {name: {'path': '/synthetic/' + name, 'sha256': 'c' * 64}
                 for name in ('python', 'dotnet', 'cargo', 'rustc')}
        runtimes = {name: {'path': '/synthetic/' + name, 'sha256': 'b' * 64}
                    for name in ('managed-tests', 'driver-e2e')}
        with self.temporary() as directory:
            output = Path(directory)
            with patch.object(module, 'candidate_identity', return_value=candidate), \
                 patch.object(module, 'tool_identities', return_value=tools), \
                 patch.object(module, 'runtimes', return_value=runtimes), \
                 patch.dict(module.os.environ, {'GITHUB_SHA': 'head'}):
                module.before(REPO, output, 'default', '')
                module.before_tests(REPO, output)
                with self.assertRaises(RuntimeError):
                    module.before_tests(REPO, output)
                module.after(REPO, output)
                result = json.loads((output / 'build-identity.json').read_text())
                self.assertEqual(result['runtimes_before_tests'], result['runtimes_after_tests'])
                self.assertEqual(result['candidate_before'], result['candidate_after'])
                with patch.object(module, 'candidate_identity', return_value=dict(candidate, head='changed')):
                    with self.assertRaises(RuntimeError):
                        module.after(REPO, output)

    def test_hot_oracle_is_real_exit_zero_and_never_a_rejection(self):
        reference = c.hot_oracle(HERE / 'goldens')
        self.assertEqual(reference['exit'], 0)
        self.assertEqual(reference['stdout'], b'ready\ncaught\nready\ncaught\n')
        self.assertEqual(reference['stderr'], b'')
        self.assertNotIn(c.HOT, c.REJECTED)
        self.assertEqual(c.REJECTED, frozenset(c.CASES[2:]))
        with self.temporary() as directory:
            entry = Path(directory) / 'entry.py'
            entry.write_bytes(reference['source_bytes'])
            c.compare(reference, {'exit': 0, 'failure': None, 'raw_streams_complete': True},
                      reference['stdout'], b'', entry)
            entry.write_bytes(reference['source_bytes'] + b'\n')
            with self.assertRaises(ValueError):
                c.compare(reference, {'exit': 0, 'failure': None, 'raw_streams_complete': True},
                          reference['stdout'], b'', entry)

    def test_hot_pe_site_is_unique_not_a_runtime_hit_counter(self):
        def instruction(op, arg=None, pop='Pop0', push='Push0', count=None, returns=None):
            return {'Op': op, 'Arg': arg, 'Offset': 0, 'Pop': pop, 'Push': push,
                    'CallArgs': count, 'CallReturns': returns}
        il = [instruction('ldc.i4.0', push='Pushi'), instruction('ldnull', push='Pushref'),
              instruction('ldstr', 'kind', push='Pushref'),
              instruction('call', '[RyuPy.Runtime]RyuPy.Runtime.PyPic::GetAttr[0;0;3](Int32,Object,String)->Object',
                          'Varpop', 'Varpush', 3, True)]
        data = {'Methods': [{'Name': 'resume', 'Signature': '()->Void', 'IL': il}]}
        result = c.hot_site(data, REPO)
        self.assertEqual(result['sites'][0]['site_id'], 0)
        self.assertIn('no product hit counts', result['claim'])
        data['Methods'].append(copy.deepcopy(data['Methods'][0]))
        with self.assertRaises(AssertionError):
            c.hot_site(data, REPO)

    def test_runner_rejects_source_changed_during_compile(self):
        with self.temporary() as directory:
            root = Path(directory)
            runner = gate.Runner.__new__(gate.Runner)
            runner.out, runner.work = root / 'evidence', root / 'work'
            runner.out.mkdir(); runner.work.mkdir()
            runner.oracle, runner.tools = self.reference, {'cli': root / 'not-executed'}
            def fake_command(stage, label, argv, *unused):
                entry = Path(argv[argv.index('build') + 1])
                entry.write_bytes(entry.read_bytes() + b'\n')
                return {}, b'', b''
            runner.command = fake_command
            with self.assertRaisesRegex(ValueError, '编译阶段改变原源'):
                runner.stage(c.CASES[0], 'jit')

    def test_strict_legacy_module_plan(self):
        with self.temporary() as directory:
            path = Path(directory) / 'module-environment.json'
            good = {'modules': [{'module': '__main__', 'execution': 'LegacySingleton', 'compiled': True}]}
            path.write_text(json.dumps(good))
            gate.require_plan(path)
            for key, value in (('module', 'another'), ('compiled', False), ('execution', 'InstanceGlobals')):
                bad = copy.deepcopy(good)
                bad['modules'][0][key] = value
                path.write_text(json.dumps(bad))
                with self.assertRaises(ValueError):
                    gate.require_plan(path)


if __name__ == '__main__':
    unittest.main()
