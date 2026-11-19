# ryupy: pending
# 若旧入口忽略 Middle 则误返 middle；此能力当前必须明确拒绝。
import subject
assert subject.Child().label() == 'base'
print('pending default explicit arguments must not be ignored')
