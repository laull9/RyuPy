# ryupy: pending
# 文本变换与可调用值管线：先捕获callee，再依源序计算实参；嵌套调用仍进静态core。
import operator
import builtins

events = []
record = events.append
preprocess = str.strip
finish = str.upper
replace = globals().__setitem__
target = "old:{}:{}".format
alternate = "new:{}:{}".format

def transform(x):
    value = preprocess(x)
    record(value)
    return finish(value)

def invoke(x):
    return x()

def many(x):
    return x("a", "b", "c", "d")

def select(x):
    record("callee")
    return target

def first(x):
    replace("target", alternate)
    record("first")
    return x

def second(x):
    record("second")
    return x

def ordered(x):
    return select(x)(first(x), second(x))

def fetch(x):
    return extractor(x)

def echo(x):
    return x

def nested(x):
    return echo(x)

def environment(x):
    return globals()

def explicit_compile(x):
    return x("1 + 2", "<probe>", "eval", 0, True)

def explicit_eval(x):
    return x("value + 1", scope)

def explicit_exec(x):
    x("result = value + 2", scope)
    return None

def explicit_vars(x):
    return inspector(x)

print(transform("  hello 雪  "))
print(events)
print(invoke("mixed".upper))
print(many("{}|{}|{}|{}".format))
events.clear()
print(ordered("A"), ordered("B"))
print(events)
marker = object()
extractor = operator.itemgetter(0)
print(fetch([marker]) is marker, nested(marker) is marker)
print(environment(None) is globals())
print(invoke(getattr(builtins, "globals")) is globals())
try:
    invoke(getattr(builtins, "super"))
except RuntimeError as error:
    print(type(error).__name__, error.args, str(error))
scope = getattr(builtins, "dict")(value=41)
print(type(explicit_compile(getattr(builtins, "compile"))).__name__)
print(explicit_eval(getattr(builtins, "eval")))
print(explicit_exec(getattr(builtins, "exec")) is None, scope["result"])
inspector = getattr(builtins, "vars")
Plain = type("Plain", (), {})
instance = Plain()
instance.value = 9
print(explicit_vars(instance)["value"])
extractor = operator.itemgetter(1)
try:
    fetch([marker])
except IndexError as error:
    print(type(error).__name__, error.args, str(error))
try:
    invoke(123)
except TypeError as error:
    print(type(error).__name__, error.args, str(error))
