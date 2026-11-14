# 同名普通类及重绑导入的真实继承调用，不开放原生异常派生。
import xml.etree.ElementTree as ET
from xml.etree.ElementTree import ParseError as Imported

class SyntaxError:
    def value(self):
        return 11

class ParseError:
    def value(self):
        return 22

class SyntaxChild(SyntaxError):
    pass

class ParseChild(ParseError):
    pass

Imported = ParseError
class ImportedChild(Imported):
    pass

ET = ParseError
class ModuleChild(ET):
    pass

print('ordinary-syntax', SyntaxChild().value())
print('ordinary-parse', ParseChild().value())
print('rebound-member', ImportedChild().value())
print('rebound-module', ModuleChild().value())
print('ordinary complete')
