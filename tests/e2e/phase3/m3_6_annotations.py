# 注解在定义位置求值，晚于默认值，闭包与类命名空间读取保留真实对象。
from typing import Optional, List, Union, get_args, get_origin

log = []
def mark(name, value):
    log.append(name)
    return value

def annotated(value: mark('arg', Optional[int]) = mark('default', None)) -> mark('return', List[int]):
    return value

print(log)
print(annotated.__annotations__['value'] == Optional[int])
print(get_args(Optional[int]), get_origin(List[int]) is list)

def outer(annotation):
    def inner(value: annotation) -> annotation:
        return value
    return inner

print(outer(str).__annotations__ == {'value': str, 'return': str})
Alias = Optional[int]
def alias(value: Alias):
    return value
Alias = str
print(alias.__annotations__['value'] == Optional[int])

class Annotated:
    alias = str
    def method(self, value: alias) -> alias:
        return value

print(Annotated.method.__annotations__ == {'value': str, 'return': str})

def generator():
    def nested(value: (yield 'annotation')):
        return value
    yield nested

it = generator()
print(next(it))
fn = it.send(str)
print(fn.__annotations__['value'] is str)

class ClassValues:
    first: mark('first-annotation', str) = mark('first-value', 'a')
    second: mark('second-annotation', int)

print(log[-3:])
print(ClassValues.__annotations__ == {'first': str, 'second': int})
