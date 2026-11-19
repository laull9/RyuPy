# 工厂生成真实Element，以函数身份作tag；QName真类型和HTML_EMPTY共享可变集合。
import xml.etree.ElementTree as ET
from xml.etree.ElementTree import Comment, ProcessingInstruction, PI, QName, HTML_EMPTY

comment = Comment('note <&>')
assert type(comment) is ET.Element and comment.tag is Comment
assert comment.text == 'note <&>' and comment.tail is None
assert Comment().text is None
pi = ProcessingInstruction('target', 'data <&>')
assert pi.tag is ProcessingInstruction and pi.text == 'target data <&>'
assert PI is ProcessingInstruction
assert PI('target').text == 'target'
assert PI('target', '').text == 'target'
name = QName('urn:sample', 'item')
other = QName('{urn:sample}item')
assert type(name) is QName and isinstance(name, QName)
assert name.text == '{urn:sample}item' and str(name) == name.text
assert name == other and name == '{urn:sample}item'
assert '{urn:sample}item' == name and name != 'item'
assert hash(name) == hash(name.text)
lookup = {name: 'payload'}
assert lookup[other] == 'payload' and lookup[name.text] == 'payload'
payload = lookup[other]
node = ET.Element(name)
assert node.tag is name
name.text = 'changed'
assert str(name) == 'changed' and hash(name) == hash('changed')
expected = ['area', 'base', 'basefont', 'br', 'col', 'embed', 'frame', 'hr', 'img',
            'input', 'isindex', 'link', 'meta', 'param', 'source', 'track', 'wbr']
assert type(HTML_EMPTY) is set and sorted(HTML_EMPTY) == expected
assert ET.HTML_EMPTY is HTML_EMPTY
HTML_EMPTY.add('temporary')
assert 'temporary' in ET.HTML_EMPTY
ET.HTML_EMPTY.remove('temporary')
assert sorted(HTML_EMPTY) == expected
print('sentinels', comment.tag is Comment, pi.tag is PI, comment.text, pi.text)
print('qname', other.text, str(name), node.tag is name, payload)
print('html-empty', sorted(HTML_EMPTY), ET.HTML_EMPTY is HTML_EMPTY)
