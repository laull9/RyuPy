# 消费者与工厂分离；消费外部实例而不是在当前模块内重建生成器。
def drain(iterator):
    return sum(iterator)


def drain_with_start(iterator, start):
    return sum(iterator, start)


def ordinary(iterator):
    return list(iterator)
