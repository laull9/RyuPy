# 故意使用同名普通用户包验证源码优先，marker API不伪装标准库Element实现。
import import_events
import xml

assert xml.SOURCE_KIND == 'user-xml'
assert import_events.events == ['xml']
import xml.etree
assert xml.etree.SOURCE_KIND == 'user-etree'
assert import_events.events == ['xml', 'xml.etree']
import xml.etree.ElementTree as ET
from xml import etree as via_package
from xml.etree import ElementTree as via_parent
from xml.etree.ElementTree import make_marker
import xml.etree.ElementTree as repeated

assert ET.SOURCE_KIND == 'user-elementtree'
assert xml.etree is via_package
assert xml.etree.ElementTree is ET
assert via_parent is ET and repeated is ET
assert make_marker is ET.make_marker
assert make_marker('value') == 'user-marker:value'
assert import_events.events == ['xml', 'xml.etree', 'xml.etree.ElementTree']
print('source-markers', xml.SOURCE_KIND, xml.etree.SOURCE_KIND, ET.SOURCE_KIND)
print('aliases', xml.etree is via_package, ET is via_parent, ET is repeated, ET.make_marker is make_marker)
print('side-effects', import_events.events)
print('user-function', make_marker('value'))
