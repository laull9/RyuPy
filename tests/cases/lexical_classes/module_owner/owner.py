# 被另一个模块调用的普通类工厂；元数据取定义模块当前绑定，不取调用者ambient。
def factory(value):
    class Product:
        field = value
        def read(self, initial=value):
            return initial, value, __class__
    return Product
