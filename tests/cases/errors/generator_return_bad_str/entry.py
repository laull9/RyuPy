class Result:
    def __str__(self):
        print("格式化一次")
        raise ValueError("消息失败")


def result():
    if False:
        yield 0
    return Result()


next(result())
