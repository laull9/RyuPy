
class Mapping:
    def keys(self):
        print('keys')
        return ['first', 'second']
    def __getitem__(self, key):
        print('getitem', key)
        if key == 'second':
            raise ValueError('mapping-failed')
        return 1
    def __iter__(self):
        print('wrong-iter')
        return iter([('wrong', 9)])
d = {'old': 0}
try:
    d.update(Mapping())
except ValueError as e:
    print('caught', str(e))
print('after', d)
