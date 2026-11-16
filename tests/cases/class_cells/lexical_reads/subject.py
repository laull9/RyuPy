__class__ = 'module value'
x = 'module x'
class Subject:
    before = __class__
    __class__ = 'namespace value'
    seen = __class__
    x = 'namespace x'
    body_lambda = lambda: __class__
    def ident(self):
        return __class__
    def nested(self):
        def middle():
            return lambda: __class__
        return middle()
    def ordinary(self):
        return x
    def local(self):
        __class__ = 'local value'
        return lambda: __class__
    def parameter(self, __class__):
        return lambda: __class__
    def global_value(self):
        global __class__
        return __class__
    @staticmethod
    def static():
        return __class__
    @classmethod
    def class_method(cls):
        return cls, __class__
    @property
    def property_value(self):
        return __class__
class Other:
    pass
saved_class = Subject
saved_method = Subject.ident
Other.transplanted = Subject.ident
Subject.__module__ = 'changed class module'
Subject.ident.__module__ = 'changed function module'
Subject = Other
