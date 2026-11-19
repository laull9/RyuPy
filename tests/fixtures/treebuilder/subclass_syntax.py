"""准确新builtin基类必须有位置E0100，不能落到MissingMethod。"""
class Child(SyntaxError):
    pass
