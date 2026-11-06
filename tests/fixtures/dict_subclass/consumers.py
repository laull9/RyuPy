
# 配置记录跨调用展开、JSON与dataclass；属性不得混入内容，循环按公开owner识别。
import json
from dataclasses import dataclass, field
class Config(dict):
    pass

settings = Config(host='local', port=9000)
settings.secret = 'attribute-only'
def connect(host, port):
    return host + ':' + str(port)
assert connect(**settings) == 'local:9000'
assert json.dumps(settings) == '{"host": "local", "port": 9000}'
assert dict(settings) == {'host': 'local', 'port': 9000}

print('dict-subclass.mapping-consumers', connect(**settings), json.dumps(settings))

class Public(Config):
    def items(self):
        return [('public', 2)]
public = Public(private=1)
assert dict(public) == {'private': 1}
assert json.dumps(public) == '{"public": 2}'
cycle = Config()
cycle['owner'] = cycle
assert repr(cycle) == "{'owner': {...}}"
try:
    json.dumps(cycle)
except ValueError as error:
    assert str(error) == 'Circular reference detected'
else:
    raise AssertionError('cycle not detected')
print('dict-subclass.json-policy-cycle', json.dumps(public), repr(cycle))

try:
    @dataclass
    class Bad:
        config: dict = Config()
except ValueError as error:
    assert 'mutable default' in str(error)
else:
    raise AssertionError('mutable subclass default accepted')
@dataclass
class Job:
    config: dict = field(default_factory=Config)
a = Job()
b = Job()
a.config['job'] = 1
assert type(a.config) is Config and type(b.config) is Config and b.config == {}
print('dict-subclass.dataclass-default', a.config, b.config)
