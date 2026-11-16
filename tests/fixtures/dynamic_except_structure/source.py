# 结构门只含一个动态try，避免把其他清理scope误算作本scope的token。
def numeric(a: int, b: int) -> int:
    return a + b

def typed(value: int) -> int:
    try:
        return value // 0
    except ZeroDivisionError:
        return 7

def mixed(targets, value):
    try:
        raise value
    except targets.first as caught:
        return caught
    except ValueError:
        return None

# 未命中的as不能为后一个Name目标提供确定赋值。
def late_target(value):
    try:
        raise value
    except TypeError as missing:
        return None
    except missing:
        return None

# cleanup写Unbound后，finally裸读仍需LoadChecked。
def cleanup_read(target, value):
    try:
        try:
            raise value
        except target as caught:
            return None
    finally:
        print(caught)

# 正文赋值在异常发生之后，不能让handler目标跳过LoadChecked。
def body_target(early, value):
    try:
        if early:
            raise value
        target = ValueError
    except target:
        return None

# handler正文不能继承try里异常发生后未执行赋值的事实。
def handler_body(early, value):
    target = ValueError
    try:
        if early:
            raise value
        body_local = 7
    except target:
        return body_local
