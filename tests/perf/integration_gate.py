"""组合候选门：真实feature解析、完整TRX与固定dev的精确11项ABI及归档身份。"""
import hashlib
import json
from pathlib import Path
import sys
import xml.etree.ElementTree as ET
sys.path.insert(0,str(Path(__file__).parent/'generator-product'))
import runtime_gate

def features(path,arm):
    data=json.loads(path.read_text()); ids={p['name']:p['id'] for p in data['packages']}
    nodes={n['id']:n for n in data['resolve']['nodes']}
    for name in ('ryupy-cli','ryupy-driver','ryupy-mir'):
        actual=nodes[ids[name]]['features']
        for feature in ('generator-sum-experiment','records-increment-experiment'):
            assert (feature in actual)==(arm=='on'),(name,feature,actual)
        assert 'records-increment-diagnostics' not in actual
    assert ids['ryupy-driver'] in nodes[ids['ryupy-e2e']]['dependencies']

def historical_artifact(folder):
    data=json.loads((folder/'pe/generator-frozen-dev-abi.json').read_text())
    assert data['commit']=='81758bb477729aee4636f2ca6d6b0815b949849a' and data['sdk']=='10.0.403'
    assert data['oldSignatures']>0 and data['candidateSignatures']==data['oldSignatures']+11
    assert len(data['additions'])==11 and len(set(data['additions']))==11
    assert data['removed']==[] and sorted(data['added'])==sorted(data['additions'])
    assert len(data['actual'])==data['candidateSignatures'] and sorted(data['expected'])==sorted(data['actual'])
    assert len(set(data['actual']))==len(data['actual'])
    assert len(data['sha256'])==len(data['candidateSha256'])==64
    # 按固定归档布局重定位，不能跟随原runner绝对路径去读取其他文件。
    frozen = folder.parent/'frozen-dev-abi'
    manifest = json.loads((frozen/'manifest.json').read_text())
    assert manifest['commit']==data['commit'] and manifest['sdk']==data['sdk']
    assert manifest['configuration']=='Release'
    assert data['baseline']==manifest['dll'] and data['sha256']==manifest['sha256']
    assert data['baselineBytes']==manifest['bytes']
    assert Path(data['baselineManifest']).parent.name=='frozen-dev-abi' and Path(data['baselineManifest']).name=='manifest.json'
    relative = Path('build/bin/RyuPy.Runtime/release/RyuPy.Runtime.dll')
    assert Path(manifest['dll']).parts[-len(relative.parts):]==relative.parts
    assert data['candidateArchive']=='generator-loaded-candidate.dll'
    baseline = (frozen/relative).read_bytes()
    candidate = (folder/'pe'/data['candidateArchive']).read_bytes()
    assert len(baseline)==data['baselineBytes'] and hashlib.sha256(baseline).hexdigest()==data['sha256']
    assert len(candidate)==data['candidateBytes'] and hashlib.sha256(candidate).hexdigest()==data['candidateSha256']
    assert Path(data['candidate']).is_absolute() and data['candidate']!=data['baseline']


def runtime(path):
    # 历史records专项另列跳过；固定dev的真实DLL门仍必须执行。
    runtime_gate.ALLOWED_SKIPS.add('RyuPy.Runtime.Tests.PyTraceStateArtifactTests.PinnedRecordsDevPublicAbiProvesNinePreexistingAdditions')
    runtime_gate.check(path,'full')
    runtime_gate.compiled(path)
    runtime_gate.historical(path)
    ns={'t':'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}
    rows=ET.parse(path).getroot().findall('t:Results/t:UnitTestResult',ns)
    name='RyuPy.Runtime.Tests.PyTraceStateArtifactTests.IntegratedIncrementRejectsMissingChangedDuplicateAndUnknownMembers'
    matched=[r for r in rows if r.get('testName')==name]
    assert len(matched)==1 and matched[0].get('outcome')=='Passed'
    historical_artifact(path.parent)

if __name__=='__main__':
    if sys.argv[1]=='features': features(Path(sys.argv[2]),sys.argv[3])
    else: runtime(Path(sys.argv[2]))
