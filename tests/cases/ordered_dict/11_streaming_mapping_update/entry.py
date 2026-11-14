# mapping 按 keys 逐next/get/写；回调可改源keys与目标，不预先物化。
from collections import OrderedDict as OD

d = OD(seed=0)
class Keys:
    def __iter__(self):
        print('keys_iter', list(d.items()))
        yield 'a'
        print('keys_after_a', list(d.items()))
        assert d['a'] == 'A'
        yield 'b'
        print('keys_after_b', list(d.items()))
        assert d['b'] == 'B'
class Mapping:
    def keys(self):
        print('mapping_keys')
        return Keys()
    def __getitem__(self, key):
        print('mapping_get', key, list(d.items()))
        if key == 'a':
            d['callback'] = 5
        return key.upper()
    def __iter__(self):
        raise AssertionError('mapping 不应读 pairs')
print('update_return', d.update(Mapping(), tail='T'))
print('result', list(d.items()))
class LiveKeys:
    def __init__(self):
        self.names = ['x', 'y']
    def keys(self):
        print('live_keys')
        return self.names
    def __getitem__(self, key):
        print('live_get', key)
        if key == 'x':
            self.names.append('z')
        return key
print('live_result', list(OD(LiveKeys()).items()))
class AttributeFallback:
    @property
    def keys(self):
        print('keys_attribute')
        raise AttributeError('missing keys attribute')
    def __iter__(self):
        print('fallback_pairs')
        yield ('fallback', 7)
print('fallback', list(OD(AttributeFallback()).items()))
error = AttributeError('keys call error')
class CallFailure:
    def keys(self):
        print('keys_call')
        raise error
    def __iter__(self):
        raise AssertionError('keys 调用失败不得回退')
try:
    OD(CallFailure())
except AttributeError as exc:
    print('call_failure', exc is error, str(exc), exc.__cause__, exc.__context__)
s = OD(a=1, b=2, c=3)
s.move_to_end('a')
print('od_source', list(OD(s).items()))
d.update(s)
print('od_update', list(d.items()))
print('DONE streaming_mapping_update')
