# 父模块经别名重绑后，完整属性链必须读取实际普通类，不能冻结原生异常身份。
import xml.etree.ElementTree

class Ordinary:
    def value(self):
        return 33

class FakeET:
    ParseError = Ordinary

alias = xml.etree
alias.ElementTree = FakeET

class Child(xml.etree.ElementTree.ParseError):
    pass

print('mutable-parent', Child().value())
print('mutable parent complete')
