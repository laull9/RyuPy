# ryupy: pending
# 字典字面量展开是既有HIR语法缺口，与默认真实布局分开验收。
class Config(dict):
    pass
shared = []
r = Config(host='local', port=9000, tags=shared)
c = {**r}
assert type(c) is dict and c is not r and c['tags'] is shared
assert {**r, 'host': 'remote'} == {'host': 'remote', 'port': 9000, 'tags': []}
print('pending.literal-spread', c, c['tags'] is shared)
