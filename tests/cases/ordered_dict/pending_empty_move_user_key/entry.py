from collections import OrderedDict

events = []

class Key:
    def __hash__(self):
        events.append('hash')
        return 7
    def __repr__(self):
        events.append('repr')
        return 'user-key'

class RaisingRepr:
    def __hash__(self):
        events.append('hash-raising')
        return 9
    def __repr__(self):
        events.append('repr-raising')
        raise RuntimeError('repr must not run before catch')

class Last:
    def __init__(self, target, clear):
        self.target = target
        self.clear = clear
    def __bool__(self):
        events.append('truth')
        if self.clear:
            self.target.clear()
        return True

def check(label, key, clear=False):
    events.clear()
    target = OrderedDict()
    if clear:
        target['seed'] = 1
    try:
        target.move_to_end(key, Last(target, clear))
        print(label, 'unexpected success', len(target), events)
    except Exception as exc:
        # Do not format the exception or key: observe callbacks before the catch.
        print(label, type(exc).__name__, exc.args[0] is key, len(target), events)

check('scalar', 'missing')
check('empty-list', [])
check('user-key', Key())
check('list-user-key', [Key()])
check('tuple-key', ('x',))
check('bytes-key', b'x')
check('raising-repr', RaisingRepr())
check('truth-clears-user-key', Key(), True)
