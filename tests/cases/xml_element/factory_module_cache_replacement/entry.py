# ryupy: pending
# 保存的Python工厂保定义模块globals；缓存替换与原模块属性重绑是两种不同动作。
import sys
import types
import xml.etree.ElementTree as ET

module_name = 'xml.etree.ElementTree'
original = ET
comment = ET.Comment
pi = ET.ProcessingInstruction
element = ET.Element
qname = ET.QName
root = element('root')
name = qname('name')
decoy = types.ModuleType(module_name)
decoy.Comment = 'decoy-comment'
decoy.ProcessingInstruction = 'decoy-pi'
decoy.Element = 'not-callable'
comment_tag = object()
pi_tag = object()

try:
    sys.modules[module_name] = decoy
    assert sys.modules[module_name] is decoy
    assert original is ET and original is not decoy
    first = comment('original globals')
    second = pi('target', 'original globals')
    assert first.tag is comment and second.tag is pi
    assert type(first) is element and type(second) is element
    original.Comment = comment_tag
    original.ProcessingInstruction = pi_tag
    changed = comment('changed original binding')
    changed_pi = pi('target', 'changed original binding')
    assert changed.tag is comment_tag and changed_pi.tag is pi_tag
    assert decoy.Comment == 'decoy-comment' and decoy.ProcessingInstruction == 'decoy-pi'
    assert original.PI is pi
    assert type(root) is element and isinstance(root, element)
    assert type(name) is qname and isinstance(name, qname)
    print('cache', sys.modules[module_name] is decoy, original is ET)
    print('saved-globals', first.tag is comment, second.tag is pi, type(first) is element)
    print('original-rebinding', changed.tag is comment_tag, changed_pi.tag is pi_tag, original.PI is pi)
    print('unaffected-types', type(root) is element, type(name) is qname)
finally:
    original.Comment = comment
    original.ProcessingInstruction = pi
    sys.modules[module_name] = original

assert sys.modules[module_name] is ET and ET.Comment is comment and ET.ProcessingInstruction is pi
print('restored', sys.modules[module_name] is ET, ET.PI is pi)
