# ryupy: pending
# 自定义分配、copy/deepcopy及asdict算法另列，不由默认布局合同外推。
import copy
from dataclasses import dataclass, field, asdict
class D(dict):
    pass
class Foreign(dict):
    def __new__(cls, *args, **kwargs):
        return {}
    def __init__(self, *args, **kwargs):
        raise AssertionError('foreign init must be skipped')
assert type(Foreign(1, 2)) is dict
x = Foreign.fromkeys(['a'], 3)
assert type(x) is dict and x == {'a': 3}
d = D(a=[1])
c = copy.copy(d)
e = copy.deepcopy(d)
assert type(c) is D and c['a'] is d['a']
assert type(e) is D and e['a'] is not d['a']
@dataclass
class Job:
    data: dict = field(default_factory=D)
j = Job(d)
r = asdict(j)
assert type(r['data']) is D and r['data']['a'] is not d['a']
print('pending.custom-new-copy', x, c, e, r)
