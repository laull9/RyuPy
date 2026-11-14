value = 10

class Box:
    value = 70
    seen = []
    def read(self, offset=value):
        return value + offset
    @staticmethod
    def static_read():
        return value
    @classmethod
    def class_read(cls):
        cls.seen.append(value)
        return cls.value, value
    @property
    def current(self):
        return value
