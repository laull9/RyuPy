# 真实辅助模块；默认module必须来自当前执行函数，不是入口文件或调用者。
from collections import namedtuple

ModuleRecord = namedtuple('ModuleRecord', 'value')


def make_record(value):
    return namedtuple('ImportedRecord', 'value')(value)


def make_none(value):
    return namedtuple('ImportedNone', 'value', module=None)(value)


def set_name(value):
    global __name__
    __name__ = value


def make_closure(seed):
    def closure(value):
        return namedtuple('NestedRecord', 'value')(seed + value)
    return closure


def make_stream(seed):
    def stream():
        yield namedtuple('YieldRecord', 'value')(seed)
        yield namedtuple('YieldRecord', 'value', module=None)(seed + 1)
    return stream


def invoke_callback(callback, value):
    return callback(value)
