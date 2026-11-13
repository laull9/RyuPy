# ryupy: pending
"""默认活代理容器比较：双向、嵌套、实时别名修改、原始排序结果及循环边界。"""
import ffi_demo

native = [1, {'x': [2]}]
foreign = ffi_demo.nested_list()
assert native == foreign and foreign == native
assert ffi_demo.equal_pair(native, foreign)
assert ffi_demo.equal_pair(foreign, native)
native[1]['x'].append(3)
assert native != foreign and foreign != native

events = []
changing_list = [0, 99]


def shrink_list():
    events.append('list')
    changing_list.pop()


mutating_list = ffi_demo.mutating_list(shrink_list)
assert not (mutating_list == changing_list)
assert events == ['list']
assert changing_list == [0]

growing_list = [0]


def grow_list():
    growing_list.append(99)


assert not ffi_demo.equal_pair(growing_list, ffi_demo.mutating_single(grow_list))
assert growing_list == [0, 99]

changing_dict = {'a': 0, 'b': 2}


def change_value():
    events.append('dict')
    changing_dict['b'] = 3


mutating_dict = ffi_demo.mutating_dict(change_value)
assert not (mutating_dict == changing_dict)
assert changing_dict['b'] == 3
assert events == ['list', 'dict']
assert ffi_demo.equal_pair({'a': [1]}, ffi_demo.nested_dict())

changing_keys = {'a': 0, 'b': 2}


def replace_key():
    del changing_keys['b']
    changing_keys['c'] = 3


assert not ffi_demo.equal_pair(changing_keys, ffi_demo.mutating_dict(replace_key))
assert changing_keys == {'a': 0, 'c': 3}


def replace_python_key():
    del changing_python_keys['b']
    changing_python_keys['c'] = 3


changing_python_keys = ffi_demo.mutating_dict(replace_python_key)
assert not (changing_python_keys == {'a': 0, 'b': 2})
assert changing_python_keys['c'] == 3

ordering = ffi_demo.ordering_list()
assert (ordering < [0]) is ffi_demo.order_token()
assert ([0] < ordering) is ffi_demo.order_token()

cycle = []
cycle.append(cycle)
foreign_cycle = ffi_demo.cycle()
try:
    cycle == foreign_cycle
except RecursionError:
    pass
else:
    raise AssertionError('循环比较缺少有界递归错误')
print('活代理容器比较通过')
