# 本切允许的普通模块属性重绑：旧工厂取定义全局当前tag，C SubElement不跟随Element名字。
import xml.etree.ElementTree as ET
saved_element = ET.Element
comment = ET.Comment
pi = ET.PI
assert pi is ET.ProcessingInstruction
comment_marker = []
pi_marker = []
ET.Comment = comment_marker
node = comment(comment_marker)
assert node.tag is comment_marker and node.text is comment_marker
ET.ProcessingInstruction = pi_marker
instruction = pi('target', 'payload')
assert instruction.tag is pi_marker and instruction.text == 'target payload'
ET.PI = 'unrelated'
assert pi('next').tag is pi_marker
root = saved_element('root')
ET.Element = None
child = ET.SubElement(root, 'child')
assert type(child) is saved_element and root[0] is child
ET.Element = saved_element
ET.Comment = comment
ET.ProcessingInstruction = pi
ET.PI = pi
assert comment('again').tag is comment
assert pi('again').tag is pi
print('factory', node.tag is comment_marker, node.text is comment_marker, instruction.tag is pi_marker)
print('pi', instruction.text, ET.PI is ET.ProcessingInstruction)
print('child', root[0] is child, type(child) is saved_element)
