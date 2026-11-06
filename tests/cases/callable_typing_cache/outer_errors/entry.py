import typing as t
C=t.Callable
expected='Callable must be used as Callable[[arg, ...], result].'
def check(value):
    try: C[value]
    except TypeError as error:
        assert str(error)==expected
    else: assert False
for raw in (int, [], [[],int], (), (int,), (int,int,int)):
    check(raw)
t._type_check=None
for raw in (int, (), (int,), (int,int,int)):
    check(raw)
print('non-tuple and non-two lengths: exact TypeError before helper: ok')
