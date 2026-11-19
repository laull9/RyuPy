# 已承诺域的非法实参必须报真实错误，且不挂半成品、不覆盖原属性。
import xml.etree.ElementTree as ET

root = ET.Element('root', {'safe': 'value'})
child = ET.SubElement(root, 'child')
attributes = root.attrib

def fails(label, action, expected):
    try:
        action()
    except expected as error:
        print(label, type(error).__name__, str(error))
    else:
        raise AssertionError(label + ' did not fail')
    assert len(root) == 1 and root[0] is child
    assert root.attrib is attributes and root.attrib == {'safe': 'value'}

fails('missing-tag', lambda: ET.Element(), TypeError)
fails('extra-positional', lambda: ET.Element('x', {}, 'extra'), TypeError)
fails('attrib-list', lambda: ET.Element('x', []), TypeError)
fails('subelement-parent', lambda: ET.SubElement(None, 'x'), TypeError)
fails('subelement-attrib', lambda: ET.SubElement(root, 'x', []), TypeError)
fails('append-bad', lambda: root.append('bad'), TypeError)
fails('insert-bad-child', lambda: root.insert(0, 'bad'), TypeError)
fails('insert-bad-index', lambda: root.insert('bad', child), TypeError)
fails('index-high', lambda: root[1], IndexError)
fails('index-low', lambda: root[-2], IndexError)
fails('index-string', lambda: root['bad'], TypeError)
fails('slice-zero', lambda: root[::0], ValueError)
fails('get-no-key', lambda: root.get(), TypeError)
fails('set-no-value', lambda: root.set('key'), TypeError)
fails('items-extra', lambda: root.items(1), TypeError)
fails('keys-extra', lambda: root.keys(1), TypeError)

def assign_bad():
    root.attrib = None

fails('assign-attrib', assign_bad, TypeError)
print('final-state', root.tag, root.attrib, [node.tag for node in root])

# v3根据默认C观察修正新增构造绑定；上方v1所有断言、输出和失败状态检查原样保留。
named = ET.Element('named', attrib={'a': 'source'}, a='extra', text='attribute')
assert named.tag == 'named' and named.attrib == {'a': 'extra', 'text': 'attribute'}
assert named.text is None
print('element-keywords', named.tag, list(named.attrib.items()), named.text)

parent = ET.Element('parent')
sub = ET.SubElement(parent, 'child', {'a': 'old'}, a='new', tag='attribute-tag', attrib='attribute-value')
assert sub.attrib == {'a': 'new', 'tag': 'attribute-tag', 'attrib': 'attribute-value'}
assert sub.tag == 'child' and parent[0] is sub
print('subelement-keywords-are-attributes', sub.tag, list(sub.attrib.items()))

extra_tag = ET.Element('x', tag='again')
assert extra_tag.tag == 'x' and extra_tag.attrib == {'tag': 'again'}
extra_attrib = {'nested': 'value'}
duplicate_attrib = ET.Element('x', {'original': 'value'}, attrib=extra_attrib)
assert duplicate_attrib.attrib == {'original': 'value', 'attrib': {'nested': 'value'}}
assert duplicate_attrib.attrib['attrib'] is extra_attrib
print('element-keywords-by-position', extra_tag.attrib, duplicate_attrib.attrib)
fails('element-keyword-tag-not-binding', lambda: ET.Element(tag='x'), TypeError)
fails('element-no-tag-with-attrib', lambda: ET.Element(attrib={}), TypeError)
fails('element-bad-attrib-before-extra', lambda: ET.Element('x', None, extra='value'), TypeError)
fails('element-overarity-before-attrib', lambda: ET.Element('x', None, 'third'), TypeError)
fails('subelement-keyword-parent-tag', lambda: ET.SubElement(parent=root, tag='x'), TypeError)
fails('subelement-keyword-tag', lambda: ET.SubElement(root, tag='x'), TypeError)
fails('subelement-overarity-before-parent', lambda: ET.SubElement(None, 'x', {}, 'fourth'), TypeError)
fails('subelement-parent-before-attrib', lambda: ET.SubElement(None, 'x', None), TypeError)
fails('subelement-attrib-none', lambda: ET.SubElement(root, 'x', None), TypeError)
print('binding-final-state', len(root), root[0] is child, root.attrib is attributes)
