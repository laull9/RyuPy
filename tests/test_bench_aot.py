"""AOT基准入口纯轻测：模拟构建/进程，伪原生头只校验文件，绝不运行SDK或被测程序。"""
import contextlib
import hashlib
import io
import json
from pathlib import Path
import sys
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

sys.path.insert(0, str(Path(__file__).resolve().parent))
sys.path.insert(0, str(Path(__file__).resolve().parent / "aot"))
import run_benchmarks as entry
from benchlib import artifact, measure, report
from benchlib.suite import Benchmark
from test_manifest_verify import manifest_fixture


class BenchAotTests(unittest.TestCase):
    def test_parse_aot_profile_rejected_before_work(self):
        with contextlib.redirect_stderr(io.StringIO()), self.assertRaises(SystemExit):
            entry.parse_args(["--aot", "--profile"])
        self.assertTrue(entry.parse_args(["--aot"]).aot)

    def test_backend_comparison_and_invalid_results(self):
        report.validate_backend("jit", {}, {})
        with self.assertRaises(ValueError):
            report.validate_backend("aot", {}, {})
        with self.assertRaises(ValueError):
            report.validate_backend("aot", {"backend":"aot"}, {"x":{"backend":"jit"}})
        bench = Benchmark("x", Path("x.py"))
        for status in ("mismatch", "build-failed", "run-failed", "pending"):
            result = measure.Result(bench, measure.Series([2]), measure.Series([1]), status=status)
            self.assertIsNone(report.speedup(result))
            self.assertEqual(report.summary([result]), [])
            rows = report.compare_table([result], {}, {"x":{"status":"ok","ryupy":measure.Series([2]).to_json()}})
            self.assertFalse(any(line.startswith("| x |") for line in rows))

    def test_early_compare_rejects_before_build(self):
        with tempfile.TemporaryDirectory() as temp:
            target=Path(temp); path=entry.results_path(target,"old")
            path.parent.mkdir(parents=True);path.write_text(json.dumps({"environment":{},"results":[]}))
            with patch.object(measure,"target_dir",return_value=target), patch.object(measure,"build_cli") as build, contextlib.redirect_stderr(io.StringIO()):
                self.assertEqual(entry.main(["--aot","--compare","old","hello"]),2)
                build.assert_not_called()

    def test_save_rejects_cross_legacy_and_invalid_before_build(self):
        cases = [
            (True, {"environment":{"backend":"jit"},"results":[]}),
            (False, {"environment":{"backend":"aot"},"results":[]}),
            (True, {"environment":{},"results":[]}),
            (True, {"environment":None,"results":[]}),
            (True, {"environment":{"backend":"aot"},"results":[{"name":"x","backend":"jit"}]}),
            (True, "not JSON"),
        ]
        for aot, old in cases:
            with self.subTest(old=old), tempfile.TemporaryDirectory() as temp:
                target=Path(temp); path=entry.results_path(target,"saved")
                path.parent.mkdir(parents=True)
                original=(old if isinstance(old,str) else json.dumps(old)).encode()
                path.write_bytes(original)
                with patch.object(measure,"target_dir",return_value=target), patch.object(measure,"build_cli") as build, patch.object(measure,"measure_bench") as sample, contextlib.redirect_stderr(io.StringIO()):
                    args=(["--aot"] if aot else [])+["--save","saved","hello"]
                    self.assertEqual(entry.main(args),2)
                    build.assert_not_called();sample.assert_not_called()
                self.assertEqual(path.read_bytes(),original)

    def test_save_same_backend_and_legacy_jit_continue(self):
        for backend, old_env in (("aot",{"backend":"aot"}),("jit",{"backend":"jit"}),("jit",{})):
            with self.subTest(backend=backend,old=old_env), tempfile.TemporaryDirectory() as temp:
                target=Path(temp);path=entry.results_path(target,"saved")
                path.parent.mkdir(parents=True);path.write_text(json.dumps({"environment":old_env,"results":[]}))
                # 在mock构建入口停止，证明前置准入允许同backend，而非执行任何构建或采样。
                with patch.object(measure,"target_dir",return_value=target), patch.object(measure,"build_cli",side_effect=RuntimeError("mock-build-reached")) as build:
                    args=(["--aot"] if backend=="aot" else [])+["--save","saved","hello"]
                    with self.assertRaisesRegex(RuntimeError,"mock-build-reached"):
                        entry.main(args)
                    build.assert_called_once()

    def test_aot_failure_does_not_return_old_artifact(self):
        with tempfile.TemporaryDirectory() as temp:
            root=Path(temp); old=root/'bench/aot/old/program.dist/program';old.parent.mkdir(parents=True);old.write_bytes(b'old')
            with patch.object(measure.subprocess,"run",return_value=SimpleNamespace(returncode=1,stderr="failure")) as run:
                product,_,error=measure.build_case(root,Path("cli"),Benchmark("program",root/'program.py'),root,backend="aot")
                self.assertIsNone(product);self.assertEqual(error,"failure")
                self.assertIn("--aot",run.call_args.args[0]);self.assertEqual(old.read_bytes(),b'old')

    def test_native_direct_command_and_original_sampling(self):
        bench=Benchmark("x",Path("x.py")); stats={"main_ms":1,"alloc_bytes":2,"jit_ms":0,"jit_methods":0}
        sample=measure.Sample(1,2,stats,b'ok',b'',0)
        with patch.object(measure,"build_case",return_value=(Path('/owned/x.dist/x'),4,"")), patch.object(measure,"run_once",return_value=sample) as run:
            result=measure.measure_bench(measure.Plan(Path('.'),"python",1,5,True,False,"aot"),Path('cli'),Path('target'),bench)
            self.assertEqual(run.call_count,12)
            self.assertEqual(len(result.ryupy.wall_ms),5)
            for call in run.call_args_list[1::2]: self.assertEqual(call.args[0],['/owned/x.dist/x'])
            self.assertIs(result.ryupy.stats,stats)
            self.assertEqual(result.backend,"aot")

    def test_report_saved_raw_jit_and_units(self):
        raw={"jit_ms":0,"jit_methods":0,"alloc_bytes":1048576}
        result=measure.Result(Benchmark('x',Path('x.py')),ryupy=measure.Series([1],[.5],[1024],raw),backend='aot')
        text='\n'.join(report.resource_table([result]));self.assertIn('N/A',text);self.assertIn('MiB',text)
        with tempfile.TemporaryDirectory() as temp:
            path=Path(temp)/'saved.json';report.save(path,{'backend':'aot'},[result]); env, rows=report.load(path)
            self.assertEqual(rows['x']['ryupy']['stats'],raw);self.assertEqual(rows['x']['backend'],'aot')

    def fixture(self, root):
        bench=Benchmark('program',root/'program.py');bench.path.write_text('print(1)\n')
        build=root/'program.build/attempt-current';build.mkdir(parents=True)
        managed=build/'program.dll';managed.write_bytes(b'IL');(build/'RyuPy.Runtime.dll').write_bytes(b'RT')
        sdk=build/'ryupy-aot-current';(sdk/'host').mkdir(parents=True);(sdk/'publish').mkdir()
        (sdk/'host/sdk.version').write_text('10.0.403\n')
        dist=root/'program.dist';data=manifest_fixture(dist);(dist/'program').chmod(0o755)
        data['sources'][0]['compiled_text_sha256']=hashlib.sha256(bench.path.read_bytes()).hexdigest()
        for item,name in zip(data['build_inputs'],['program.dll','RyuPy.Runtime.dll']):
            raw=(build/name).read_bytes();item.update(size=len(raw),sha256=hashlib.sha256(raw).hexdigest())
        (dist/'ryupy-artifact.json').write_text(json.dumps(data))
        log=f'已生成 {managed}\n已生成 {dist / "program"}\nAOT 清单：{dist / "ryupy-artifact.json"}\n'
        return bench,log,dist

    def test_verified_native_and_input_hash_reject(self):
        with tempfile.TemporaryDirectory() as temp, patch.object(artifact,'host_rid',return_value='linux-x64'):
            root=Path(temp);bench,log,dist=self.fixture(root)
            self.assertEqual(artifact.verified_native(root,bench,log),dist/'program')
            (root/'program.build/attempt-current/program.dll').write_bytes(b'bad')
            with self.assertRaises(ValueError): artifact.verified_native(root,bench,log)

    def test_current_empty_digest_native_pipeline_and_size_guard(self):
        with tempfile.TemporaryDirectory() as temp, patch.object(artifact,'host_rid',return_value='linux-x64'):
            root=Path(temp);bench,log,dist=self.fixture(root)
            manifest=dist/'ryupy-artifact.json';data=json.loads(manifest.read_text())
            for source in data['sources']:source['compiled_text_sha256']=''
            for item in data['build_inputs']+data['artifacts']:item['sha256']=''
            manifest.write_text(json.dumps(data))
            self.assertEqual(artifact.verified_native(root,bench,log),dist/'program')
            (root/'program.build/attempt-current/program.dll').write_bytes(b'longer')
            with self.assertRaises(ValueError):artifact.verified_native(root,bench,log)

    def test_missing_or_tampered_manifest_never_falls_back(self):
        with tempfile.TemporaryDirectory() as temp, patch.object(artifact,'host_rid',return_value='linux-x64'):
            root=Path(temp);bench,log,dist=self.fixture(root)
            with self.assertRaises(ValueError): artifact.verified_native(root,bench,log.replace('已生成 ', 'old ',1))
            (dist/'program').write_bytes(b'not-native')
            with self.assertRaises(ValueError): artifact.verified_native(root,bench,log)
            (dist/'ryupy-artifact.json').unlink()
            with self.assertRaises((OSError,ValueError)): artifact.verified_native(root,bench,log)


if __name__=='__main__': unittest.main()
