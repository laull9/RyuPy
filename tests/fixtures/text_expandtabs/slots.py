# ryupy: pending
# 类型级index及descriptor raw三参；只用静态类定义，不依赖动态type或类__dict__。
events = []


class Value:
    def __call__(self):
        events.append('value')
        return 4


value = Value()


class PropertyIndex:
    @property
    def __index__(self):
        events.append('property')
        return value


class StaticIndex:
    __index__ = staticmethod(value)


class ClassIndex:
    @classmethod
    def __index__(cls):
        assert cls is ChildIndex
        return 4


class ChildIndex(ClassIndex):
    pass


class CallableIndex:
    __index__ = value


class Descriptor:
    def __get__(self, instance, owner):
        assert owner is ChildDescriptorIndex
        assert type(instance) is owner
        events.append('get')
        return value


descriptor = Descriptor()


class DescriptorIndex:
    __index__ = descriptor


class ChildDescriptorIndex(DescriptorIndex):
    pass


for index in (PropertyIndex(), StaticIndex(), ChildIndex(), CallableIndex()):
    assert 'a\t'.expandtabs(index) == 'a   '
index = ChildDescriptorIndex()
index.__index__ = lambda: 8
descriptor.__get__ = lambda instance, owner: None
events.clear()
assert 'a\t'.expandtabs(index) == 'a   '
assert events == ['get', 'value']


class RawGet:
    def __call__(self, descriptor, instance, owner):
        assert owner is RawIndex
        events.append('raw-three')
        return value


class RawDescriptor:
    __get__ = RawGet()


class RawIndex:
    __index__ = RawDescriptor()


assert '\t'.expandtabs(RawIndex()) == '    '
assert events[-2:] == ['raw-three', 'value']


class ClassGet:
    @classmethod
    def __get__(cls, instance, owner):
        raise AssertionError('不能解包classmethod')


class PropertyGet:
    @property
    def __get__(self):
        raise AssertionError('不能调用property getter')


class ClassGetIndex:
    __index__ = ClassGet()


class PropertyGetIndex:
    __index__ = PropertyGet()


for index, message in ((ClassGetIndex(), "'classmethod' object is not callable"),
                       (PropertyGetIndex(), "'property' object is not callable")):
    try:
        '\t'.expandtabs(index)
    except TypeError as error:
        assert str(error) == message
    else:
        raise AssertionError('raw槽不可调用')
marker = LookupError('getter-marker')


class RaisingProperty:
    @property
    def __index__(self):
        raise marker


class RaisingDescriptor:
    def __get__(self, instance, owner):
        raise marker


class RaisingIndex:
    __index__ = RaisingDescriptor()


for index in (RaisingProperty(), RaisingIndex()):
    try:
        ''.expandtabs(index)
    except LookupError as error:
        assert error is marker
    else:
        raise AssertionError('getter异常不能丢失')
print('expandtabs slots')
