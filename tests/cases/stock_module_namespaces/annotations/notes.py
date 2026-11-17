# ryupy: pending
# SETUP_ANNOTATIONS在首条语句前执行；所有写入读取当前权威字典，普通局部注解不求值。
import sys
import builtins

events = []
startup = sorted(__annotations__)
startup_exact = type(__annotations__) is dict
module = sys.modules[__name__]
sentinel = object()

def mark(label):
    events.append(('annotation', label))
    return sentinel

def value(label, result):
    events.append(('value', label))
    return result

first: mark('first') = value('rhs-first', 3)
empty: mark('empty')
if False:
    hidden: int

# 无显式基类不读取可变builtins.object，也不额外执行import hook。
original_object = builtins.object
original_import = builtins.__import__
hidden_imports = []

def observe_import(*args, **kwargs):
    hidden_imports.append(args[0])
    return original_import(*args, **kwargs)

try:
    builtins.object = int
    builtins.__import__ = observe_import
    class Plain:
        pass
    implicit_original = Plain.__base__ is original_object
finally:
    builtins.__import__ = original_import
    builtins.object = original_object

class Target:
    pass

target = Target()
(target.attribute): mark('attribute') = value('rhs-attribute', 5)
(target.other): mark('attribute-empty')
mapping = {}
mapping[value('index', 'key')]: mark('subscript') = value('rhs-subscript', 8)
mapping[value('unbound-index', 'ignored')]: mark('subscript-empty')
(parenthesized): mark('parenthesized') = value('rhs-parenthesized', 9)

failure = RuntimeError('annotation-failure')

def fail_annotation():
    events.append('failing-annotation')
    raise failure

try:
    committed: fail_annotation() = value('rhs-before-failure', 11)
except RuntimeError as caught:
    failed_same = caught is failure

initial_keys = sorted(__annotations__)
initial_identity = __annotations__['first'] is sentinel

class Recorder:
    def __setitem__(self, key, annotation):
        events.append(('store', key, annotation is sentinel))

__annotations__ = Recorder()
late: mark('late') = value('rhs-late', 12)

def switch_annotations():
    global __annotations__
    events.append('switch-annotations')
    __annotations__ = Recorder()
    return sentinel

switched: switch_annotations() = value('rhs-switched', 14)

def local():
    local_only: mark('must-not-run') = 13
    absent: mark('must-not-run')
    try:
        return absent
    except UnboundLocalError:
        return local_only

local_result = local()
same_globals = mark.__globals__ is module.__dict__
