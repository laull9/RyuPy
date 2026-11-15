"""组合证据门轻测：不允许漏feature、错误ABI计数或归档字节伪装通过。"""
import copy
import hashlib
import json
from pathlib import Path
import tempfile
import unittest
import integration_gate as gate
from integration_semantic import archive_for_relocation, MANIFEST_NAME

class GateTests(unittest.TestCase):
    def test_relocation_archive_paths(self):
        with tempfile.TemporaryDirectory() as t:
            out=Path(t);directory=out/'arm';dist=directory/'x.dist';dist.mkdir(parents=True)
            entry=dist/'x';entry.write_bytes(b'fake-native');(dist/MANIFEST_NAME).write_text('{}')
            row={'entry':str(entry.relative_to(out)), 'sha256':hashlib.sha256(entry.read_bytes()).hexdigest()}
            moved=archive_for_relocation(entry,directory,out,row)
            self.assertFalse((out/row['original_entry']).exists())
            self.assertEqual((out/row['entry']).read_bytes(),b'fake-native')
            self.assertTrue((out/row['manifest']).is_file())
            self.assertEqual(out/row['relocated'],moved/'x')
            self.assertEqual((out/row['relocated']).read_bytes(),(out/row['entry']).read_bytes())

    def test_feature_resolution(self):
        names=['ryupy-cli','ryupy-driver','ryupy-mir','ryupy-e2e']
        data={'packages':[{'name':n,'id':n} for n in names], 'resolve':{'nodes':[
            {'id':n,'features':[],'dependencies':['ryupy-driver'] if n in ('ryupy-e2e','ryupy-cli') else []} for n in names]}}
        with tempfile.TemporaryDirectory() as t:
            p=Path(t)/'m.json'; p.write_text(json.dumps(data)); gate.features(p,'off')
            for node in data['resolve']['nodes'][1:3]:node['features']=['generator-sum-experiment','records-increment-experiment']
            p.write_text(json.dumps(data)); gate.features(p,'on')
            for index in (1,2):
                bad=copy.deepcopy(data);bad['resolve']['nodes'][index]['features'].pop()
                p.write_text(json.dumps(bad))
                with self.assertRaises(AssertionError):gate.features(p,'on')
            for index in (0,3):
                bad=copy.deepcopy(data);bad['resolve']['nodes'][index]['dependencies']=[]
                p.write_text(json.dumps(bad))
                with self.assertRaises(AssertionError):gate.features(p,'on')
            data['resolve']['nodes'][0]['features'].append('records-increment-diagnostics')
            p.write_text(json.dumps(data))
            with self.assertRaises(AssertionError):gate.features(p,'on')

    def test_exact_increment_and_archives(self):
        with tempfile.TemporaryDirectory() as t:
            root=Path(t); folder=root/'check-on';(folder/'pe').mkdir(parents=True)
            frozen=root/'frozen-dev-abi'; dll=frozen/'build/bin/RyuPy.Runtime/release/RyuPy.Runtime.dll';dll.parent.mkdir(parents=True)
            dll.write_bytes(b'baseline mock'); candidate=folder/'pe/generator-loaded-candidate.dll';candidate.write_bytes(b'candidate mock')
            digest=lambda p:hashlib.sha256(p.read_bytes()).hexdigest()
            manifest=dict(commit='81758bb477729aee4636f2ca6d6b0815b949849a',sdk='10.0.403',configuration='Release',dll=str(dll),bytes=dll.stat().st_size,sha256=digest(dll))
            mp=frozen/'manifest.json';mp.write_text(json.dumps(manifest))
            additions=[f'addition-{n}' for n in range(11)]
            data=dict(commit=manifest['commit'],sdk=manifest['sdk'],oldSignatures=1,candidateSignatures=12,additions=additions,removed=[],added=additions,actual=['old']+additions,expected=['old']+additions,sha256=digest(dll),candidateSha256=digest(candidate),baseline=str(dll),baselineBytes=dll.stat().st_size,baselineManifest=str(mp),candidateArchive=candidate.name,candidateBytes=candidate.stat().st_size,candidate='/original/loaded.dll')
            report=folder/'pe/generator-frozen-dev-abi.json';report.write_text(json.dumps(data));gate.historical_artifact(folder)
            for key,value in [('candidateSignatures',9),('additions',additions[:8]),('removed',['old']),('candidateSha256','0'*64)]:
                bad=data|{key:value};report.write_text(json.dumps(bad))
                with self.assertRaises(AssertionError):gate.historical_artifact(folder)
            report.write_text(json.dumps(data));candidate.write_bytes(b'tampered')
            with self.assertRaises(AssertionError):gate.historical_artifact(folder)

if __name__=='__main__':unittest.main()
