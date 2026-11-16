# ryupy: pending
# 锁定CPython 3.12的真实stdlib消费者，不复制算法，不代表Markdown项目通过。
from _markupbase import ParserBase


class Parser(ParserBase):
    pass


parser = Parser()
parser.rawdata = '😀a\nb😀\nc'
parser.reset()
assert parser.getpos() == (1, 0)
assert parser.updatepos(0, 2) == 2
assert parser.getpos() == (1, 2)
assert parser.updatepos(2, 3) == 3
assert parser.getpos() == (2, 0)
assert parser.updatepos(3, 7) == 7
assert parser.getpos() == (3, 1)
assert parser.updatepos(7, 7) == 7
assert parser.getpos() == (3, 1)
parser.reset()
assert parser.getpos() == (1, 0)
assert parser.updatepos(0, 7) == 7
assert parser.getpos() == (3, 1)
print('text search markupbase')
