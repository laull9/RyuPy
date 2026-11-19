# last 先真值再读取当前容器；绑定错误必须早于回调，异常保原身份。
from collections import OrderedDict as OD

class Truth:
    def __init__(self, target, action, answer):
        self.target = target
        self.action = action
        self.answer = answer
    def __bool__(self):
        print('truth', self.action, list(self.target))
        if self.action == 'insert':
            self.target['new'] = 9
        elif self.action == 'remove':
            del self.target['a']
        elif self.action == 'clear':
            self.target.clear()
        elif self.action == 'rotate':
            self.target.move_to_end('a')
        print('truth_after', list(self.target))
        return self.answer

for action in ('insert', 'clear', 'rotate'):
    d = OD(a=1, b=2)
    try:
        print('pop_callback', action, d.popitem(Truth(d, action, True)), list(d))
    except KeyError as exc:
        print('pop_callback', action, type(exc).__name__, str(exc), list(d))
d = OD()
print('pop_empty_insert', d.popitem(Truth(d, 'insert', False)), list(d))
d = OD(a=1, b=2)
try:
    d.move_to_end('a', Truth(d, 'remove', True))
except KeyError as exc:
    print('move_removed', str(exc), list(d))
d = OD(a=1)
print('move_added', d.move_to_end('new', Truth(d, 'insert', False)), list(d))
error = RuntimeError('truth failure')
class BadTruth:
    def __bool__(self):
        print('bad_truth')
        raise error

for label, fn in (
    ('move_empty', lambda: OD().move_to_end('x', BadTruth())),
    ('move_unhashable', lambda: OD(a=1).move_to_end([], BadTruth())),
    ('pop_empty', lambda: OD().popitem(BadTruth())),
    ('invalid_keyword', lambda: OD(a=1).move_to_end('a', BadTruth(), bad=1)),
):
    try:
        fn()
    except Exception as exc:
        print(label, exc is error, type(exc).__name__, str(exc), exc.__cause__, exc.__context__)
class LenTruth:
    def __len__(self):
        print('last_len')
        return 0
d = OD(a=1, b=2)
print('len_truth', d.popitem(LenTruth()), list(d))
print('DONE last_truth_callbacks')
