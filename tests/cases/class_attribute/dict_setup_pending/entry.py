"""CP 合法域参考 pending：用实例字典布置同名字段，只观察读取，不做动态改类。"""
import json

events = []

def emit(case, *values):
    print(json.dumps({"case": case, "values": values, "events": events[:]}, ensure_ascii=False))
    events.clear()

class Plain:
    pass

plain = Plain()
plain.__dict__["__class__"] = "instance field"
emit("root_data_descriptor_beats_field", plain.__class__ is Plain,
     type(plain) is Plain, plain.__dict__["__class__"])

class Ordinary:
    __class__ = "class value"

obj = Ordinary()
obj.__dict__["__class__"] = "instance field"
emit("class_shadow_allows_field", obj.__class__, type(obj) is Ordinary,
     Ordinary.__class__ is type, Ordinary.__dict__["__class__"])

class Method:
    def __class__(self):
        events.append("method body")
        return "method"

obj = Method()
obj.__dict__["__class__"] = "instance field"
emit("field_beats_nondata_method", obj.__class__, type(obj) is Method)

class Property:
    @property
    def __class__(self):
        events.append("getter")
        return "property"

obj = Property()
obj.__dict__["__class__"] = "instance field"
emit("property_beats_field", obj.__class__, type(obj) is Property,
     obj.__dict__["__class__"])

class NoGetter:
    __class__ = property()

obj = NoGetter()
obj.__dict__["__class__"] = "instance field"
try:
    obj.__class__
except Exception as exc:
    emit("getter_missing_beats_field", type(exc).__name__, str(exc))

class ChildOrdinary(Property):
    __class__ = "child"

obj = ChildOrdinary()
obj.__dict__["__class__"] = "instance field"
emit("nearest_ordinary_not_base_property", obj.__class__, type(obj) is ChildOrdinary)
