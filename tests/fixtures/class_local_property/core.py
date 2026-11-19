# 同类刚定义方法的raw命名空间读取；property实读写、定义位置取值和方法全局作用域。
events = []
decorator_inputs = []
offset = 100
conditional = 'module-fallback'

def getv(self):
    raise AssertionError('module getter must not become the property getter')
def setv(self, value):
    raise AssertionError('module setter must not become the property setter')
module_getv = getv
module_setv = setv

def capture(raw):
    decorator_inputs.append(raw)
    def decorate(method):
        return method
    return decorate

class Cell:
    before_get = getv
    before_set = setv
    before_offset = offset
    offset = 3
    after_offset = offset

    def __init__(self, value):
        self._value = value
    def getv(self):
        events.append(('get', self._value))
        return self._value + offset
    alias_get = getv
    def setv(self, value):
        events.append(('set', value))
        self._value = value
    alias_set = setv
    v = property(getv, setv)
    alias_property = v
    raw_getter = v.fget is getv
    raw_setter = v.fset is setv

    @capture(getv)
    def snapshot(self, reader=getv, captured=offset):
        return reader(self), captured, offset

    def nested(self):
        def read():
            return offset
        return read

# 类体取原始函数/descriptor，不绑定self或调用getter/setter。
assert events == []
assert Cell.before_get is module_getv
assert Cell.before_set is module_setv
assert Cell.before_offset == 100
assert Cell.after_offset == 3
assert Cell.raw_getter and Cell.raw_setter
assert Cell.alias_get is Cell.getv
assert Cell.alias_set is Cell.setv
assert Cell.__dict__['getv'] is Cell.alias_get
assert Cell.__dict__['v'] is Cell.alias_property
assert Cell.v is Cell.alias_property
assert len(decorator_inputs) == 1
assert decorator_inputs[0] is Cell.getv

cell = Cell(5)
assert cell.v == 105
cell.v = 7
assert cell.alias_property == 107
assert cell.snapshot() == (107, 3, 100)
assert events == [('get', 5), ('set', 7), ('get', 7), ('get', 7)]
print('property', cell._value, Cell.before_offset, Cell.after_offset)
print('events', events)

# 默认值保存定义时类值；方法及方法内闭包都仍按模块全局late查找。
reader = cell.nested()
Cell.offset = 900
offset = 200
assert cell.v == 207
assert cell.snapshot() == (207, 3, 200)
assert reader() == 200
print('scope', cell.snapshot(), reader())

# 可选类名只在真正存在时遮蔽模块名，未执行的分支不制造绑定。
class Present:
    if True:
        conditional = 'class-selected'
    selected = conditional
class Absent:
    if False:
        conditional = 'wrong'
    selected = conditional
assert Present.selected == 'class-selected'
assert Absent.selected == 'module-fallback'
assert 'conditional' not in Absent.__dict__
print('branches', Present.selected, Absent.selected)

# 同类getter经property调用时仍保原方法owner，super读取父property。
class Child(Cell):
    def getv(self):
        return super().v + 1
    v = property(getv, Cell.setv)
child = Child(4)
assert child.v == 205
child.v = 8
assert child.v == 209
assert child._value == 8
print('super', child.v, child._value)
print('class-local-property-ok')
