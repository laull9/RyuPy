# ryupy: pending
# 自定义 descriptor 不能被普通方法绑定冒充。
import subject
obj = subject.Child()
assert obj.read() == (obj, subject.Child)
print('pending arbitrary descriptor binding')
