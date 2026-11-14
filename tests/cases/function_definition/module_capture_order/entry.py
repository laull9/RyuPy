# 函数定义来源与限定名语义验收。
# 默认值与标注求值完成后捕获module，装饰器应用前可见；后续全局改名不回溯。
events = []
original_module = __name__

def change_module(label, result):
    global __name__
    __name__ = label
    events.append(label)
    return result

def decorator_expression(label):
    change_module(label, None)

    def decorate(function):
        events.append((label, function.__module__, function.__qualname__))
        return function
    return decorate

@decorator_expression("decorator-outer")
@decorator_expression("decorator-inner")
def configured(value: change_module("annotation-value", int) = change_module("default", 3),
               *, extra=change_module("keyword-default", 4)) -> change_module("annotation-return", int):
    return value + extra

assert events == ["decorator-outer", "decorator-inner", "default", "keyword-default",
                  "annotation-value", "annotation-return",
                  ("decorator-inner", "annotation-return", "configured"),
                  ("decorator-outer", "annotation-return", "configured")]
assert configured.__module__ == "annotation-return"
assert configured.__qualname__ == "configured" and configured() == 7
__name__ = "after-definition"
assert configured.__module__ == "annotation-return"
configured.__module__ = "custom-module"
configured.__dict__["__module__"] = "dictionary-spoof"
assert configured.__module__ == "custom-module"
configured.__module__ = None
assert configured.__module__ is None
configured.__module__ = 42
assert configured.__module__ == 42
configured.__module__ = ["ordinary", "value"]
assert configured.__module__ == ["ordinary", "value"]
__name__ = original_module
for event in events:
    print(event)
print(configured.__qualname__, configured.__module__, configured())
