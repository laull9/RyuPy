# captured get在key求值前捕获；复杂求值不应被实体键matcher改写。
class Receiver:
    def __init__(self):
        self.values = {}
    def get(self, key, default):
        print('old-get', key, default)
        return 8
    def __setitem__(self, key, value):
        self.values[key] = value
        print('set', key, value)

def alternate(key, default):
    print('new-get', key, default)
    return 100

r = Receiver()
calls = []
def key():
    calls.append(1)
    r.get = alternate
    return 'x'

r[key()] = r.get(key(), 0) + 1
print('captured', len(calls), r.values['x'])

class ThrowHash:
    def __hash__(self):
        print('hash-error')
        raise ValueError('hash failed')

old = {'seed': 5}
try:
    old[ThrowHash()] = old.get(ThrowHash(), 0) + 1
except ValueError as error:
    print('unchanged', str(error), old['seed'], len(old))
