# 外函数新增动态handler不应误判为生成器；内生成器仍旧静态。
def ordinary(target, value):
    def stream():
        try:
            yield 1
        except ValueError:
            yield 2
    try:
        raise value
    except target:
        return stream()
