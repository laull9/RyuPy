# 仅由动态名称消费的已声明源码，不能在启动期提前执行。
from inspect import signature


def operation(value=9):
    return value


def describe():
    return str(signature(operation))
