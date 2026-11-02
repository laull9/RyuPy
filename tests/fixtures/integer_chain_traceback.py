# 未捕获错误driver候选：保用户抛错帧和调用链，不把溢出回退当新Python调用。
import sys


def chain(a, b, c):
    return a + b + c


class AddFailure:
    def __add__(self, other):
        raise ValueError('chain-add')


class RightFailure:
    def __radd__(self, other):
        raise ValueError('chain-right')


if sys.argv[1] == 'add':
    chain(AddFailure(), 1, 2)
else:
    chain(9223372036854775807, 1, RightFailure())
