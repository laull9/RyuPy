# ryupy: pending
# 类接收者不能被误收为普通 PyInstance。
import subject
assert subject.Child.identify() is subject.Child
print('pending class receiver')
