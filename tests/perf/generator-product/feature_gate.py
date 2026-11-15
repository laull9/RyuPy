"""检查Cargo真实resolve节点，避免feature只出现在命令字符串却未启用。"""
import json
from pathlib import Path
import sys


def verify(path, arm):
    data=json.loads(path.read_text());by_name={p['name']:p['id'] for p in data['packages']}
    nodes={n['id']:n for n in data['resolve']['nodes']}
    for name in ('ryupy-driver','ryupy-mir'):
        assert ('generator-sum-experiment' in nodes[by_name[name]]['features']) == (arm=='on'), name
    e2e=nodes[by_name['ryupy-e2e']]
    assert by_name['ryupy-driver'] in e2e['dependencies'], 'e2e必须真实链接已核driver'
    return arm


if __name__=='__main__':print(verify(Path(sys.argv[1]),sys.argv[2]))
