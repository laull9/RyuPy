# ryupy: pending
# 独立完整主源：真实defaultdict/partial载体、命中时不消费factory，以及exact dict替换。
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
def target():
    return "body"

print(overload(target) is typing._overload_dummy)
print(list(registry), type(registry["__main__"]) is defaultdict)
module = registry["__main__"]
print(module.default_factory is dict, list(module), type(module["target"]) is dict)
print(len(module["target"]), list(module["target"].values())[0] is target)
top_factory = registry.default_factory
registry.default_factory = None
module.default_factory = None
print(overload(target) is typing._overload_dummy, get_overloads(target)[0] is target)
registry.default_factory = top_factory
module.default_factory = dict
snapshot = get_overloads(target)
print(clear_overloads(), registry, snapshot[0]())

replacement = {"__main__": {"target": {}}}
typing._overload_registry = replacement
print(overload(target) is typing._overload_dummy)
print(get_overloads(target)[0] is target, len(replacement["__main__"]["target"]))
print(clear_overloads(), replacement)
try:
    overload(target)
except KeyError as error:
    print(type(error).__name__, str(error))
print(get_overloads(target), replacement)
typing._overload_registry = registry
print(clear_overloads(), registry)
