# __class__ 用户遮蔽走普通属性协议，不触碰真实布局或 type 身份。
class D(dict):
    @property
    def __class__(self):
        return self.get('kind', 'initial')
    @__class__.setter
    def __class__(self, value):
        self['kind'] = value
    @__class__.deleter
    def __class__(self):
        self.pop('kind', None)
d = D()
print('get', d.__class__, type(d) is D)
d.__class__ = 'changed'
print('set', d.__class__, d['kind'], type(d) is D)
del d.__class__
print('delete', d.__class__, 'kind' in d, type(d) is D)
class Child(D):
    pass
child = Child()
child.__class__ = 'child-value'
print('inherited', child.__class__, type(child) is Child)
del child.__class__
print('inherited-delete', child.__class__, type(child) is Child)
class Shadow(dict):
    __class__ = 'class-field'
s = Shadow()
print('field-default', s.__class__, type(s) is Shadow)
s.__class__ = 'instance-field'
print('field-set', s.__class__, Shadow.__class__, type(s) is Shadow)
del s.__class__
print('field-delete', s.__class__, type(s) is Shadow)
