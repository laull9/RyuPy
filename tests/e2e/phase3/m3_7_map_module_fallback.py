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

# 默认字典读取不能变成常量或缓存；已有模块值也不能被字典改绑覆盖。
import builtins
saved = builtins.map
try:
    builtins.map = lambda function, items: ['replacement']
    print(mapped())
    map = lambda function, items: ['module']
    print(mapped())
    del map
    print(mapped())
finally:
    builtins.map = saved
print(mapped())
