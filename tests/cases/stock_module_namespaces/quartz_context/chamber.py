# ryupy: pending
# Python A→CIL B→原生CAPI后检查A真实字典恢复，并覆盖异常原对象与再次调用。
import builtins
import sys
import mineral

foreign = builtins.__dict__.copy()
foreign.clear()
foreign_builtins = builtins.__dict__.copy()
foreign['__builtins__'] = foreign_builtins
foreign['sys'] = sys
foreign['dictionary'] = foreign
foreign['captured'] = foreign_builtins
builtins.exec("""
def apply(callback, value):
    before = (sys._getframe().f_globals is dictionary,
              sys._getframe().f_builtins is captured)
    result = callback(value)
    return before, result, (sys._getframe().f_globals is dictionary,
                            sys._getframe().f_builtins is captured)
""", foreign)
apply = foreign['apply']

def observe():
    return [apply(reader, -5) for reader in [mineral.old, mineral.fresh, mineral.retained, mineral.from_literal]]

failure = RuntimeError('quartz-context-failure')

def fail(value):
    assert sys._getframe().f_globals is sys.modules[__name__].__dict__
    raise failure

def recover():
    try:
        apply(fail, 1)
    except RuntimeError as caught:
        return caught is failure, apply(mineral.fresh, -2)

def reenter():
    return mineral.bridge(lambda: mineral.old(-3)), mineral.bridge(lambda: apply(mineral.fresh, -4))
