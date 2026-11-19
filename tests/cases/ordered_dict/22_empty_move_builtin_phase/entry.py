from collections import OrderedDict

events = []

class Last:
    def __init__(self, target, mode):
        self.target = target
        self.mode = mode
    def __bool__(self):
        events.append('truth')
        if self.mode == 'clear':
            self.target.clear()
        if self.mode == 'fill':
            self.target['present'] = 1
        return True

def check(label, key, mode='empty'):
    events.clear()
    target = OrderedDict()
    if mode == 'clear' or mode == 'populated':
        target['seed'] = 1
    try:
        target.move_to_end(key, last=Last(target, mode))
        print(label, 'unexpected success')
    except KeyError as exc:
        same = exc.args[0] is key if len(exc.args) else None
        print(label, 'KeyError', len(exc.args), same, len(target), events)
    except TypeError as exc:
        print(label, 'TypeError', str(exc), len(target), events)

check('str', 'missing')
check('none', None)
check('nonempty-missing-none', None, 'populated')
check('int', 8)
check('list', [])
check('dict', {})
check('set', set())
check('bytearray', bytearray())
check('truth-clear-list', [], 'clear')
check('truth-fill-list', [], 'fill')
