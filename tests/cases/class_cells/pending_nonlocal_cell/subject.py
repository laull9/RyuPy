class Mutable:
    def read(self):
        return __class__
    def write(self, value):
        nonlocal __class__
        __class__ = value
    def delete(self):
        nonlocal __class__
        del __class__
obj = Mutable()
