"""C1 方法描述符消费者：在真正的 CPython 侧观察绑定与可变元数据，不走编译类镜像读法。"""


def verify(cls, instance, native=False):
    descriptor = vars(cls)['sample']
    assert descriptor.__get__(None, cls) is descriptor
    bound = descriptor.__get__(instance, cls)
    assert bound.__self__ is instance
    assert bound.__func__ is descriptor
    assert bound(2, z=3) == 12
    descriptor.__name__ = 'renamed'
    descriptor.__qualname__ = 'Changed.renamed'
    assert bound.__name__ == 'renamed'
    assert bound.__qualname__ == 'Changed.renamed'
    descriptor.tag = instance
    assert descriptor.__dict__['tag'] is instance
    descriptor.__module__ = 'changed_module'
    descriptor.__doc__ = 'changed doc'
    assert descriptor.__module__ == 'changed_module'
    assert descriptor.__doc__ == 'changed doc'
    descriptor.__annotations__ = {'return': int}
    assert descriptor.__annotations__['return'] is int
    descriptor.__defaults__ = (99,)
    descriptor.__kwdefaults__ = {'z': 88}
    # 真实调用仍由原编译函数绑定，包装层的无参转发元数据独立保存。
    assert bound(2, z=3) == 12
    for name in ('__name__', '__qualname__'):
        try:
            setattr(descriptor, name, 1)
        except TypeError:
            pass
        else:
            raise AssertionError(name + ' accepted an integer')
        try:
            delattr(descriptor, name)
        except TypeError:
            pass
        else:
            raise AssertionError(name + ' allowed deletion')
    if native:
        try:
            descriptor.__code__
        except NotImplementedError as error:
            assert 'CPython CodeType' in str(error)
        else:
            raise AssertionError('native descriptor fabricated a CPython CodeType')
    return True
