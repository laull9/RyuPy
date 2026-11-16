# ryupy: pending
# AttributeError只跳过最后登记，不回滚已建立的真实桶；dict替换可自然保KeyError。
import typing
from typing import overload, get_overloads, clear_overloads
from collections import defaultdict
from functools import partial

clear_overloads()
registry = typing._overload_registry
print(type(registry) is defaultdict, type(registry.default_factory) is partial)
print(registry.default_factory.func is defaultdict, registry.default_factory.args == (dict,))
print(len(registry))
overload(None)
print(len(registry))
overload(len)
print(list(registry), type(registry["builtins"]) is defaultdict)
print(registry["builtins"].default_factory is dict, list(registry["builtins"]), registry["builtins"]["len"])
print(get_overloads(len))
def target():
    pass

replacement = {"__main__": {"target": {}}}
typing._overload_registry = replacement
print(overload(target) is typing._overload_dummy)
print(get_overloads(target)[0] is target, len(replacement["__main__"]["target"]))
print(clear_overloads(), replacement)
try:
    overload(target)
except KeyError as error:
    print(type(error).__name__, str(error))
typing._overload_registry = registry
print(list(registry), clear_overloads(), registry)
