# 函数内非简单目标的注解不求值；右值、目标和实际存储仍按普通赋值执行。
log = []
class Holder:
    pass
holder = Holder()
items = [0, 1]

def base():
    log.append('base')
    return holder

def sequence():
    log.append('sequence')
    return items

def index():
    log.append('index')
    return 0

def rhs():
    log.append('rhs')
    return 7

def annotation():
    raise AssertionError('局部注解不应求值')

def update():
    base().value: annotation() = rhs()
    sequence()[index()]: UnknownAnnotation = rhs()
    base().missing: annotation()
    sequence()[index()]: annotation()

update()
print(log)
print(holder.value, items, hasattr(holder, 'missing'))

# 注解不执行与“是否生成器”的语法判定不同：直接 yield 仍标记工厂。
def annotated_generator():
    holder.generated: (yield '不会产出') = 11
    return 19

iterator = annotated_generator()
print(hasattr(holder, 'generated'))
try:
    next(iterator)
except StopIteration as error:
    print(error.value, holder.generated)

# lambda 的 body 是另一个作用域，不能把普通函数误标成生成器。
def ordinary():
    holder.lambda_value: (lambda: (yield 1)) = 12
    return '普通函数'

print(ordinary(), holder.lambda_value)

# ignored annotation 内的 super 不需要保存恢复上下文，因为永远不调用。
def annotated_super():
    holder.super_value: super() = 13
    yield holder.super_value

print(list(annotated_super()))

# ignored lambda 默认值的 yield 属于外层符号判定，但仍无运行时暂停。
def annotation_default():
    holder.default_value: (lambda arg=(yield 1): arg) = 14
    return 23

try:
    next(annotation_default())
except StopIteration as error:
    print(error.value, holder.default_value)
