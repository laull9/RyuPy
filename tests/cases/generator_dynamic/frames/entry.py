# 不调用observer：异常帧与getter失败相位独立观察，保完整未捕获错误链。
class Target:
    @property
    def error(self):
        raise TypeError("target failed")


def generate(target):
    try:
        yield "ready"
        raise ValueError("original")
    except target.error:
        yield "wrong"
    except TypeError:
        yield "wrong sibling"


it = generate(Target())
print(next(it))
next(it)
