"""显式Instance类与26方法业务共享的通用最小owner合同。"""
import base
value = 10
def helper():
    return value
class Parser(base.choose(mode == 'base')):
    body = base.mark('body', mode == 'body')
    field = base.descriptor(mode == 'set-name')
    def read(self, initial=base.mark('default', mode == 'default')):
        return helper(), initial, self.inherited()
    def builtin(self):
        return len([])
