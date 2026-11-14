# 多层导入保同一类型；ElementTree只包装真实根，不读取空节点真值。
import xml
import xml.etree
import xml.etree.ElementTree as ET
from xml.etree import ElementTree as imported
from xml.etree.ElementTree import Element, SubElement, ElementTree

assert xml.etree.ElementTree is ET and imported is ET
assert ET.Element is Element and ET.SubElement is SubElement
assert ET.ElementTree is ElementTree
root = Element('catalog')
assert type(root) is Element and isinstance(root, Element)
other = Element('catalog')
assert root is not other and root != other
roots = {root: 'first', other: 'second'}
assert roots[root] == 'first' and roots[other] == 'second'
tree = ElementTree(root)
assert tree.getroot() is root
assert ElementTree(element=root).getroot() is root
assert ElementTree().getroot() is None
assert ElementTree(None).getroot() is None
alias = tree.getroot()
child = SubElement(alias, 'item')
child.text = 'persisted'
assert root[0] is child and tree.getroot()[0] is child
assert tree.getroot()[0].text == 'persisted'
print('modules', xml.etree.ElementTree is ET, ET.Element is Element)
print('types', type(root) is Element, isinstance(tree, ElementTree))
print('identity', root == root, root == other, roots[root], roots[other])
print('roots', tree.getroot() is root, ElementTree().getroot())
print('shared-child', tree.getroot()[0] is child, root[0].text)
