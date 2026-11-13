"""公开bench命令管线的纯mock回归，不运行make/SDK/基准程序。"""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import bench_smoke as smoke
from test_manifest_verify import manifest_fixture


def payload():
    series = {'wall_ms': [1]*5, 'main_ms': [.5]*5, 'maxrss': [1024]*5, 'stats': {'main_ms': .5}}
    return {'environment': {'backend':'aot', 'runs':5, 'warmup':1, 'jit_metrics_applicable':False,
                           'resource_aggregation':'last_timed_process'},
            'results':[{'name':'hello','status':'ok','backend':'aot','variants':{},'compile_ms':1,
                        'cpython':copy.deepcopy(series),'ryupy':copy.deepcopy(series)}]}


def bundle(root, source):
    attempt=root/'hello.build/attempt-one';attempt.mkdir(parents=True)
    (root/'hello.build/ryupy-build-owner.json').write_bytes(b'owner-bytes');
    (attempt/'.hidden-sdk.cs').write_bytes(b'hidden-source');(attempt/'empty').mkdir()
    (attempt/'hello.dll').write_bytes(b'il');(attempt/'RyuPy.Runtime.dll').write_bytes(b'rt')
    host=attempt/'ryupy-aot-one/host';host.mkdir(parents=True);(host/'sdk.version').write_text('10.0.403\n')
    dist=root/'hello.dist';data=manifest_fixture(dist)
    # 真实无flag CLI采用catalog-only，管线回归须覆盖当前默认而非历史explicit-only。
    data['compiler']['options']['module_environment_policy']='catalog-only'
    (dist/'program').rename(dist/'hello');(dist/'hello').chmod(0o755)
    for record in data['artifacts']:
        if record['relative_path']=='program':record['relative_path']='hello'
    for record,name in zip(data['build_inputs'],('hello.dll','RyuPy.Runtime.dll')):
        raw=(attempt/name).read_bytes();record.update(size=len(raw),sha256=hashlib.sha256(raw).hexdigest())
    data['sources'][0]['compiled_text_sha256']=hashlib.sha256(source.read_bytes()).hexdigest()
    # 当前普通发布明确省略这些摘要；归档器自校验证据仍另算摘要。
    for item in data['sources']:item['compiled_text_sha256']=''
    for item in data['build_inputs']+data['artifacts']:item['sha256']=''
    (dist/'ryupy-artifact.json').write_text(json.dumps(data))


class BenchSmokeTests(unittest.TestCase):
    def test_schema_rejects_failed_backend_count_nonfinite(self):
        smoke.validate_result(payload())
        for change in ('failed','backend','count','nan'):
            data=payload()
            if change=='failed':data['results'][0]['status']='mismatch'
            elif change=='backend':data['environment']['backend']='jit'
            elif change=='count':data['results'][0]['ryupy']['wall_ms'].pop()
            else:data['results'][0]['ryupy']['main_ms'][0]=float('nan')
            with self.assertRaises(AssertionError):smoke.validate_result(data)

    def test_one_make_budget_and_fresh_bundle(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);target=root/'target';out=root/'evidence'
            source=root/'tests/benchmarks/hello.py';source.parent.mkdir(parents=True);source.write_text('print("hello")\n')
            old=target/'bench/aot/hello-old';old.mkdir(parents=True)
            def fake(label, command, **kwargs):
                self.assertEqual(command[:3],['make','bench-aot','BENCH=hello'])
                self.assertEqual(kwargs['timeout'],1800);self.assertTrue(kwargs['check'])
                self.assertEqual(kwargs['env']['CARGO_TARGET_DIR'],str(target))
                name=command[3].removeprefix('ARGS=--save ')
                saved=target/'bench/results'/f'{name}.json';saved.parent.mkdir(parents=True);saved.write_text(json.dumps(payload()))
                bundle(target/'bench/aot/hello-new',source)
            with patch.object(smoke.Runner,'run',side_effect=fake) as run:
                smoke.run(root,out,target)
                self.assertEqual(run.call_count,1)
            evidence=json.loads((out/'result.json').read_text())
            self.assertEqual(evidence['status'],'passed');self.assertEqual(evidence['bundle_relative_to_target'],'bench/aot/hello-new')
            self.assertTrue(old.exists());self.assertTrue((out/'saved-result.json').is_file())
            self.assertEqual((out/'owned-bundle/hello.build/attempt-one/.hidden-sdk.cs').read_bytes(),b'hidden-source')
            self.assertTrue((out/'owned-bundle/hello.build/attempt-one/empty').is_dir())
            self.assertEqual(evidence['archive']['owner_sha256'],hashlib.sha256(b'owner-bytes').hexdigest())

    def test_make_failure_preserves_status_never_checks_old_bundle(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);out=root/'evidence'
            with patch.object(smoke.Runner,'run',side_effect=RuntimeError('make failed')), patch.object(smoke,'validate_bundle') as verify:
                with self.assertRaises(RuntimeError):smoke.run(root,out,root/'target')
                verify.assert_not_called()
            self.assertEqual(json.loads((out/'result.json').read_text())['status'],'failed')

    def test_failed_make_preserves_only_unique_fresh_tree(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);out=root/'evidence';target=root/'target'
            def fail(*args,**kwargs):
                fresh=target/'bench/aot/hello-failed';fresh.mkdir(parents=True)
                (fresh/'.failure').write_bytes(b'diagnostic')
                raise RuntimeError('make failed')
            with patch.object(smoke.Runner,'run',side_effect=fail):
                with self.assertRaises(RuntimeError):smoke.run(root,out,target)
            self.assertEqual((out/'owned-bundle/.failure').read_bytes(),b'diagnostic')
            self.assertEqual(json.loads((out/'result.json').read_text())['status'],'failed')

    def test_archive_rejects_link_and_expired_budget(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source=root/'source';source.mkdir();out=root/'out';out.mkdir()
            (source/'link').symlink_to(root)
            with self.assertRaises(ValueError):smoke.archive_bundle(source,out,float('inf'))
            self.assertFalse((out/'owned-bundle').exists())
            (source/'link').unlink()
            with self.assertRaises(TimeoutError):smoke.archive_bundle(source,out,0)
            self.assertFalse((out/'owned-bundle').exists())

    def test_native_input_tamper_rejected(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp);source=root/'hello.py';source.write_text('print("hello")\n')
            dest=root/'bundle';bundle(dest,source)
            smoke.validate_bundle(dest,source)
            (dest/'hello.build/attempt-one/hello.dll').write_bytes(b'changed')
            with self.assertRaises(ValueError):smoke.validate_bundle(dest,source)


if __name__=='__main__':unittest.main()
