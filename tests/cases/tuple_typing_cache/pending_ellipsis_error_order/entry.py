# Ellipsis普通前缀CP合法，必须先检查再遇非法tuple，完整重跑顺序不可裁剪。
# CP 接受 Ellipsis 作为普通前缀，必须真的执行检查后才到后面的非法 exact tuple。
import typing as t
original=t._type_check
log=[]
def checking(value,message):
    log.append((repr(value),message))
    return original(value,message)
t._type_check=checking
for raw,expected in [((Ellipsis,()),'Tuple[t0, t1, ...]: each t must be a type. Got ().'),((Ellipsis,(),Ellipsis),'Tuple[t, ...]: t must be a type. Got ().')]:
    log.clear()
    try:
        t.Tuple[raw]
    except TypeError as e:
        assert str(e)==expected
        assert len(log)==4 and [x[0] for x in log]==['Ellipsis','()','Ellipsis','()']
        print(str(e),log)
    else:
        assert False
