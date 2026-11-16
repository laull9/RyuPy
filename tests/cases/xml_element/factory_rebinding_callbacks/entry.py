# ryupy: pending
# 保存的Python工厂读取当前模块绑定；C SubElement不跟随ET.Element重绑。
import xml.etree.ElementTree as ET

comment = ET.Comment
pi = ET.ProcessingInstruction
element = ET.Element
sentinel = object()
events = []

def maker(tag):
    events.append(('make', tag is sentinel))
    return element('replacement')

def raising(tag):
    events.append(('raise', tag is sentinel))
    raise ValueError('factory stopped')

try:
    ET.Comment = sentinel
    ET.ProcessingInstruction = sentinel
    assert comment('note').tag is sentinel
    assert pi('target', 'data').tag is sentinel
    assert ET.PI is pi
    ET.Element = maker
    result = comment('body')
    assert result.tag == 'replacement' and result.text == 'body'
    result_pi = pi('target', 'body')
    assert result_pi.tag == 'replacement' and result_pi.text == 'target body'
    parent = element('parent')
    child = ET.SubElement(parent, 'child')
    assert type(child) is element and parent[0] is child
    assert events == [('make', True), ('make', True)]
    ET.Element = raising
    try:
        comment('never')
    except ValueError as error:
        print('callback-error', type(error).__name__, str(error))
    else:
        raise AssertionError('factory error was hidden')
    print('binding', result.tag, result.text, result_pi.text, type(child) is element)
    print('events', events)
finally:
    ET.Element = element
    ET.Comment = comment
    ET.ProcessingInstruction = pi

assert ET.PI is ET.ProcessingInstruction
print('restored', ET.Element is element, ET.Comment is comment, ET.PI is pi)
