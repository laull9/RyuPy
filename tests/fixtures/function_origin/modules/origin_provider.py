# 未执行的真实辅助模块：工厂函数所属globals与调用者模块分离。
initial_name = __name__
def exported(value=10):
    return ['provider', value]
def set_name(value):
    global __name__
    __name__ = value
def make_closure(value):
    def nested():
        return value
    return nested
def make_stream(value):
    def stream():
        try:
            # 显式闭包属性读取，只验证metadata和闭包，不验证frame来源。
            yield [value, stream.__module__]
            yield [value + 1, stream.__module__]
        finally:
            print('stream-finally', value, stream.__module__)
    return stream
class Owner:
    def method(self):
        return 12
