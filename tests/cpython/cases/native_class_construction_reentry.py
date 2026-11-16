"""C1 构造回调中的 descriptor/new/property/inherited classmethod 保留单一类和实例身份。"""
from bridge_fixture.class_construction import EagerBase, SetNameBase

seen = []


class Desc:
    def __set_name__(self, owner, name):
        seen.append(owner)


class Named(EagerBase):
    marker = Desc()


assert seen.pop() is Named


class ManualNamed(SetNameBase):
    marker = Desc()


assert seen.pop() is ManualNamed


class Allocated(EagerBase):
    eager_new = True

    def __new__(cls):
        seen.append(cls)
        return object.__new__(cls)


assert seen.pop() is Allocated
allocated = Allocated()
assert seen.pop() is Allocated
allocated.__new__.__name__ = 'renamed_new'
again = allocated.__new__(Allocated)
assert seen.pop() is Allocated
assert type(again) is Allocated


class SetProperty(EagerBase):
    eager_property = 'setter'

    def put(self, value):
        seen.append(self)
    value = property(None, put)


assert type(seen.pop()) is SetProperty


class DelProperty(EagerBase):
    eager_property = 'deleter'

    def remove(self):
        seen.append(self)
    value = property(None).deleter(remove)


assert type(seen.pop()) is DelProperty


class Parent(EagerBase):
    @classmethod
    def inherited_callback(cls):
        seen.append(cls)
        return cls


class Inherited(Parent):
    eager_inherited = True


assert seen.pop() is Inherited
assert seen == []
print('native class construction reentry passed')
