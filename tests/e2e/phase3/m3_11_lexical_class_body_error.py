# ryupy: pending
# CPerror：普通表达式调用抛错，外部binding没有机会被新类覆盖；固定exit1。
def fail():
    raise ValueError('lexical class body failure')

def make():
    class C:
        value = fail()
    return C

make()
