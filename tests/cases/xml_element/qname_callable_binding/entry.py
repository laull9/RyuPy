# 已公开QName Python方法与object wrapper绑定，保完整错误文本。
from xml.etree.ElementTree import QName, ElementTree
q = QName('x')
tree = ElementTree(q)
def check(label, function):
    try:
        print(label, function())
    except TypeError as error:
        print(label, type(error).__name__, str(error))
check('eq-keyword', lambda: q.__eq__(other='x'))
check('eq-empty', lambda: q.__eq__())
check('eq-extra', lambda: q.__eq__('x', 'y'))
check('eq-self', lambda: q.__eq__('x', self=q))
check('ne-keyword', lambda: q.__ne__(other='x'))
check('ne-empty', lambda: q.__ne__())
check('ne-extra', lambda: q.__ne__('x', 'y'))
check('str-extra', lambda: q.__str__(1))
check('hash-extra', lambda: q.__hash__(1))
check('tree-extra', lambda: tree.getroot(1))
check('tree-self', lambda: tree.getroot(self=tree))
check('qname-empty', lambda: QName())
check('qname-extra', lambda: QName('x', 'y', 'z'))
check('tree-init-extra', lambda: ElementTree(None, None, None))
print('root', tree.getroot() is q)
