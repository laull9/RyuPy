# ryupy: pending
# 固定CP3.12.13的24输入合同；产品差分由专用runner执行，不提前转正。
import textwrap

cases = [
    ('empty', '', '', True),
    ('plain_no_change', 'plain payload', 'plain payload', True),
    ('spaces_only', '   ', '', False),
    ('tabs_only_lf', '\t\t\n', '\n', False),
    ('trailing_blank_no_lf', '  alpha\n   ', 'alpha\n', False),
    ('blank_middle', '  alpha\n \t \n    beta\n', 'alpha\n\n  beta\n', False),
    ('space_depth', '  a\n    b\n  c', 'a\n  b\nc', False),
    ('tab_depth', '\ta\n\t\tb', 'a\n\tb', False),
    ('space_tab_no_common', ' a\n\tb', ' a\n\tb', True),
    ('space_tab_common_prefix', ' \ta\n  b', '\ta\n b', False),
    ('crlf_indented_cr', '  \r\n  x', '\r\nx', False),
    ('crlf_unindented_cr', '\r\n  x', '\r\n  x', True),
    ('bare_cr_not_linebreak', '  a\r  b', 'a\r  b', False),
    ('nbsp_no_indent', '\xa0a\n  b', '\xa0a\n  b', True),
    ('nbsp_after_space', ' \xa0a\n b', '\xa0a\nb', False),
    ('ideographic_space', '\u3000a\n  b', '\u3000a\n  b', True),
    ('vt_after_indent', '  \x0b\n  x', '\x0b\nx', False),
    ('ff_after_indent', '  \x0c\n  x', '\x0c\nx', False),
    ('nel_not_linebreak', '  a\x85  b', 'a\x85  b', False),
    ('line_sep_not_linebreak', '  a\u2028  b', 'a\u2028  b', False),
    ('para_sep_not_linebreak', '  a\u2029  b', 'a\u2029  b', False),
    ('emoji_cjk', '  \U0001f600\n    \u4e2d', '\U0001f600\n  \u4e2d', False),
    ('trailing_blank_no_margin', 'x\n \t', 'x\n', False),
    ('cr_only_after_indent', '  \r', '\r', False),
]
for name, text, expected, identity in cases:
    result = textwrap.dedent(text)
    assert result == expected
    assert type(result) is str
    if identity:
        assert result is text
    print("core", name, repr(result))
print("CORE_OK", len(cases))
