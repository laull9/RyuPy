# 不调用observer：异常帧与getter失败相位独立观察，保完整未捕获错误链。
class Receiver:
    @property
    def selector(self):
        raise TypeError("target failed")


def produce(target):
    try:
        yield "ready"
        raise ValueError("original")
    except target.selector:
        yield "wrong"
    except TypeError:
        yield "wrong sibling"


factory = produce
it = factory(Receiver())
print(next(it))
next(it)
