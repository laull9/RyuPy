# ryupy: pending
# 非法ForwardRef语法的精确SyntaxError尚未支持；保留原冻结构造次序合同。
import typing as t

def check(label, call):
    try:
        call()
    except Exception as error:
        print(label, type(error).__name__, str(error))
    else:
        assert False

check('bound-before-single', lambda: t.TypeVar('V', int, bound='['))
check('bound-before-conflict', lambda: t.TypeVar('V', int, str, bound='['))
check('flags-before-bound', lambda: t.TypeVar('V', bound='[', covariant=True, contravariant=True))
