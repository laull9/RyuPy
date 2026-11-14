# 函数定义来源与限定名语义验收。
# 独立正例：定义位置表达式中的lambda属于外层，急切/惰性推导式遵守CP3.12路径。
seen = []
def retain(callback):
    seen.append(callback)
    return lambda function: function

class Scope:
    @(retain(lambda: 'decorator'))
    def method(self, value: (lambda: 'annotation') = (lambda: 'default')) -> (lambda: 'return'):
        return value

assert seen[0].__qualname__ == 'Scope.<lambda>'
assert Scope.method.__qualname__ == 'Scope.method'
assert Scope().method().__qualname__ == 'Scope.<lambda>'
assert Scope.method.__annotations__['value'].__qualname__ == 'Scope.<lambda>'
assert Scope.method.__annotations__['return'].__qualname__ == 'Scope.<lambda>'
assert seen[0]() == 'decorator'
assert Scope().method()() == 'default'
assert Scope.method.__annotations__['value']() == 'annotation'
assert Scope.method.__annotations__['return']() == 'return'

def outer():
    eager = [lambda: 'list' for value in [0]]
    mapping = {(lambda: 'key'): (lambda: 'value') for value in [0]}
    unique = {lambda: 'set' for value in [0]}
    lazy = (lambda: 'generator' for value in [0])
    nested = ([lambda: 'nested' for value in [0]] for unused in [0])
    left_iterable = (value for value in [lambda: 'outer iterable'])
    later_iterable = (value for unused in [0] for value in [lambda: 'inner iterable'])
    return eager[0], list(mapping)[0], list(mapping.values())[0], list(unique)[0], next(lazy), next(nested)[0], next(left_iterable), next(later_iterable)

functions = outer()
expected = ['outer.<locals>.<lambda>'] * 4 + ['outer.<locals>.<genexpr>.<lambda>'] * 2 + ['outer.<locals>.<lambda>', 'outer.<locals>.<genexpr>.<lambda>']
assert [function.__qualname__ for function in functions] == expected
assert [function() for function in functions] == ['list', 'key', 'value', 'set', 'generator', 'nested', 'outer iterable', 'inner iterable']
left, right = (lambda: 1), (lambda: 2)
assert left is not right and left.__qualname__ == right.__qualname__ == '<lambda>'
assert left() == 1 and right() == 2
for name in [seen[0].__qualname__, Scope.method.__qualname__, Scope().method().__qualname__] + expected:
    print(name)
print([function() for function in functions])
