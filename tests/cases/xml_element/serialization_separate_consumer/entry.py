# ryupy: pending
# 标准库XML/HTML/text输出是后续独立消费者，不替代Markdown自己的serializer。
import xml.etree.ElementTree as ET

root = ET.Element('root', {'z': '&<>"\n\r\t', 'a': "'"})
root.text = '&amp;<>中文😀'
child = ET.SubElement(root, 'empty')
child.tail = 'TAIL&'
root.tail = 'OUT<'
xml = ET.tostring(root, encoding='unicode')
long_xml = ET.tostring(root, encoding='unicode', short_empty_elements=False)
assert '&amp;amp;&lt;&gt;中文😀' in xml
assert 'z="&amp;&lt;&gt;&quot;&#10;&#13;&#09;"' in xml
assert '<empty />TAIL&amp;</root>OUT&lt;' in xml
assert '<empty></empty>' in long_xml
assert list(root.attrib) == ['z', 'a'] and child.tail == 'TAIL&'
print('xml', repr(xml))
print('xml-long', repr(long_xml))
encoded = ET.tostring(root)
assert type(encoded) is bytes
print('default-bytes', encoded)

html_root = ET.Element('section')
script = ET.SubElement(html_root, 'script')
script.text = 'if (a < b && c > d) {}'
ET.SubElement(html_root, 'br').tail = '<&>'
print('html', repr(ET.tostring(html_root, encoding='unicode', method='html')))
print('text', repr(ET.tostring(html_root, encoding='unicode', method='text')))

special = ET.Element('root')
special.append(ET.Comment('note'))
special.append(ET.ProcessingInstruction('target', 'data'))
print('special', repr(ET.tostring(special, encoding='unicode')))

namespaced = ET.Element(ET.QName('urn:test', 'root'))
namespaced.set(ET.QName('urn:test', 'key'), 'value')
print('namespace', repr(ET.tostring(namespaced, encoding='unicode')))
assert type(root) is ET.Element and root[0] is child
