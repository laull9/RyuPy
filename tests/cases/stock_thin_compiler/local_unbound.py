# ryupy: pending
# 函数级词法绑定和未走分支的局部必须保留UnboundLocalError，不能活查同名全局。
shadow = object()

def before(x):
    value = shadow
    shadow = x
    return value

def branch(x):
    if x is None:
        value = x
    return value

try:
    before(None)
except UnboundLocalError as error:
    print(type(error).__name__)
    print(error.name is None, error.args, str(error))
try:
    branch(shadow)
except UnboundLocalError as error:
    print(type(error).__name__)
    print(error.name is None, error.args, str(error))
print(branch(None) is None)
