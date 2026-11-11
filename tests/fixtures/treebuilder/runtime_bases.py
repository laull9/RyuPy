# 原生异常经别名及容器选择后仍按真实对象拒绝；这是NI合同而非CP等价。
import xml.etree.ElementTree as ET

SyntaxAlias = SyntaxError
Base = [SyntaxAlias][0]
print('before syntax-container')
try:
    class SyntaxChild(Base):
        pass
except NotImplementedError as error:
    assert str(error) == 'SyntaxError/ParseError user inheritance is not supported'
    print('NI syntax-container', str(error))
else:
    raise AssertionError('native SyntaxError base accepted')
print('after syntax-container')

ParseAlias = ET.ParseError
Base = [ParseAlias][0]
print('before parse-container')
try:
    class ParseChild(Base):
        pass
except NotImplementedError as error:
    assert str(error) == 'SyntaxError/ParseError user inheritance is not supported'
    print('NI parse-container', str(error))
else:
    raise AssertionError('native ParseError base accepted')
print('after parse-container')
# 完整无别名链经可变父模块读取真实原生对象，仍由运行时守卫拒绝。
import xml.etree.ElementTree
print('before dotted-parse')
try:
    class DottedChild(xml.etree.ElementTree.ParseError):
        pass
except NotImplementedError as error:
    assert str(error) == 'SyntaxError/ParseError user inheritance is not supported'
    print('NI dotted-parse', str(error))
else:
    raise AssertionError('native dotted ParseError base accepted')
print('after dotted-parse')
print('runtime bases complete')
