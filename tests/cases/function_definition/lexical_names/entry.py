# 函数定义来源与限定名语义验收。
# 公开函数限定名贯穿普通函数、lambda、闭包、方法和生成器；调用结果保持真实。
def plain(value):
    return value + 1

factory = lambda base: (lambda value: base + value)
inner_lambda = factory(10)

def make_transform(base):
    def transform(value):
        return base * value
    return transform

transform = make_transform(3)

class Container:
    __qualname__ = "RenamedClass"

    def method(self, value):
        return value + 2

    def factory(self, base):
        def inner(value):
            return base - value
        return inner

    def stream(self, value):
        yield value
        yield value + 1

def stream(value):
    yield value
    yield value * 2

def make_stream(base):
    def nested_stream():
        yield base
        yield base + 1
    return nested_stream

container = Container()
method_inner = container.factory(20)
nested_stream = make_stream(7)
names = [plain.__qualname__, factory.__qualname__, inner_lambda.__qualname__,
         transform.__qualname__, Container.method.__qualname__,
         method_inner.__qualname__, stream.__qualname__,
         Container.stream.__qualname__, nested_stream.__qualname__]
assert names == ["plain", "<lambda>", "<lambda>.<locals>.<lambda>",
                 "make_transform.<locals>.transform", "Container.method",
                 "Container.factory.<locals>.inner", "stream",
                 "Container.stream", "make_stream.<locals>.nested_stream"]
assert Container.__qualname__ == "RenamedClass"
assert plain(4) == 5 and inner_lambda(2) == 12 and transform(4) == 12
assert container.method(4) == 6 and method_inner(3) == 17
assert list(stream(4)) == [4, 8]
assert list(container.stream(4)) == [4, 5]
assert list(nested_stream()) == [7, 8]
for name in names:
    print(name)
print(Container.__qualname__, plain(4), inner_lambda(2), transform(4))
print(list(stream(4)), list(container.stream(4)), list(nested_stream()))
