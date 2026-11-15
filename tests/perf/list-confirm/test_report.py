"""复测报告合成正负例；不表示真实产品执行成功。"""
import json
from pathlib import Path
import tempfile
import unittest
import report


class Report(unittest.TestCase):
    def setUp(self):
        self.temp=tempfile.TemporaryDirectory();self.addCleanup(self.temp.cleanup);self.root=Path(self.temp.name)
        for name,value in [('base.txt',report.BASE),('candidate.txt',report.CANDIDATE),('python.txt','Python 3.12.13'),('sdk.txt','10.0.403')]:
            (self.root/name).write_text(value)
        for index in (1,2,3):
            folder=self.root/f'round-{index}';folder.mkdir();rows=[]
            for name in sorted(report.NAMES):
                samples={a:[{'ms':2 if (a=='B')==(index==2) else 1,'rss':42} for _ in range(15)] for a in ('CP','A','B')}
                rows.append({'name':name,'samples':samples,'stats':{a:{'main_ms':1,'alloc_bytes':1} for a in ('A','B')},'free_bytes_before_build':{a:4*1024**3 for a in ('A','B')}})
                for arm in ('A','B'):
                    path=folder/'manifests'/name/arm/'ryupy-artifact.json';path.parent.mkdir(parents=True)
                    path.write_text(json.dumps({'toolchain':{'sdk_version':'10.0.403','target_framework_major':10}}))
            old,new=('candidate','base') if index==2 else ('base','candidate')
            (folder/'paired.json').write_text(json.dumps({'runs':15,'warmup':1,'old':f'/tmp/frozen-{old}/ryupy','new':f'/tmp/frozen-{new}/ryupy','rows':rows}))

    def mutate(self,field,value):
        p=self.root/'round-2/paired.json';d=json.loads(p.read_text());d[field]=value;p.write_text(json.dumps(d))

    def test_swap_normalization(self):
        result=report.verify(self.root)
        for rounds in result['results'].values():
            self.assertEqual([r['candidate_over_base'] for r in rounds],[0.5,0.5,0.5])

    def test_wrong_mapping_rejected(self):
        self.mutate('old','/tmp/frozen-base/ryupy')
        with self.assertRaises(AssertionError):report.verify(self.root)

    def test_short_samples_rejected(self):
        p=self.root/'round-2/paired.json';d=json.loads(p.read_text());d['rows'][0]['samples']['A'].pop();p.write_text(json.dumps(d))
        with self.assertRaises(AssertionError):report.verify(self.root)

    def test_missing_sdk_rejected(self):
        next((self.root/'round-2/manifests').rglob('*.json')).unlink()
        with self.assertRaises(FileNotFoundError):report.verify(self.root)

    def test_wrong_source_rejected(self):
        (self.root/'candidate.txt').write_text('bad')
        with self.assertRaises(AssertionError):report.verify(self.root)

    def test_nonfinite_rejected(self):
        p=self.root/'round-2/paired.json';d=json.loads(p.read_text());d['rows'][0]['samples']['A'][0]['ms']=float('inf');p.write_text(json.dumps(d))
        with self.assertRaises(AssertionError):report.verify(self.root)


if __name__=='__main__':unittest.main()
