# 唯一显式Instance模块：构造与属性调用表达式在本函数内执行，类与变更回调由Legacy提供。
alias = super


def canonical():
    return alias


def construct(start, receiver):
    return alias(start, receiver)


def direct(start, receiver):
    return super(start, receiver)


def invoke_read(proxy):
    return proxy.read()


def invoke_callback(proxy, callback):
    return proxy.call(callback())
