"""控制证据合成正负例；不代替真实NativeAOT。"""
import json
from pathlib import Path
import tempfile
import unittest
import controls


class Controls(unittest.TestCase):
    def test_all_raw_streams_samples_and_identity(self):
        with tempfile.TemporaryDirectory() as tmp:
            root=Path(tmp);identity={'inputs':{},'cli':{x:{'path':f'/frozen-{x}/ryupy','sha256':'b'*64} for x in ('off','on')}}
            for index in (1,2,3,4):
                for selected,names in controls.CASES.items():
                    folder=root/f'round-{index}'/selected;folder.mkdir(parents=True);rows=[]
                    for name in names:
                        raw=folder/'raw'/name;raw.mkdir(parents=True);source='/source/'+name+'.py'
                        (raw/'source.py').write_bytes(b'print(1)')
                        identity['inputs'][name]={'path':name+'.py','sha256':controls.digest(raw/'source.py')}
                        arms={a:{'cli':identity['cli'][x]['path'],'cli_sha256':'b'*64,'executable':f'/build/{a}/{name}',
                                 'executable_sha256':'c'*64} for a,x in ({'A':'on','B':'off'} if index%2==0 else {'A':'off','B':'on'}).items()}
                        data={'source':source,'source_sha256':identity['inputs'][name]['sha256'],'arms':arms}
                        (raw/'identities.json').write_text(json.dumps(data));(raw/'complete.txt').write_text('COMPLETE\n')
                        row={'name':name,'samples':{a:[{'ms':1,'rss':2} for _ in range(12)] for a in ('A','B','CP')},'stats':{'A':{},'B':{}}};rows.append(row)
                        tags=['oracle']+[f'sample-{n}-{a}' for n in range(13) for a in ('A','B','CP')]+['stats-A','stats-B','moved-A','moved-B']
                        for tag in tags:
                            command=['/python','-B',source] if tag=='oracle' or tag.endswith('-CP') else [arms[tag[-1]]['executable']]
                            cwd='/source'
                            if tag.startswith('moved-'):cwd='/moved/'+tag[-1];command=[cwd+'/'+name]
                            (raw/(tag+'.json')).write_text(json.dumps({'exit':0,'ms':1,'rss':2,'stats':{},'command':command,'cwd':cwd}))
                            (raw/(tag+'.stdout')).write_bytes(b'1\n');(raw/(tag+'.stderr')).write_bytes(b'')
                        for a,d in arms.items():
                            (raw/(a+'-build-command.json')).write_text(json.dumps({'command':[d['cli'],'build',source,'-o','out','--aot','--native']}))
                    (folder/'paired.json').write_text(json.dumps({'rows':rows,'python':'/python'}))
            self.assertEqual(controls.verify_raw(root,identity),1056)
            raw=root/'round-2/original/raw/hello'
            for filename,new in [('sample-7-A.stdout',b'wrong'),('sample-7-A.stderr',b'warning'),('source.py',b'tampered')]:
                path=raw/filename;old=path.read_bytes();path.write_bytes(new)
                with self.assertRaises(AssertionError):controls.verify_raw(root,identity)
                path.write_bytes(old)
            path=raw/'identities.json';old=path.read_text();data=json.loads(old);data['arms']['A']['cli']='/wrong'
            path.write_text(json.dumps(data))
            with self.assertRaises(AssertionError):controls.verify_raw(root,identity)
            path.write_text(old)
            (raw/'sample-8-B.json').unlink()
            with self.assertRaises(AssertionError):controls.verify_raw(root,identity)


if __name__=='__main__':unittest.main()
