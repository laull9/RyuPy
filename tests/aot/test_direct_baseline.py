"""直接基线编排纯mock：同PE命令、诊断后置、失败保留证据，不构建SDK。"""
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

import direct_baseline as baseline
from perf_schema import FIELDS


class DirectBaselineTests(unittest.TestCase):
    def test_profilers_and_loader_injection_are_not_inherited(self):
        original={'CORECLR_ENABLE_PROFILING':'1','COR_PROFILER':'x','LD_PRELOAD':'x',
                  'LD_LIBRARY_PATH':'x','DOTNET_TieredCompilation':'0','RYUPY_STATS':'x','PYTHONPROFILEIMPORTTIME':'1','PATH':'tools'}
        env=baseline.measurement_environment(original)
        for key in original:
            if key!='PATH':self.assertNotIn(key,env)
        self.assertEqual(env['PATH'],'tools')

    def exercise(self, root, *, bad_diagnostic=False):
        target=root/'target';(target/'release').mkdir(parents=True)
        (target/'release/ryupy').write_text('not executed')
        out=root/'out';calls=[];layouts={}
        class FakeRunner:
            def __init__(self, logs, deadline):self.logs=logs;logs.mkdir()
            def run(self,label,command,*,cwd,env,limit,stats=False):
                calls.append((label,list(map(str,command)),dict(env),stats))
                case=self.logs/label;case.mkdir()
                value=b'expected';stderr=b''
                if label in ('revision','final-revision'):value=b'a'*40
                elif label in ('clean','final-clean'):value=b''
                elif label=='sdk':value=b'10.0.403'
                elif label=='rustc':value=b'rustc fixed'
                elif label.startswith('build-'):
                    name=label.removeprefix('build-');output=Path(command[-1]);output.mkdir()
                    managed=output/(name+'.dll');managed.write_text('IL')
                    managed.with_suffix('.runtimeconfig.json').write_text('{}')
                    native=output/name;native.write_text('native')
                    manifest=output/'ryupy-artifact.json'
                    manifest.write_text(json.dumps({'schema_version':1,'toolchain':{'sdk_version':'10.0.403'},
                                                   'compiler':{'options':{}},'artifacts':[]}))
                    layouts[name]=SimpleNamespace(managed=managed,manifest=manifest)
                elif label.startswith('diagnostic-'):
                    if stats:data=dict.fromkeys(FIELDS,0)
                    else:data=dict.fromkeys(('main_ms','gc0','gc1','gc2'),0)
                    if bad_diagnostic:data={}
                    (case/'stats.json').write_text(json.dumps(data))
                return {'case':str(case),'exit':0,'stdout':value,'stderr':stderr,'wall_ms':1}
        with patch.object(baseline,'Runner',FakeRunner),patch.object(baseline.platform,'python_version',return_value='3.12.13'),\
             patch.object(baseline.platform,'machine',return_value='x86_64'),patch.object(baseline.shutil,'which',return_value='/fixed/dotnet'),\
             patch.object(baseline,'verified_native',side_effect=lambda output,bench,stderr:output/bench.name),\
             patch.object(baseline,'reported_aot',side_effect=lambda stderr,output,name,rid:layouts[name]):
            if bad_diagnostic:
                with self.assertRaises(ValueError):baseline.run(root,target,out)
            else:baseline.run(root,target,out)
        return out,calls,layouts

    def test_same_pe_and_diagnostics_follow_all_direct_samples(self):
        with tempfile.TemporaryDirectory() as temp:
            out,calls,layouts=self.exercise(Path(temp))
            report=json.loads((out/'run.json').read_text());self.assertEqual(report['status'],'passed')
            labels=[x[0] for x in calls];last_wall=max(i for i,x in enumerate(labels) if x.startswith('wall-'))
            self.assertTrue(all(i>last_wall for i,x in enumerate(labels) if x.startswith('diagnostic-')))
            for label,command,env,stats in calls:
                if label.startswith(('wall-','pre-')):
                    self.assertFalse(stats);self.assertNotIn('RYUPY_STATS',env)
                    self.assertFalse(any('harness' in x for x in command))
                    if label.endswith('-jit'):
                        name=label.split('-')[-2]
                        self.assertEqual(command,['/fixed/dotnet',str(layouts[name].managed)])
            wall=json.loads((out/'wall.json').read_text());self.assertEqual(len(wall['records']),54)
            diagnostic=json.loads((out/'diagnostics.json').read_text());self.assertEqual(len(diagnostic),9)
            self.assertTrue(all(x['comparable_wall'] is False and 'wall_ms' not in x for x in diagnostic))

    def test_bad_diagnostic_fails_overall_but_preserves_direct_evidence(self):
        with tempfile.TemporaryDirectory() as temp:
            out,_,_=self.exercise(Path(temp),bad_diagnostic=True)
            self.assertEqual(json.loads((out/'run.json').read_text())['status'],'failed')
            self.assertEqual(len(json.loads((out/'wall.json').read_text())['records']),54)


if __name__=='__main__':unittest.main()
