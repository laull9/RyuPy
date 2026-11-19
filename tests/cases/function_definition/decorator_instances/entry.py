# 函数定义来源与限定名语义验收。
# 装饰器先见真实初始限定名；重复同一def的新对象共享词法来源但公开成员互不污染。
observed = []
retained = []

def observe(function):
    observed.append((function.__name__, function.__qualname__))
    retained.append(function)
    return function

def build(value):
    @(
        observe
    )
    @observe
    def action(extra=1):
        return value + extra
    return action

first = build(10)
second = build(20)
assert first is not second
assert retained[0] is retained[1] is first
assert retained[2] is retained[3] is second
assert observed == [("action", "build.<locals>.action")] * 4
first.__qualname__ = "first.changed"
first.__module__ = "first-module"
assert second.__qualname__ == "build.<locals>.action"
assert second.__module__ == __name__
second.__qualname__ = "second.changed"
third = build(30)
assert third.__qualname__ == "build.<locals>.action"
assert third.__module__ == __name__
assert first.__qualname__ == "first.changed"
assert second.__qualname__ == "second.changed"

class Service:
    __qualname__ = "PublicAlias"

    @observe
    def run(self, value):
        return value * 3

def build_stream(value):
    @observe
    def stream():
        yield value
    return stream

stream_first = build_stream(4)
stream_second = build_stream(5)
stream_first.__qualname__ = "one.stream"
assert stream_second.__qualname__ == "build_stream.<locals>.stream"
assert observed[-3:] == [("run", "Service.run"),
                         ("stream", "build_stream.<locals>.stream"),
                         ("stream", "build_stream.<locals>.stream")]
assert Service.run.__qualname__ == "Service.run"
assert Service().run(6) == 18
assert [first(), second(), third()] == [11, 21, 31]
assert list(stream_first()) == [4] and list(stream_second()) == [5]
for item in observed:
    print(item)
print(first.__qualname__, second.__qualname__, third.__qualname__)
print(first(), second(), third(), Service().run(6))
print(stream_first.__qualname__, stream_second.__qualname__, list(stream_first()), list(stream_second()))
