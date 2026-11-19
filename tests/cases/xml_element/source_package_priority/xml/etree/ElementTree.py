# 普通用户同名模块，不实现或冒充标准库XML API。
import import_events
import_events.events.append('xml.etree.ElementTree')
SOURCE_KIND = 'user-elementtree'
def make_marker(value):
    return 'user-marker:' + value
