# 无globals反射的真实模块名规范化及构造时序；事件与输出由入口观察。
import warnings

events = []

class NameObject:
    def __str__(self):
        events.append('name-str-forbidden')
        raise RuntimeError('name-str')
    def __bool__(self):
        events.append('name-bool-forbidden')
        raise RuntimeError('name-bool')
    def __eq__(self, other):
        events.append('name-eq-forbidden')
        raise RuntimeError('name-eq')

class NameString(str):
    def __str__(self):
        events.append('str-subclass-str-forbidden')
        raise RuntimeError('str-subclass-str')

class Constructed(DeprecationWarning):
    def __init__(self, message):
        global __name__
        events.append('constructor')
        __name__ = 'hidden-after-constructor'

class Stringified(DeprecationWarning):
    def __str__(self):
        global __name__
        events.append('warning-str')
        __name__ = 'hidden-after-str'
        return 'existing-warning'

def issue(message, category):
    warnings.warn(message, category)
