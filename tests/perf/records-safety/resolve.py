"""核Cargo实际解析，而非根据命令字符串宣称feature覆盖了e2e。"""
import json,sys
m=json.load(open(sys.argv[1])); enabled=sys.argv[2]=='feature'
packages={p['id']:p for p in m['packages']}
nodes={n['id']:n for n in m['resolve']['nodes']}
by_name={p['name']:nodes[p['id']] for p in m['packages'] if p['id'] in nodes}
for name in ['ryupy-driver','ryupy-mir']:
    assert ('records-increment-experiment' in by_name[name]['features'])==enabled,(name,by_name[name]['features'])
    assert 'records-increment-diagnostics' not in by_name[name]['features']
e2e=by_name['ryupy-e2e']
assert any(packages[d['pkg']]['name']=='ryupy-driver' for d in e2e['deps'])
assert any(packages[d['pkg']]['name']=='ryupy-mir' for d in by_name['ryupy-driver']['deps'])
print(json.dumps({'mode':sys.argv[2],'driver':by_name['ryupy-driver']['features'],'mir':by_name['ryupy-mir']['features'],'e2e_driver_edge':True}))
