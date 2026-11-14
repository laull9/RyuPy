# 裸 return、显式 None 与落尾均保留 None 单例身份；静态 Unit/int 合同独立。
def bare():
    return

def explicit():
    return None

def fallthrough():
    pass

def unit() -> None:
    return

def integer() -> int:
    return 7

def annotated_bare() -> int:
    return

def mixed(flag: bool) -> int:
    if flag:
        return
    return 7

def finally_bare():
    try:
        return 7
    finally:
        return

def bare_through_finally(events):
    try:
        events.append('try')
        return
    finally:
        events.append('finally')
    events.append('unreachable')

class Methods:
    def method_bare(self, left='', right=''):
        return
    def method_explicit(self):
        return None
    def method_fallthrough(self):
        pass
    def overlap(self):
        amount = 0
        if '$' != '' and self.method_bare(left='|', right='$') is not None:
            amount += 1
        return amount

def report(label, result):
    print(label, type(result).__name__, result is None, result is not None, repr(result))

report('bare', bare())
report('explicit', explicit())
report('fallthrough', fallthrough())
report('unit', unit())
report('integer', integer())
report('annotated_bare', annotated_bare())
report('mixed-true', mixed(True))
report('mixed-false', mixed(False))
report('finally', finally_bare())
events = []
report('through-finally', bare_through_finally(events))
print('events', events)
methods = Methods()
report('method-bare', methods.method_bare(left='|', right='$'))
report('method-explicit', methods.method_explicit())
report('method-fallthrough', methods.method_fallthrough())
print('overlap', methods.overlap())
print('same-none', bare() is explicit(), methods.method_bare() is fallthrough())
