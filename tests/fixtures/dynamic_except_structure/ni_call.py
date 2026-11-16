# 首切明确不支持：handler位置是Call表达式。
def unsupported(factory, value):
    try:
        raise value
    except factory():
        return None
