# ryupy: pending
# 模块槽未绑定时仍应回退内建；当前未知名字竖切不冒称实现这一层。
print(len([1, 2]))
len = 3
print(len)


def declared_builtin():
    global locals
    return isinstance(locals(), dict)


print(declared_builtin())
if False:
    locals = None
