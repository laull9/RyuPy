# 合法同名源码模块；其导出不应被原生 codecs 适配器替换。
BOM_UTF8 = b'local-bom'
marker = 'source-codecs'

def encode(value):
    return marker + ':' + value
