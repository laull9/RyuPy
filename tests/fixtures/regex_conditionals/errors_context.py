"""K3：捕获条件语法错误全字段；另以模块context锁Pattern回调完整用户链。"""
import re


# 错误期待直接取唯一CP参考；不把parser旧re.error帧缺失顺手变成新支持项。
ERRORS = [
    ("K3.text.named", "(?P<_n>a)?(?( _n )b|c)", 0,
     "bad character in group name ' _n ' at position 13", "bad character in group name ' _n '", 13, 1, 14),
    ("K3.bytes.named", b'(?P<_n>a)?(?( _n )b|c)', 0,
     "bad character in group name ' _n ' at position 13", "bad character in group name ' _n '", 13, 1, 14),
    ("K3.text.name-case-sensitive", "(?P<n>a)(?(N)b|c)", 2,
     "unknown group name 'N' at position 11", "unknown group name 'N'", 11, 1, 12),
    ("K3.bytes.name-case-sensitive", b'(?P<n>a)(?(N)b|c)', 2,
     "unknown group name 'N' at position 11", "unknown group name 'N'", 11, 1, 12),
    ("K3.text.error-missing-name", "(?(),", 0,
     "missing group name at position 3", "missing group name", 3, 1, 4),
    ("K3.bytes.error-missing-name", b'(?(),', 0,
     "missing group name at position 3", "missing group name", 3, 1, 4),
    ("K3.text.error-empty-name", "(?())", 0,
     "missing group name at position 3", "missing group name", 3, 1, 4),
    ("K3.bytes.error-empty-name", b'(?())', 0,
     "missing group name at position 3", "missing group name", 3, 1, 4),
    ("K3.text.error-unknown-name", "(?(missing)a|b)", 0,
     "unknown group name 'missing' at position 3", "unknown group name 'missing'", 3, 1, 4),
    ("K3.bytes.error-unknown-name", b'(?(missing)a|b)', 0,
     "unknown group name 'missing' at position 3", "unknown group name 'missing'", 3, 1, 4),
    ("K3.text.error-forward-name", "(?(n)a|b)(?P<n>c)", 0,
     "unknown group name 'n' at position 3", "unknown group name 'n'", 3, 1, 4),
    ("K3.bytes.error-forward-name", b'(?(n)a|b)(?P<n>c)', 0,
     "unknown group name 'n' at position 3", "unknown group name 'n'", 3, 1, 4),
    ("K3.text.error-zero", "()(?(0)a|b)", 0,
     "bad group number at position 5", "bad group number", 5, 1, 6),
    ("K3.bytes.error-zero", b'()(?(0)a|b)', 0,
     "bad group number at position 5", "bad group number", 5, 1, 6),
    ("K3.text.error-zeroes", "()(?(000)a|b)", 0,
     "bad group number at position 5", "bad group number", 5, 1, 6),
    ("K3.bytes.error-zeroes", b'()(?(000)a|b)', 0,
     "bad group number at position 5", "bad group number", 5, 1, 6),
    ("K3.text.error-negative", "()(?(-1)a|b)", 0,
     "bad character in group name '-1' at position 5", "bad character in group name '-1'", 5, 1, 6),
    ("K3.bytes.error-negative", b'()(?(-1)a|b)', 0,
     "bad character in group name '-1' at position 5", "bad character in group name '-1'", 5, 1, 6),
    ("K3.text.error-positive-sign", "()(?(+1)a|b)", 0,
     "bad character in group name '+1' at position 5", "bad character in group name '+1'", 5, 1, 6),
    ("K3.bytes.error-positive-sign", b'()(?(+1)a|b)', 0,
     "bad character in group name '+1' at position 5", "bad character in group name '+1'", 5, 1, 6),
    ("K3.text.error-space-id", "(?x)()(?( 1 )a|b)", 0,
     "bad character in group name ' 1 ' at position 9", "bad character in group name ' 1 '", 9, 1, 10),
    ("K3.bytes.error-space-id", b'(?x)()(?( 1 )a|b)', 0,
     "bad character in group name ' 1 ' at position 9", "bad character in group name ' 1 '", 9, 1, 10),
    ("K3.text.error-underscore-id", "()(?(1_0)a|b)", 0,
     "bad character in group name '1_0' at position 5", "bad character in group name '1_0'", 5, 1, 6),
    ("K3.bytes.error-underscore-id", b'()(?(1_0)a|b)', 0,
     "bad character in group name '1_0' at position 5", "bad character in group name '1_0'", 5, 1, 6),
    ("K3.text.error-mixed-id", "()(?(1a)a|b)", 0,
     "bad character in group name '1a' at position 5", "bad character in group name '1a'", 5, 1, 6),
    ("K3.bytes.error-mixed-id", b'()(?(1a)a|b)', 0,
     "bad character in group name '1a' at position 5", "bad character in group name '1a'", 5, 1, 6),
    ("K3.text.error-unknown-id", "()(?(2)a|b)", 0,
     "invalid group reference 2 at position 5", "invalid group reference 2", 5, 1, 6),
    ("K3.bytes.error-unknown-id", b'()(?(2)a|b)', 0,
     "invalid group reference 2 at position 5", "invalid group reference 2", 5, 1, 6),
    ("K3.text.error-numeric-cap", "()(?(1073741823)a|b)", 0,
     "invalid group reference 1073741823 at position 5", "invalid group reference 1073741823", 5, 1, 6),
    ("K3.bytes.error-numeric-cap", b'()(?(1073741823)a|b)', 0,
     "invalid group reference 1073741823 at position 5", "invalid group reference 1073741823", 5, 1, 6),
    ("K3.text.error-large-id", "()(?(9999999999999999999999999999999999999999)a|b)", 0,
     "invalid group reference 9999999999999999999999999999999999999999 at position 5", "invalid group reference 9999999999999999999999999999999999999999", 5, 1, 6),
    ("K3.bytes.error-large-id", b'()(?(9999999999999999999999999999999999999999)a|b)', 0,
     "invalid group reference 9999999999999999999999999999999999999999 at position 5", "invalid group reference 9999999999999999999999999999999999999999", 5, 1, 6),
    ("K3.text.error-missing-condition-close", "()(?(1", 0,
     "missing ), unterminated name at position 5", "missing ), unterminated name", 5, 1, 6),
    ("K3.bytes.error-missing-condition-close", b'()(?(1', 0,
     "missing ), unterminated name at position 5", "missing ), unterminated name", 5, 1, 6),
    ("K3.text.error-missing-body-close", "()(?(1)a|b", 0,
     "missing ), unterminated subpattern at position 2", "missing ), unterminated subpattern", 2, 1, 3),
    ("K3.bytes.error-missing-body-close", b'()(?(1)a|b', 0,
     "missing ), unterminated subpattern at position 2", "missing ), unterminated subpattern", 2, 1, 3),
    ("K3.text.error-extra-branch", "()(?(1)a|b|c)", 0,
     "conditional backref with more than two branches at position 10", "conditional backref with more than two branches", 10, 1, 11),
    ("K3.bytes.error-extra-branch", b'()(?(1)a|b|c)', 0,
     "conditional backref with more than two branches at position 10", "conditional backref with more than two branches", 10, 1, 11),
    ("K3.text.error-extra-empty-branch", "()(?(1)||)", 0,
     "conditional backref with more than two branches at position 8", "conditional backref with more than two branches", 8, 1, 9),
    ("K3.bytes.error-extra-empty-branch", b'()(?(1)||)', 0,
     "conditional backref with more than two branches at position 8", "conditional backref with more than two branches", 8, 1, 9),
    ("K3.text.error-syntax-before-forward-check", "(?(2)a|b)[", 0,
     "unterminated character set at position 9", "unterminated character set", 9, 1, 10),
    ("K3.bytes.error-syntax-before-forward-check", b'(?(2)a|b)[', 0,
     "unterminated character set at position 9", "unterminated character set", 9, 1, 10),
    ("K3.text.error-extra-branch-before-forward-check", "(?(2)a|b|c)", 0,
     "conditional backref with more than two branches at position 8", "conditional backref with more than two branches", 8, 1, 9),
    ("K3.bytes.error-extra-branch-before-forward-check", b'(?(2)a|b|c)', 0,
     "conditional backref with more than two branches at position 8", "conditional backref with more than two branches", 8, 1, 9),
    ("K3.text.error-escaped-condition-close", "()(?(1\\))a|b)", 0,
     "bad character in group name '1\\\\)' at position 5", "bad character in group name '1\\\\)'", 5, 1, 6),
    ("K3.bytes.error-escaped-condition-close", b'()(?(1\\))a|b)', 0,
     "bad character in group name '1\\\\)' at position 5", "bad character in group name '1\\\\)'", 5, 1, 6),
    ("K3.text.error-dotnet-expression", "(?(?=a)a|b)", 0,
     "bad character in group name '?=a' at position 3", "bad character in group name '?=a'", 3, 1, 4),
    ("K3.bytes.error-dotnet-expression", b'(?(?=a)a|b)', 0,
     "bad character in group name '?=a' at position 3", "bad character in group name '?=a'", 3, 1, 4),
    ("K3.text.error-dotnet-name-delimiter", "(?P<n>a)(?(<n>)b|c)", 0,
     "bad character in group name '<n>' at position 11", "bad character in group name '<n>'", 11, 1, 12),
    ("K3.bytes.error-dotnet-name-delimiter", b'(?P<n>a)(?(<n>)b|c)', 0,
     "bad character in group name '<n>' at position 11", "bad character in group name '<n>'", 11, 1, 12),
    ("K3.text.error-scalar-error-position", "😀()(?(2)a|b)", 0,
     "invalid group reference 2 at position 6", "invalid group reference 2", 6, 1, 7),
    ("K3.bytes.non-ascii-name-reference", b'(?(\xb5)a|b)', 0,
     "bad character in group name '\\xb5' at position 3", "bad character in group name '\\xb5'", 3, 1, 4),
]

for label, pattern, flags, message, msg, position, line, column in ERRORS:
    try:
        re.compile(pattern, flags)
    except re.error as error:
        assert type(error) is re.error and error.pattern is pattern
        assert str(error) == message and error.msg == msg
        assert error.pos == position and error.lineno == line and error.colno == column
        print(label, str(error), error.msg, error.pos, error.lineno, error.colno)
    else:
        raise AssertionError('invalid conditional syntax accepted')

# Pattern.sub无stdlib包装帧；模块级context避免函数内catch/重抛旧债。
compiled = re.compile(br'(?P<n>a)?(?(n)b|c)')


def callback_leaf(match):
    assert match.group('n') == b'a' and match.span() == (0, 2)
    raise LookupError('conditional callback chain failed')


def callback(match):
    return callback_leaf(match)


def middle():
    compiled.sub(callback, b'ab c')


def outer():
    middle()


print('K3.chain', 'ready')
try:
    raise ValueError('conditional outer context')
except ValueError:
    outer()
