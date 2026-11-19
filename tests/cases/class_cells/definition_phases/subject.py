import helpers
__class__ = 'module value'
@(lambda: (helpers.phase.append(__class__), helpers.class_decorator)[1])()
class Subject((lambda: (helpers.phase.append(__class__), helpers.Base)[1])()):
    body_lambda = lambda: __class__
    @helpers.method_decorator(lambda: __class__)
    def ident(self):
        return __class__
    def default(self, callback=lambda: __class__):
        return callback()
