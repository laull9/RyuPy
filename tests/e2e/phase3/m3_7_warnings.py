# warnings必须具有真实过滤、异常、捕获与位置行为，不能只提供可导入模块。
import warnings


def issue(text):
    warnings.warn(text)


with warnings.catch_warnings(record=True) as captured:
    warnings.simplefilter("default")
    issue("same")
    issue("same")
    print(len(captured), str(captured[0].message), captured[0].category is UserWarning)
    warnings.simplefilter("always")
    issue("same")
    issue("same")
    print(len(captured))
    warnings.simplefilter("ignore")
    issue("hidden")
    print(len(captured))
    warnings.simplefilter("error", RuntimeWarning)
    original = RuntimeWarning("failure")
    try:
        warnings.warn(original)
    except Warning as error:
        print(error is original, isinstance(error, RuntimeWarning))

with warnings.catch_warnings(record=True) as outer:
    warnings.simplefilter("always")
    with warnings.catch_warnings(record=True) as inner:
        issue("inner")
    issue("outer")
    print(len(inner), len(outer), str(inner[0].message), str(outer[0].message))

with warnings.catch_warnings(record=True) as captured:
    warnings.warn("main", DeprecationWarning)
    print(len(captured), captured[0].category is DeprecationWarning)

factory = UserWarning
instance = factory("raw", 3)
print(instance.args)
with warnings.catch_warnings(record=True) as captured:
    warnings.simplefilter("always")
    warnings.warn(instance, int)
    print(captured[0].message is instance)
    warnings.warn(3)
    print(captured[1].message.args)


def wrapper():
    warnings.warn("caller", stacklevel=2)


with warnings.catch_warnings(record=True) as captured:
    warnings.simplefilter("always")
    wrapper()
    print(captured[0].filename.endswith("m3_7_warnings.py"), captured[0].lineno > 0)
    warnings.warn("outside", stacklevel=1000000)
    print(captured[1].filename, captured[1].lineno)

manager = warnings.catch_warnings(record=True, action="always")
print(type(manager) is warnings.catch_warnings, isinstance(manager, warnings.catch_warnings))
with manager as captured:
    issue("action")
    print(len(captured))
try:
    with manager:
        pass
except RuntimeError:
    print("single entry")

# 非Warning消息用于过滤时不调用警告子类的str；已有实例才调用。
events = []


class CustomWarning(UserWarning):
    def __str__(self):
        events.append("warning str")
        return "rendered"


with warnings.catch_warnings(record=True) as captured:
    warnings.simplefilter("always")
    warnings.warn("plain", CustomWarning)
    print(events)
    custom = CustomWarning("instance")
    warnings.warn(custom)
    print(events, captured[1].message is custom)
    warnings.simplefilter("ignore")
    warnings.warn("hidden", CustomWarning)
    print(events)

with warnings.catch_warnings(record=True) as captured:
    warnings.simplefilter("default")
    for value in [3, 3.0]:
        warnings.warn(value)
    print(len(captured), captured[0].message.args)
