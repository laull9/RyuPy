"""核实完整检查的真实依赖feature；不把CLI字符串当作e2e覆盖证据。"""
import json
import sys

metadata = json.load(open(sys.argv[1]))
enabled = sys.argv[2] == 'feature'
nodes = {node['id']: node for node in metadata['resolve']['nodes']}
packages = {package['id']: package for package in metadata['packages']}
by_name = {package['name']: nodes[package['id']] for package in packages.values() if package['id'] in nodes}
for name in ['ryupy-driver', 'ryupy-mir']:
    assert ('records-increment-experiment' in by_name[name]['features']) == enabled
    assert 'records-increment-diagnostics' not in by_name[name]['features']
assert any(packages[edge['pkg']]['name'] == 'ryupy-driver' for edge in by_name['ryupy-e2e']['deps'])
assert any(packages[edge['pkg']]['name'] == 'ryupy-mir' for edge in by_name['ryupy-driver']['deps'])
print(json.dumps({'arm': sys.argv[2], 'driver': by_name['ryupy-driver']['features'],
                  'mir': by_name['ryupy-mir']['features'], 'e2e_driver': True, 'driver_mir': True}))
