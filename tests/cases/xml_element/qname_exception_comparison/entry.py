# ryupy: pending
# 用户异常对象仍可能带反向比较；QName的有限域应NI，不可静默False。
from xml.etree.ElementTree import QName
class EqualError(Exception):
    def __eq__(self, other):
        print('custom-eq', other)
        return True
q = QName('x')
other = EqualError()
print('direct', q.__eq__(other))
print('operator', q == other)
