# ryupy: pending
# 错误地沿 Left.BaseClass 会漏掉 right。
import subject
assert subject.Child().label() == 'left:right:base'
print('pending receiver MRO suffix')
