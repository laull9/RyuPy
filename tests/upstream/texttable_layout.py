# texttable 1.7.0 tests.py 原测试子集；MIT 完整许可见 licenses/texttable-1.7.0.txt。
# 仅预计算固定期望字符串的 dedent/u_dedent；原输入、断言与 clean 算法保留。
import re
from texttable import Texttable

def clean(text):
    return re.sub(r'( +)$', '', text, flags=re.MULTILINE) + '\n'

def test_texttable():
    table = Texttable()
    table.set_cols_align(["l", "r", "c"])
    table.set_cols_valign(["t", "m", "b"])
    table.add_rows([
        ["Name", "Age", "Nickname"],
        ["Mr\nXavier\nHuon", 32, "Xav'"],
        ["Mr\nBaptiste\nClement", 1, "Baby"],
        ["Mme\nLouise\nBourgeau", 28, "Lou\n \nLoue"],
    ])
    assert clean(table.draw()) == "+----------+-----+----------+\n|   Name   | Age | Nickname |\n+==========+=====+==========+\n| Mr       |     |          |\n| Xavier   |  32 |          |\n| Huon     |     |   Xav'   |\n+----------+-----+----------+\n| Mr       |     |          |\n| Baptiste |   1 |          |\n| Clement  |     |   Baby   |\n+----------+-----+----------+\n| Mme      |     |   Lou    |\n| Louise   |  28 |          |\n| Bourgeau |     |   Loue   |\n+----------+-----+----------+\n"

def test_set_cols_width():
    table = Texttable()
    table.set_deco(Texttable.HEADER)
    table.set_cols_width([10, 10])
    table.add_rows([
        ["key", "value"],
        [1,     "a"],
        [2,     "b"],
    ])
    assert clean(table.draw()) == '   key         value\n=======================\n1            a\n2            b\n'

def test_exceeding_max_width():
    table = Texttable(max_width=35)
    table.set_deco(Texttable.HEADER)
    table.add_rows([
        ["key", "value"],
        [1,     "a"],
        [2,     "b"],
        [3,     "very long, very long, very long"],
    ])
    assert clean(table.draw()) == 'key               value\n===================================\n1     a\n2     b\n3     very long, very long, very\n      long\n'

def test_exceeding_max_width2():
    table = Texttable(max_width=14)
    table.add_rows([
        ["a", "b"],
        [1, "+"],
        [22, "++++++++"],
    ])
    assert clean(table.draw()) == '+----+-------+\n| a  |   b   |\n+====+=======+\n| 1  | +     |\n+----+-------+\n| 22 | +++++ |\n|    | +++   |\n+----+-------+\n'

def test_exceeding_max_width3():
    table = Texttable()
    table.set_max_width(35)
    table.set_deco(Texttable.HEADER)
    table.add_rows([
        ["key", "value"],
        [1,     "a"],
        [2,     "b"],
        [3,     "very long, very long, very long"],
    ])
    assert clean(table.draw()) == 'key               value\n===================================\n1     a\n2     b\n3     very long, very long, very\n      long\n'

def test_exceeding_max_width4():
    table = Texttable()
    table.set_max_width(14)
    table.add_rows([
        ["a", "b"],
        [1, "+"],
        [22, "++++++++"],
    ])
    assert clean(table.draw()) == '+----+-------+\n| a  |   b   |\n+====+=======+\n| 1  | +     |\n+----+-------+\n| 22 | +++++ |\n|    | +++   |\n+----+-------+\n'

def test_obj2unicode():
    table = Texttable()
    table.set_deco(Texttable.HEADER)
    table.add_rows([
        ["key", "value"],
        [1,     "a"],
        [2,     1],
        [3,     None],
    ])
    assert clean(table.draw()) == 'key   value\n===========\n1     a\n2     1\n3     None\n'

def test_combining_char():
    table = Texttable()
    table.set_cols_align(["l", "r", "r"])
    table.add_rows([
        ["str", "code-point\nlength", "display\nwidth"],
        ["ā", 2, 1],
        ["a", 1, 1],
    ])
    assert clean(table.draw()) == '+-----+------------+---------+\n| str | code-point | display |\n|     |   length   |  width  |\n+=====+============+=========+\n| ā   |          2 |       1 |\n+-----+------------+---------+\n| a   |          1 |       1 |\n+-----+------------+---------+\n'

def test_combining_char2():
    table = Texttable()
    table.add_rows([
        ["a", "b", "c"],
        ["诶诶诶", "bbb", "西西西"],
    ], False)
    assert clean(table.draw()) == '+--------+-----+--------+\n| a      | b   | c      |\n+--------+-----+--------+\n| 诶诶诶 | bbb | 西西西 |\n+--------+-----+--------+\n'

test_texttable()

test_set_cols_width()

test_exceeding_max_width()

test_exceeding_max_width2()

test_exceeding_max_width3()

test_exceeding_max_width4()

test_obj2unicode()

test_combining_char()

test_combining_char2()
