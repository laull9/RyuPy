# ryupy: pending
# QName.__eq__是Python方法可用other=，__ne__继承object则不接受keyword。
import xml.etree.ElementTree as ET
q = ET.QName('x')
try:
    print('eq-keyword', q.__eq__(other='x'))
except TypeError as error:
    print('eq-keyword', type(error).__name__, str(error))
try:
    print('ne-keyword', q.__ne__(other='x'))
except TypeError as error:
    print('ne-keyword', type(error).__name__, str(error))
original = ET.QName
ET.QName = 1
try:
    print('rebound-eq', q.__eq__(q))
except TypeError as error:
    print('rebound-eq', type(error).__name__, str(error))
ET.QName = original
assert q == q
