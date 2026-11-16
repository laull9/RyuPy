# ryupy: pending
# CP真实repr/str包含同次对象地址；只输出结构检查结果，不冻结不稳定地址。
import xml.etree.ElementTree as ET

root = ET.Element('leaf')
ET.SubElement(root, 'child')
tree = ET.ElementTree(root)
children = iter(root)
preorder = root.iter()

def check_representation(label, value, prefix, type_name):
    representation = repr(value)
    string = str(value)
    direct = value.__repr__()
    address = hex(id(value))
    expected = prefix + address + '>'
    assert type(value).__name__ == type_name
    assert representation == expected
    assert string == representation and direct == representation
    assert representation.startswith(prefix) and representation.endswith('>')
    print(label, type(value).__name__, representation == expected, string == direct)

check_representation('element', root, "<Element 'leaf' at ", 'Element')
check_representation('tree', tree, '<xml.etree.ElementTree.ElementTree object at ', 'ElementTree')
check_representation('children', children, '<iterator object at ', 'iterator')
check_representation('preorder', preorder, '<_elementtree._element_iterator object at ', '_element_iterator')
assert next(children) is root[0]
assert next(preorder) is root
check_representation('children-after-next', children, '<iterator object at ', 'iterator')
check_representation('preorder-after-next', preorder, '<_elementtree._element_iterator object at ', '_element_iterator')

name = ET.QName('urn:sample', 'item')
assert str(name) == '{urn:sample}item'
assert repr(name) == "<QName '{urn:sample}item'>"
assert name.__repr__() == repr(name)
print('qname', str(name), repr(name), name.__repr__() == repr(name))
name.text = 'changed'
assert str(name) == 'changed' and repr(name) == "<QName 'changed'>"
assert name.__repr__() == repr(name)
print('qname-mutated', str(name), repr(name))
