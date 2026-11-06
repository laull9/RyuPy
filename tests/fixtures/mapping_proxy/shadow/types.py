# 本地types源码必须压过同名原生注册；不依赖真实types的任何其它API。
origin = 'mappingproxy-shadow-source'
calls = []
def MappingProxyType(mapping):
    calls.append(mapping['value'])
    return ('local', mapping['value'])
