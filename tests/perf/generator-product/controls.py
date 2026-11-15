"""固定成功产品源码的回退风险控制实验；四轮六排列，不预设异常是噪声。"""
import argparse
import hashlib
import json
from pathlib import Path
import subprocess
import balanced

SOURCE = 'cfcb69dc7a728d0b6926bbd19f6d83f881873a13'
CASES = {'original':{'str_concat','sieve','hello','dict_ops','generators'},
         'generator-consumers':{'genexpr_sum'}}


def digest(path): return hashlib.sha256(path.read_bytes()).hexdigest()


def freeze(args):
    source=args.source.resolve()
    assert subprocess.check_output(['git','rev-parse','HEAD'],cwd=source,text=True).strip()==SOURCE
    assert not subprocess.check_output(['git','status','--porcelain'],cwd=source,text=True).strip()
    rows={}
    for selected,names in CASES.items():
        prefix='tests/benchmarks' if selected=='original' else 'tests/perf/generator_consumers'
        for name in names:
            relative=f'{prefix}/{name}.py';actual=balanced.ROOT/relative
            assert actual.read_bytes()==(source/relative).read_bytes()
            rows[name]={'path':relative,'sha256':digest(actual)}
    return {'source_commit':SOURCE,'source_tree':subprocess.check_output(['git','rev-parse','HEAD^{tree}'],cwd=source,text=True).strip(),
            'harness_commit':subprocess.check_output(['git','rev-parse','HEAD'],cwd=balanced.ROOT,text=True).strip(),
            'inputs':rows,'cli':{a:{'path':str(p.resolve()),'sha256':digest(p)} for a,p in [('off',args.old),('on',args.new)]}}


def verify_raw(root, identity):
    total=0
    for index in (1,2,3,4):
        labels={'A':'on','B':'off'} if index%2==0 else {'A':'off','B':'on'}
        for selected,names in CASES.items():
            folder=root/f'round-{index}'/selected
            report=json.loads((folder/'paired.json').read_text())
            by_name={x['name']:x for x in report['rows']}
            for name in names:
                raw=folder/'raw'/name;data=json.loads((raw/'identities.json').read_text())
                assert (raw/'complete.txt').read_text()=='COMPLETE\n'
                assert digest(raw/'source.py')==data['source_sha256']==identity['inputs'][name]['sha256']
                assert Path(data['source']).as_posix().endswith('/'+identity['inputs'][name]['path'])
                oracle=((raw/'oracle.stdout').read_bytes(),(raw/'oracle.stderr').read_bytes())
                tags=['oracle']+[f'sample-{n}-{a}' for n in range(13) for a in ('CP','A','B')]+['stats-A','stats-B','moved-A','moved-B']
                actual={p.stem for p in raw.glob('*.json')} - {'identities','A-build-command','B-build-command'}
                assert actual==set(tags), '原始双流条目缺失或多余'
                for tag in tags:
                    item=json.loads((raw/(tag+'.json')).read_text())
                    assert item['exit']==0 and tuple((raw/(tag+'.'+s)).read_bytes() for s in ('stdout','stderr'))==oracle
                    if tag.startswith('sample-'):
                        _,n,a=tag.split('-')
                        if int(n):
                            sample=by_name[name]['samples'][a][int(n)-1]
                            assert item['ms']==sample['ms'] and item['rss']==sample['rss']
                    if tag.startswith('stats-'):assert item['stats']==by_name[name]['stats'][tag[-1]]
                for arm,label in labels.items():
                    d=data['arms'][arm];assert d['cli']==identity['cli'][label]['path'] and d['cli_sha256']==identity['cli'][label]['sha256']
                    assert len(d['executable_sha256'])==64 and all(c in '0123456789abcdef' for c in d['executable_sha256'])
                    command=json.loads((raw/(arm+'-build-command.json')).read_text())['command']
                    assert command[:3]==[d['cli'],'build',data['source']] and command[3]=='-o' and command[5:]==['--aot','--native']
                    for n in range(13):
                        assert json.loads((raw/f'sample-{n}-{arm}.json').read_text())['command']==[d['executable']]
                    assert json.loads((raw/f'stats-{arm}.json').read_text())['command']==[d['executable']]
                    moved=json.loads((raw/f'moved-{arm}.json').read_text())
                    assert Path(moved['command'][0]).name==Path(d['executable']).name
                    assert Path(moved['command'][0]).parent==Path(moved['cwd'])
                expected_cp=[report['python'],'-B',data['source']]
                assert json.loads((raw/'oracle.json').read_text())['command']==expected_cp
                for n in range(13):
                    assert json.loads((raw/f'sample-{n}-CP.json').read_text())['command']==expected_cp
                total+=len(tags)
    return total


def main():
    p=argparse.ArgumentParser()
    for name in ('source','output','old','new','python'):p.add_argument('--'+name,type=Path,required=True)
    args=p.parse_args();args.raw_evidence=True
    args.output=args.output.resolve()
    assert not args.output.is_relative_to(balanced.ROOT)
    identity=freeze(args)
    # 初始身份先保全，失败不丢失；paired子目录只由本次运行创建。
    args.output.mkdir(parents=True,exist_ok=False)
    (args.output/'identity.json').write_text(json.dumps(identity,indent=2))
    outer=args.output;args.output=outer/'paired'
    balanced.EXPECTED=CASES
    balanced.run(args)
    assert freeze(args)==identity, '完整实验期间源码或CLI身份变化'
    total=verify_raw(args.output,identity)
    (outer/'complete.json').write_text(json.dumps({'complete':True,'raw_results':total,
        'scope':'4 builds x 12 samples; includes startup/IO; shared runner; no automatic significance or no-regression claim'}))


if __name__=='__main__':main()
