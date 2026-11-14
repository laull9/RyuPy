"""显式synchronized：精确容器双向写回、共享环、回调与异常路径。"""
import bridge_fixture
import json

value = {'items': []}


def compiled_mutates(argument):
    assert argument is value
    assert argument['items'] == ['Python前']
    argument['items'].append('编译回调')


assert bridge_fixture.mutate_and_callback(value, compiled_mutates) is value
assert value['items'] == ['Python前', '编译回调', 'Python后']
assert json.loads(json.dumps(value)) == value
try:
    bridge_fixture.mutate_then_fail(value)
except ValueError as error:
    assert str(error) == '写回异常'
else:
    raise AssertionError('写回路径异常未传播')
assert value['items'][-1] == '异常前写入'
shared = []
cycle = {'left': shared, 'right': shared}
shared.append(cycle)
assert bridge_fixture.verify_shared_cycle(cycle) is cycle
assert cycle['left'] is cycle['right']
assert shared[0] is cycle
assert shared[1] == '环内Python写入'
shared.clear()
cycle.clear()
print('精确容器同步、共享环、回调与异常写回通过')
