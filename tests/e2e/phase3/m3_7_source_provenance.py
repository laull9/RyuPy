# ryupy: pending
# 真实定义元数据、活全局环境和模块入口快照是三种不同的值；不得按文件名猜归属。
import sys
import warnings
from functools import partial
from typing import TypedDict

entry_name = __name__


def make_record():
    return TypedDict("Record", {"value": int})


alias = make_record
make_record.__module__ = "reflection-owner"
__name__ = "renamed-environment"
print(alias().__module__, TypedDict("AtEntry", {"value": int}).__module__ == entry_name)
for raw in (None, "", 7, ["module"]):
    alias.__module__ = raw
    record = alias()
    print(record.__module__ is raw if raw else record.__module__ == "__main__")
__name__ = entry_name


class NativeFailure:
    def __bool__(self):
        return bool(int("not-an-integer"))


alias.__module__ = NativeFailure()
try:
    alias()
except ValueError:
    print("native truthiness failure")
# 原生抛错传播后不能残留__bool__帧；后续调用仍选实际Dynamic函数。
alias.__module__ = "after-native-failure"
print(alias().__module__)


def dynamic_issue():
    warnings.warn("mixed")


class Sink:
    def write(self, text):
        return len(text)


def static_issue() -> None:
    print("payload", flush=True)


# 纯 native 的 partial→warn 不新增 Python 帧，消费者应看见 static_issue 的真实 Static 帧。
sink = Sink()
sink.flush = partial(warnings.warn, "mixed")
saved_stdout = sys.stdout
for action in ("module", "once"):
    for peer in ("main", "dynamic"):
        for static_first in (False, True):
            with warnings.catch_warnings(record=True) as captured:
                warnings.simplefilter(action)
                for first in (True, False):
                    if first == static_first:
                        sys.stdout = sink
                        try:
                            static_issue()
                        finally:
                            sys.stdout = saved_stdout
                    elif peer == "main":
                        warnings.warn("mixed")
                    else:
                        dynamic_issue()
                print(action, peer, static_first, len(captured))

# None 抑制在消息构造/str之前，但不能跳过真实类别校验。
events = []


class Noisy(UserWarning):
    def __init__(self, message):
        events.append("init")
    def __str__(self):
        events.append("str")
        return "noise"


existing = Noisy("existing")
events.clear()
__name__ = None
with warnings.catch_warnings(record=True) as captured:
    warnings.simplefilter("always")
    warnings.warn("new", Noisy)
    warnings.warn(existing)
    try:
        warnings.warn("invalid", int)
    except TypeError:
        events.append("category")
    print(len(captured), events)
__name__ = entry_name
