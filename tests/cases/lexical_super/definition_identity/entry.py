# 方法移植、公开类名和元数据重绑均不能替换真实定义类。
import subject
obj = subject.original()
leaf = subject.Leaf()
assert subject.saved(obj) == (obj, subject.original, 'initial')
assert leaf.transplanted() == (leaf, subject.original, 'initial')
subject.marker = 'updated'
assert subject.saved(obj) == (obj, subject.original, 'updated')
try:
    subject.Foreign().transplanted()
except TypeError as exc:
    assert str(exc) == 'super(type, obj): obj must be an instance or subtype of type'
else:
    assert False
print('definition identity survives transplant and global rebinding')
