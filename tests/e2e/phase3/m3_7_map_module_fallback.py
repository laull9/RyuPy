# 模块兼容分支未执行时，真实map内建仍可从函数/生成器表达式读取；绑定优先，删除后回退。
if False:
    from itertools import imap as map

def mapped():
    return list(map(len, ['x', 'abc']))

print(mapped())
print([list(row) for row in (map(len, block) for block in [['a', 'bbb'], ['cc']])])
original = map
print(type(map(str, [])) is original)
map = lambda function, items: ['shadow']
print(mapped())
map = None
try:
    mapped()
except TypeError:
    print('none is bound')
del map
print(mapped(), map is original)
try:
    del map
except NameError:
    print('cannot delete fallback')

# 本次callee必须先于参数副作用冻结，不可在实参求值后重新读取module槽。
def replace_argument():
    global map
    map = lambda function, items: ['after argument']
    return ['q']
print(list(map(str, replace_argument())))
print(mapped())
del map
print(mapped())
