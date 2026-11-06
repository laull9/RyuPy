# ModuleSpec 完整公开源差分已通过。
from importlib.machinery import ModuleSpec

trace = []

class ObservedSpec(ModuleSpec):
    @property
    def origin(self):
        trace.append('origin-get')
        self.origin_reads += 1
        if self.origin_reads == 1:
            return '/virtual/first.py'
        return '/virtual/second.py'

    @origin.setter
    def origin(self, value):
        self.origin_reads = 0

    @property
    def _cached(self):
        trace.append('cached-get')
        return self.cache_value

    @_cached.setter
    def _cached(self, value):
        trace.append('cached-set')
        self.cache_value = value

s = ObservedSpec('example', None)
s.has_location = True
trace.clear()
first = s.cached
assert first == '/virtual/__pycache__/second.cpython-312.pyc'
assert trace == ['cached-get', 'origin-get', 'origin-get', 'cached-set', 'cached-get']
print('first_result', first)
print('first_trace', trace)
trace.clear()
second = s.cached
assert second is first
assert trace == ['cached-get', 'cached-get']
print('sticky_identity', second is first)
print('sticky_trace', trace)
s.cached = None
s.has_location = False
trace.clear()
assert s.cached is None
assert trace == ['cached-get', 'origin-get', 'cached-get']
print('disabled_trace', trace)
print('complete', 'spec_cached_descriptors')
