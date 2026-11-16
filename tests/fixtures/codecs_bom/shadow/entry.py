# 源码遮蔽独立目录；CP 启动器仅排除启动期间预载的标准 codecs。
import codecs
from codecs import BOM_UTF8, encode
assert codecs.marker == 'source-codecs'
assert BOM_UTF8 == b'local-bom'
assert encode('payload') == 'source-codecs:payload'
assert encode is codecs.encode
codecs.BOM_UTF8 = b'rebound-local'
assert BOM_UTF8 == b'local-bom'
assert codecs.BOM_UTF8 == b'rebound-local'
print('B2.source', codecs.marker, BOM_UTF8, encode('payload'))
print('B2.complete', True)
