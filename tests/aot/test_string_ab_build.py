"""双构建编排mock：Runtime隔离、同源同PE、语义先行和诊断后置。"""
import json
from pathlib import Path
import tempfile
from types import SimpleNamespace
import unittest
from unittest.mock import patch

import string_ab as ab
from string_ab_compatibility import CP_OUTPUT, RYUPY_OUTPUT
from perf_schema import FIELDS


class BuildTest(unittest.TestCase):
    def exercise(self, folder, bad_sdk=False):
        root=folder/'candidate';base=folder/'base';targets={v:folder/(v+'-target') for v in ('base','candidate')}
        for p in (root,base):
            ab.runtime_dir(p).mkdir(parents=True);(ab.runtime_dir(p)/'RyuPy.Runtime.dll').write_text('runtime')
        sources=root/'tests/aot/string_ab_fixtures';sources.mkdir(parents=True)
        for c in ab.CASES:(sources/f'{c}.py').write_text('print(1)')
        semantic=root/'tests/e2e/phase3';semantic.mkdir(parents=True)
        for c in ('m3_6_numeric_accumulator','m3_10_string_accumulator'):(semantic/f'{c}.py').write_text('print(1)')
        compatibility=root/'tests/aot/string_compatibility_fixtures';compatibility.mkdir(parents=True)
        (compatibility/'custom_slots.py').write_text('print(1)')
        for t in targets.values():(t/'release').mkdir(parents=True);(t/'release/ryupy').write_text('cli')
        calls=[];layouts={}
        class FakeRunner:
            def __init__(self,logs,deadline):self.logs=logs;logs.mkdir()
            def run(self,label,command,*,cwd,env,limit,stats=False):
                calls.append((label,list(map(str,command)),cwd,dict(env),stats))
                case=self.logs/label;case.mkdir();value=b'1\n'
                if label.endswith(('-head','-final-head')):value=b'base-sha' if label.startswith('base-') else b'candidate-sha'
                elif label.endswith(('-clean','-final-clean')):value=b''
                elif label=='sdk':value=b'10.0.403'
                elif label.startswith('build-'):
                    output=Path(command[-1]);output.mkdir();name=Path(command[2]).stem
                    managed=output/(name+'.dll');managed.write_text('IL')
                    managed.with_suffix('.runtimeconfig.json').write_text('{}')
                    manifest=output/'manifest.json';manifest.write_text(json.dumps(dict(schema_version=1,
                        toolchain=dict(sdk_version='11.0.100' if bad_sdk else '10.0.403'),compiler=dict(options={}))))
                    layouts[str(output)]=SimpleNamespace(managed=managed,manifest=manifest)
                elif label.startswith('compatibility-'):
                    value=CP_OUTPUT if label.endswith('-cpython') else RYUPY_OUTPUT
                elif label.startswith('diagnostic-'):
                    data=dict.fromkeys(FIELDS if stats else ('main_ms','gc0','gc1','gc2'),0)
                    (case/'stats.json').write_text(json.dumps(data))
                return dict(case=str(case),exit=0,stdout=value,stderr=b'',wall_ms=1)
        out=folder/'out'
        with patch.object(ab,'Runner',FakeRunner),patch.object(ab.platform,'python_version',return_value='3.12.13'),\
             patch.object(ab.platform,'machine',return_value='x86_64'),patch.object(ab.shutil,'which',return_value='/sdk/dotnet'),\
             patch.object(ab,'verified_native',side_effect=lambda output,bench,stderr:output/bench.name),\
             patch.object(ab,'reported_aot',side_effect=lambda stderr,output,name,rid:layouts[str(output)]):
            if bad_sdk:
                with self.assertRaises(ValueError):ab.run(root,base,root,targets,out,'base-sha','candidate-sha')
            else:ab.run(root,base,root,targets,out,'base-sha','candidate-sha')
        return root,base,out,calls

    def test_isolation_and_sequence(self):
        with tempfile.TemporaryDirectory() as tmp:
            root,base,out,calls=self.exercise(Path(tmp));labels=[c[0] for c in calls]
            self.assertEqual(json.loads((out/'run.json').read_text())['status'],'passed')
            self.assertEqual(len(json.loads((out/'wall.json').read_text())['records']),250)
            self.assertEqual(len(json.loads((out/'diagnostics.json').read_text())),25)
            self.assertLess(max(i for i,x in enumerate(labels) if x.startswith('semantic-')),min(i for i,x in enumerate(labels) if x.startswith('wall-')))
            self.assertGreater(min(i for i,x in enumerate(labels) if x.startswith('diagnostic-')),max(i for i,x in enumerate(labels) if x.startswith('wall-')))
            for label,command,cwd,env,stats in calls:
                if label.startswith('build-'):
                    expected_root=base if label.startswith('build-base-') else root
                    self.assertEqual(cwd,expected_root)
                    self.assertEqual(env['RYUPY_RUNTIME_DIR'],str(ab.runtime_dir(expected_root)))
                    self.assertTrue(command[2].startswith(str(root)))
                if label.startswith(('wall-','pre-','semantic-')):
                    self.assertNotIn('RYUPY_STATS',env);self.assertFalse(stats)
                    self.assertFalse(any('harness' in c for c in command))

    def test_sdk_drift_stops_before_sampling(self):
        with tempfile.TemporaryDirectory() as tmp:
            _,_,out,calls=self.exercise(Path(tmp),True)
            self.assertEqual(json.loads((out/'run.json').read_text())['status'],'failed')
            self.assertFalse(any(x[0].startswith('wall-') for x in calls))


if __name__=='__main__':unittest.main()
