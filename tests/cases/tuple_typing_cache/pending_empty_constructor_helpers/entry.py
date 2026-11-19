# 空Tuple跳过类型检查但仍消费constructor/collector；每次先128fresh逐出空键，确保真正cold。
import typing as t
root = t.Tuple
for helper in ['_GenericAlias', '_collect_parameters']:
    original = getattr(t, helper)
    for mode in ['rebind', 'delete']:
        for i in range(128):
            root[t.TypeVar('EmptyPressure')]
        try:
            if mode == 'rebind':
                setattr(t, helper, None)
            else:
                delattr(t, helper)
            try:
                root[()]
            except (TypeError, NameError) as error:
                if mode == 'rebind':
                    assert type(error) is TypeError
                    assert str(error) == "'NoneType' object is not callable"
                else:
                    assert type(error) is NameError
                    assert str(error) == "name '" + helper + "' is not defined"
                print(helper, mode, type(error).__name__, str(error))
            else:
                assert False
        finally:
            setattr(t, helper, original)
        assert root[()].__args__ == ()
print('empty-cold-consumes-constructor-and-collector', True)
