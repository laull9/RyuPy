
# 带业务策略的记录：自定义初始化/表示/missing与显式根方法保持不同分派。
EVENTS = []
class Policy(dict):
    def __init__(self, title='default', rows=(), **extra):
        EVENTS.append(title)
        self.title = title
        dict.__init__(self, rows, **extra)

    def __repr__(self):
        return 'Policy<' + self.title + '>'

    def __missing__(self, key):
        return 'missing:' + key

    def get(self, key, default=None):
        return 'business-get'

    def update(self, *args, **kwargs):
        return 'business-update'

    def root_get(self):
        return super().get

    def root_update(self, rows):
        return super().update(rows)

p = Policy('order', [('sku', 3)], state='open')
assert EVENTS == ['order'] and p['sku'] == 3
assert repr(p) == 'Policy<order>' and str(p) == 'Policy<order>'
assert dict.__repr__(p) == "{'sku': 3, 'state': 'open'}"
assert dict.__str__(p) == 'Policy<order>'
assert p.get('sku') == 'business-get' and dict.get(p, 'sku') == 3
assert p.update({'ignored': 1}) == 'business-update' and 'ignored' not in p
assert dict.update(p, {'root': 4}) is None and p.root_update({'super': 5}) is None
saved = p.root_get()
assert saved.__self__ is p
p['sku'] = 6
assert saved('sku') == 6
print('dict-subclass.policy-root', repr(p), dict.__repr__(p), saved('sku'))

p.__missing__ = lambda key: 'instance'
assert p['absent'] == 'missing:absent' and dict.__getitem__(p, 'absent') == 'missing:absent'
assert dict.get(p, 'absent') is None and 'absent' not in p
assert p.pop('absent', 'fallback') == 'fallback'
def late(self, key):
    return 'late:' + key
Policy.__missing__ = late
assert p['absent'] == 'late:absent'
del Policy.__missing__
try:
    p['absent']
except KeyError as error:
    assert error.args == ('absent',)
else:
    raise AssertionError('instance missing must not be used')
print('dict-subclass.missing-lookup', p.pop('absent', 'fallback'))

raw = dict.__new__(Policy, 1, 2, ignored=True)
assert type(raw) is Policy and len(raw) == 0 and not hasattr(raw, 'title')
assert EVENTS == ['order']
values = []
made = Policy.fromkeys(['left', 'right'], values)
assert type(made) is Policy and made.title == 'default' and EVENTS == ['order', 'default']
assert made['left'] is values and made['right'] is values
made2 = p.fromkeys(['only'], values)
assert type(made2) is Policy and made2['only'] is values
assert type(dict.fromkeys(['plain'])) is dict
print('dict-subclass.fromkeys-init', EVENTS, list(made), list(made2))
