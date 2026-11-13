# 模块字典同名反射属性不应遮蔽编译期常量；不扩展 globals() 支持。
import subject
from subject import read
print('import', subject.initial, read())
setattr(subject, '__debug__', False)
print('injected', getattr(subject, '__debug__'), subject.read(), __debug__)
