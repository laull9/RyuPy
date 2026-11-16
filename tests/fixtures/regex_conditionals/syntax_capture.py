"""K1：固定CP原流的条件分支、编号与捕获回溯；text/bytes分别保真实结果。"""
import re


def snapshot(match):
    if match is None:
        return None
    return (match.group(), match.groups(), match.groupdict(),
            [match.span(i) for i in range(match.re.groups + 1)])


# 每项是label、原pattern、flags、捕获数和有限subject/完整Match期待。
CASES = [
    ("K1.text.basic", "(a)?(?(1)b|c)", 0, 1, [
        ("ab", ("ab", ("a",), {}, [(0, 2), (0, 1)])),
        ("c", ("c", (None,), {}, [(0, 1), (-1, -1)])),
        ("ac", None),
        ("b", None),
    ]),
    ("K1.bytes.basic", b'(a)?(?(1)b|c)', 0, 1, [
        (b'ab', (b'ab', (b'a',), {}, [(0, 2), (0, 1)])),
        (b'c', (b'c', (None,), {}, [(0, 1), (-1, -1)])),
        (b'ac', None),
        (b'b', None),
    ]),
    ("K1.text.no-fallback", "(a)(?(1)b|c)", 0, 1, [
        ("ab", ("ab", ("a",), {}, [(0, 2), (0, 1)])),
        ("ac", None),
    ]),
    ("K1.bytes.no-fallback", b'(a)(?(1)b|c)', 0, 1, [
        (b'ab', (b'ab', (b'a',), {}, [(0, 2), (0, 1)])),
        (b'ac', None),
    ]),
    ("K1.text.omit-no", "(a)?(?(1)b)", 0, 1, [
        ("ab", ("ab", ("a",), {}, [(0, 2), (0, 1)])),
        ("", ("", (None,), {}, [(0, 0), (-1, -1)])),
        ("a", None),
        ("b", None),
    ]),
    ("K1.bytes.omit-no", b'(a)?(?(1)b)', 0, 1, [
        (b'ab', (b'ab', (b'a',), {}, [(0, 2), (0, 1)])),
        (b'', (b'', (None,), {}, [(0, 0), (-1, -1)])),
        (b'a', None),
        (b'b', None),
    ]),
    ("K1.text.empty-yes", "(a)?(?(1)|b)", 0, 1, [
        ("a", ("a", ("a",), {}, [(0, 1), (0, 1)])),
        ("b", ("b", (None,), {}, [(0, 1), (-1, -1)])),
        ("ab", None),
    ]),
    ("K1.bytes.empty-yes", b'(a)?(?(1)|b)', 0, 1, [
        (b'a', (b'a', (b'a',), {}, [(0, 1), (0, 1)])),
        (b'b', (b'b', (None,), {}, [(0, 1), (-1, -1)])),
        (b'ab', None),
    ]),
    ("K1.text.empty-no", "(a)?(?(1)b|)", 0, 1, [
        ("ab", ("ab", ("a",), {}, [(0, 2), (0, 1)])),
        ("", ("", (None,), {}, [(0, 0), (-1, -1)])),
        ("a", None),
    ]),
    ("K1.bytes.empty-no", b'(a)?(?(1)b|)', 0, 1, [
        (b'ab', (b'ab', (b'a',), {}, [(0, 2), (0, 1)])),
        (b'', (b'', (None,), {}, [(0, 0), (-1, -1)])),
        (b'a', None),
    ]),
    ("K1.text.both-empty", "(a)?(?(1)|)", 0, 1, [
        ("a", ("a", ("a",), {}, [(0, 1), (0, 1)])),
        ("", ("", (None,), {}, [(0, 0), (-1, -1)])),
        ("b", None),
    ]),
    ("K1.bytes.both-empty", b'(a)?(?(1)|)', 0, 1, [
        (b'a', (b'a', (b'a',), {}, [(0, 1), (0, 1)])),
        (b'', (b'', (None,), {}, [(0, 0), (-1, -1)])),
        (b'b', None),
    ]),
    ("K1.text.empty-capture", "()(?(1)a|b)", 0, 1, [
        ("a", ("a", ("",), {}, [(0, 1), (0, 0)])),
        ("b", None),
    ]),
    ("K1.bytes.empty-capture", b'()(?(1)a|b)', 0, 1, [
        (b'a', (b'a', (b'',), {}, [(0, 1), (0, 0)])),
        (b'b', None),
    ]),
    ("K1.text.empty-optional", "()?(?(1)a|b)", 0, 1, [
        ("a", ("a", ("",), {}, [(0, 1), (0, 0)])),
        ("b", ("b", (None,), {}, [(0, 1), (-1, -1)])),
        ("", None),
    ]),
    ("K1.bytes.empty-optional", b'()?(?(1)a|b)', 0, 1, [
        (b'a', (b'a', (b'',), {}, [(0, 1), (0, 0)])),
        (b'b', (b'b', (None,), {}, [(0, 1), (-1, -1)])),
        (b'', None),
    ]),
    ("K1.text.empty-star-capture", "(a*)(?(1)b|c)", 0, 1, [
        ("b", ("b", ("",), {}, [(0, 1), (0, 0)])),
        ("aab", ("aab", ("aa",), {}, [(0, 3), (0, 2)])),
        ("c", None),
    ]),
    ("K1.bytes.empty-star-capture", b'(a*)(?(1)b|c)', 0, 1, [
        (b'b', (b'b', (b'',), {}, [(0, 1), (0, 0)])),
        (b'aab', (b'aab', (b'aa',), {}, [(0, 3), (0, 2)])),
        (b'c', None),
    ]),
    ("K1.text.named-valid", "(?P<_n>a)?(?(_n)b|c)", 0, 1, [
        ("ab", ("ab", ("a",), {"_n": "a"}, [(0, 2), (0, 1)])),
        ("c", ("c", (None,), {"_n": None}, [(0, 1), (-1, -1)])),
    ]),
    ("K1.bytes.named-valid", b'(?P<_n>a)?(?(_n)b|c)', 0, 1, [
        (b'ab', (b'ab', (b'a',), {"_n": b'a'}, [(0, 2), (0, 1)])),
        (b'c', (b'c', (None,), {"_n": None}, [(0, 1), (-1, -1)])),
    ]),
    ("K1.text.internal-name-collision", "(?P<ryu2>a)(b)?(?(ryu2)c|d)", 0, 2, [
        ("ac", ("ac", ("a", None), {"ryu2": "a"}, [(0, 2), (0, 1), (-1, -1)])),
        ("abc", ("abc", ("a", "b"), {"ryu2": "a"}, [(0, 3), (0, 1), (1, 2)])),
        ("ad", None),
    ]),
    ("K1.bytes.internal-name-collision", b'(?P<ryu2>a)(b)?(?(ryu2)c|d)', 0, 2, [
        (b'ac', (b'ac', (b'a', None), {"ryu2": b'a'}, [(0, 2), (0, 1), (-1, -1)])),
        (b'abc', (b'abc', (b'a', b'b'), {"ryu2": b'a'}, [(0, 3), (0, 1), (1, 2)])),
        (b'ad', None),
    ]),
    ("K1.text.leading-zero", "(a)?(?(0001)b|c)", 0, 1, [
        ("ab", ("ab", ("a",), {}, [(0, 2), (0, 1)])),
        ("c", ("c", (None,), {}, [(0, 1), (-1, -1)])),
    ]),
    ("K1.bytes.leading-zero", b'(a)?(?(0001)b|c)', 0, 1, [
        (b'ab', (b'ab', (b'a',), {}, [(0, 2), (0, 1)])),
        (b'c', (b'c', (None,), {}, [(0, 1), (-1, -1)])),
    ]),
    ("K1.text.id-100", "()" * 100 + "(?(100)a|b)", 0, 100, [
        ("a", ("a", ("",) * 100, {}, [(0, 1)] + [(0, 0)] * 100)),
        ("b", None),
    ]),
    ("K1.bytes.id-100", b'()' * 100 + b'(?(100)a|b)', 0, 100, [
        (b'a', (b'a', (b'',) * 100, {}, [(0, 1)] + [(0, 0)] * 100)),
        (b'b', None),
    ]),
    ("K1.text.forward-first-false", "(?(1)a|b)(c)", 0, 1, [
        ("bc", ("bc", ("c",), {}, [(0, 2), (1, 2)])),
        ("ac", None),
    ]),
    ("K1.bytes.forward-first-false", b'(?(1)a|b)(c)', 0, 1, [
        (b'bc', (b'bc', (b'c',), {}, [(0, 2), (1, 2)])),
        (b'ac', None),
    ]),
    ("K1.text.forward-later-true", "(?:(?(1)a|b)(c)?){2}", 0, 1, [
        ("bca", ("bca", ("c",), {}, [(0, 3), (1, 2)])),
        ("bb", ("bb", (None,), {}, [(0, 2), (-1, -1)])),
        ("bcb", None),
    ]),
    ("K1.bytes.forward-later-true", b'(?:(?(1)a|b)(c)?){2}', 0, 1, [
        (b'bca', (b'bca', (b'c',), {}, [(0, 3), (1, 2)])),
        (b'bb', (b'bb', (None,), {}, [(0, 2), (-1, -1)])),
        (b'bcb', None),
    ]),
    ("K1.text.forward-no-captures", "(?:(?(1)a|(b))){2}", 0, 1, [
        ("ba", ("ba", ("b",), {}, [(0, 2), (0, 1)])),
        ("bb", None),
    ]),
    ("K1.bytes.forward-no-captures", b'(?:(?(1)a|(b))){2}', 0, 1, [
        (b'ba', (b'ba', (b'b',), {}, [(0, 2), (0, 1)])),
        (b'bb', None),
    ]),
    ("K1.text.forward-state-next-condition", "(?(1)a|(b))(?(1)c|d)", 0, 1, [
        ("bc", ("bc", ("b",), {}, [(0, 2), (0, 1)])),
        ("bd", None),
    ]),
    ("K1.bytes.forward-state-next-condition", b'(?(1)a|(b))(?(1)c|d)', 0, 1, [
        (b'bc', (b'bc', (b'b',), {}, [(0, 2), (0, 1)])),
        (b'bd', None),
    ]),
    ("K1.text.branch-numbering", "(a)?(?(1)(b)|(c))(d)", 0, 4, [
        ("abd", ("abd", ("a", "b", None, "d"), {}, [(0, 3), (0, 1), (1, 2), (-1, -1), (2, 3)])),
        ("cd", ("cd", (None, None, "c", "d"), {}, [(0, 2), (-1, -1), (-1, -1), (0, 1), (1, 2)])),
    ]),
    ("K1.bytes.branch-numbering", b'(a)?(?(1)(b)|(c))(d)', 0, 4, [
        (b'abd', (b'abd', (b'a', b'b', None, b'd'), {}, [(0, 3), (0, 1), (1, 2), (-1, -1), (2, 3)])),
        (b'cd', (b'cd', (None, None, b'c', b'd'), {}, [(0, 2), (-1, -1), (-1, -1), (0, 1), (1, 2)])),
    ]),
    ("K1.text.nested", "(a)?(b)?(?(1)(?(2)c|d)|(?(2)e|f))", 0, 2, [
        ("abc", ("abc", ("a", "b"), {}, [(0, 3), (0, 1), (1, 2)])),
        ("ad", ("ad", ("a", None), {}, [(0, 2), (0, 1), (-1, -1)])),
        ("be", ("be", (None, "b"), {}, [(0, 2), (-1, -1), (0, 1)])),
        ("f", ("f", (None, None), {}, [(0, 1), (-1, -1), (-1, -1)])),
    ]),
    ("K1.bytes.nested", b'(a)?(b)?(?(1)(?(2)c|d)|(?(2)e|f))', 0, 2, [
        (b'abc', (b'abc', (b'a', b'b'), {}, [(0, 3), (0, 1), (1, 2)])),
        (b'ad', (b'ad', (b'a', None), {}, [(0, 2), (0, 1), (-1, -1)])),
        (b'be', (b'be', (None, b'b'), {}, [(0, 2), (-1, -1), (0, 1)])),
        (b'f', (b'f', (None, None), {}, [(0, 1), (-1, -1), (-1, -1)])),
    ]),
    ("K1.text.nested-alternation", "(a)?(?(1)(?:b|c)|(?:d|e))", 0, 1, [
        ("ab", ("ab", ("a",), {}, [(0, 2), (0, 1)])),
        ("ac", ("ac", ("a",), {}, [(0, 2), (0, 1)])),
        ("d", ("d", (None,), {}, [(0, 1), (-1, -1)])),
        ("e", ("e", (None,), {}, [(0, 1), (-1, -1)])),
    ]),
    ("K1.bytes.nested-alternation", b'(a)?(?(1)(?:b|c)|(?:d|e))', 0, 1, [
        (b'ab', (b'ab', (b'a',), {}, [(0, 2), (0, 1)])),
        (b'ac', (b'ac', (b'a',), {}, [(0, 2), (0, 1)])),
        (b'd', (b'd', (None,), {}, [(0, 1), (-1, -1)])),
        (b'e', (b'e', (None,), {}, [(0, 1), (-1, -1)])),
    ]),
    ("K1.text.escaped-pipe", "(a)?(?(1)\\||[|)])", 0, 1, [
        ("a|", ("a|", ("a",), {}, [(0, 2), (0, 1)])),
        ("|", ("|", (None,), {}, [(0, 1), (-1, -1)])),
        (")", (")", (None,), {}, [(0, 1), (-1, -1)])),
    ]),
    ("K1.bytes.escaped-pipe", b'(a)?(?(1)\\||[|)])', 0, 1, [
        (b'a|', (b'a|', (b'a',), {}, [(0, 2), (0, 1)])),
        (b'|', (b'|', (None,), {}, [(0, 1), (-1, -1)])),
        (b')', (b')', (None,), {}, [(0, 1), (-1, -1)])),
    ]),
    ("K1.text.optional-rollback", "(a)?(?(1)b|a)", 0, 1, [
        ("a", ("a", (None,), {}, [(0, 1), (-1, -1)])),
        ("ab", ("ab", ("a",), {}, [(0, 2), (0, 1)])),
    ]),
    ("K1.bytes.optional-rollback", b'(a)?(?(1)b|a)', 0, 1, [
        (b'a', (b'a', (None,), {}, [(0, 1), (-1, -1)])),
        (b'ab', (b'ab', (b'a',), {}, [(0, 2), (0, 1)])),
    ]),
    ("K1.text.retained-capture", "(?:(a)|b){2}(?(1)c|d)", 0, 1, [
        ("abc", ("abc", ("a",), {}, [(0, 3), (0, 1)])),
        ("bbd", ("bbd", (None,), {}, [(0, 3), (-1, -1)])),
        ("bac", ("bac", ("a",), {}, [(0, 3), (1, 2)])),
    ]),
    ("K1.bytes.retained-capture", b'(?:(a)|b){2}(?(1)c|d)', 0, 1, [
        (b'abc', (b'abc', (b'a',), {}, [(0, 3), (0, 1)])),
        (b'bbd', (b'bbd', (None,), {}, [(0, 3), (-1, -1)])),
        (b'bac', (b'bac', (b'a',), {}, [(0, 3), (1, 2)])),
    ]),
    ("K1.text.repeat-rollback", "(?:(a)|(b)){1,2}(?(2)c|b)", 0, 2, [
        ("ab", ("ab", ("a", None), {}, [(0, 2), (0, 1), (-1, -1)])),
        ("abc", ("abc", ("a", "b"), {}, [(0, 3), (0, 1), (1, 2)])),
        ("bb", None),
    ]),
    ("K1.bytes.repeat-rollback", b'(?:(a)|(b)){1,2}(?(2)c|b)', 0, 2, [
        (b'ab', (b'ab', (b'a', None), {}, [(0, 2), (0, 1), (-1, -1)])),
        (b'abc', (b'abc', (b'a', b'b'), {}, [(0, 3), (0, 1), (1, 2)])),
        (b'bb', None),
    ]),
    ("K1.text.lazy-repeat", "(?:(a)|(b))*?(?(2)c|b)", 0, 2, [
        ("ab", ("ab", ("a", None), {}, [(0, 2), (0, 1), (-1, -1)])),
        ("abc", ("abc", ("a", "b"), {}, [(0, 3), (0, 1), (1, 2)])),
        ("b", ("b", (None, None), {}, [(0, 1), (-1, -1), (-1, -1)])),
    ]),
    ("K1.bytes.lazy-repeat", b'(?:(a)|(b))*?(?(2)c|b)', 0, 2, [
        (b'ab', (b'ab', (b'a', None), {}, [(0, 2), (0, 1), (-1, -1)])),
        (b'abc', (b'abc', (b'a', b'b'), {}, [(0, 3), (0, 1), (1, 2)])),
        (b'b', (b'b', (None, None), {}, [(0, 1), (-1, -1), (-1, -1)])),
    ]),
    ("K1.text.lookahead-positive", "(?=(a)?)(?(1)a|b)", 0, 1, [
        ("a", ("a", ("a",), {}, [(0, 1), (0, 1)])),
        ("b", ("b", (None,), {}, [(0, 1), (-1, -1)])),
        ("c", None),
    ]),
    ("K1.bytes.lookahead-positive", b'(?=(a)?)(?(1)a|b)', 0, 1, [
        (b'a', (b'a', (b'a',), {}, [(0, 1), (0, 1)])),
        (b'b', (b'b', (None,), {}, [(0, 1), (-1, -1)])),
        (b'c', None),
    ]),
    ("K1.text.lookahead-atomic", "(?=(a+))(?(1)\\1|b)a", 0, 1, [
        ("aa", None),
        ("aaa", None),
        ("ba", None),
    ]),
    ("K1.bytes.lookahead-atomic", b'(?=(a+))(?(1)\\1|b)a', 0, 1, [
        (b'aa', None),
        (b'aaa', None),
        (b'ba', None),
    ]),
    ("K1.text.lookahead-branch-rollback", "(?:(?=(a))b|a)(?(1)c|d)", 0, 1, [
        ("ad", ("ad", (None,), {}, [(0, 2), (-1, -1)])),
        ("ac", None),
    ]),
    ("K1.bytes.lookahead-branch-rollback", b'(?:(?=(a))b|a)(?(1)c|d)', 0, 1, [
        (b'ad', (b'ad', (None,), {}, [(0, 2), (-1, -1)])),
        (b'ac', None),
    ]),
    ("K1.text.lookahead-negative", "(?!(a)b)a(?(1)c|d)", 0, 1, [
        ("ad", ("ad", (None,), {}, [(0, 2), (-1, -1)])),
        ("ac", None),
        ("ab", None),
    ]),
    ("K1.bytes.lookahead-negative", b'(?!(a)b)a(?(1)c|d)', 0, 1, [
        (b'ad', (b'ad', (None,), {}, [(0, 2), (-1, -1)])),
        (b'ac', None),
        (b'ab', None),
    ]),
    ("K1.text.lookahead-negative-repeat", "(?:(?!(a)b)a|x){2}(?(1)c|d)", 0, 1, [
        ("axd", ("axd", (None,), {}, [(0, 3), (-1, -1)])),
        ("axc", None),
    ]),
    ("K1.bytes.lookahead-negative-repeat", b'(?:(?!(a)b)a|x){2}(?(1)c|d)', 0, 1, [
        (b'axd', (b'axd', (None,), {}, [(0, 3), (-1, -1)])),
        (b'axc', None),
    ]),
    ("K1.text.quantified-condition", "(a)?(?(1)b|c){1,2}", 0, 1, [
        ("abb", ("abb", ("a",), {}, [(0, 3), (0, 1)])),
        ("cc", ("cc", (None,), {}, [(0, 2), (-1, -1)])),
        ("abc", None),
    ]),
    ("K1.bytes.quantified-condition", b'(a)?(?(1)b|c){1,2}', 0, 1, [
        (b'abb', (b'abb', (b'a',), {}, [(0, 3), (0, 1)])),
        (b'cc', (b'cc', (None,), {}, [(0, 2), (-1, -1)])),
        (b'abc', None),
    ]),
    ("K1.text.optional-condition", "(a)?(?(1)b|c)?", 0, 1, [
        ("a", ("a", ("a",), {}, [(0, 1), (0, 1)])),
        ("c", ("c", (None,), {}, [(0, 1), (-1, -1)])),
        ("", ("", (None,), {}, [(0, 0), (-1, -1)])),
    ]),
    ("K1.bytes.optional-condition", b'(a)?(?(1)b|c)?', 0, 1, [
        (b'a', (b'a', (b'a',), {}, [(0, 1), (0, 1)])),
        (b'c', (b'c', (None,), {}, [(0, 1), (-1, -1)])),
        (b'', (b'', (None,), {}, [(0, 0), (-1, -1)])),
    ]),
    ("K1.text.ascii-ignore", "([a-z])?(?(1)X|Y)", 258, 1, [
        ("AX", ("AX", ("A",), {}, [(0, 2), (0, 1)])),
        ("y", ("y", (None,), {}, [(0, 1), (-1, -1)])),
        ("aY", None),
    ]),
    ("K1.bytes.ascii-ignore", b'([a-z])?(?(1)X|Y)', 258, 1, [
        (b'AX', (b'AX', (b'A',), {}, [(0, 2), (0, 1)])),
        (b'y', (b'y', (None,), {}, [(0, 1), (-1, -1)])),
        (b'aY', None),
    ]),
    ("K1.text.unicode-ignore", "(s)?(?(1)X|Y)", 2, 1, [
        ("ſx", ("ſx", ("ſ",), {}, [(0, 2), (0, 1)])),
        ("y", ("y", (None,), {}, [(0, 1), (-1, -1)])),
        ("ſy", None),
    ]),
    ("K1.text.ascii-restrict", "(s)?(?(1)X|Y)", 258, 1, [
        ("ſx", None),
        ("y", ("y", (None,), {}, [(0, 1), (-1, -1)])),
    ]),
    ("K1.text.latin1-literals", "(ÿ)?(?(1)é|z)", 2, 1, [
        ("ÿé", ("ÿé", ("ÿ",), {}, [(0, 2), (0, 1)])),
        ("z", ("z", (None,), {}, [(0, 1), (-1, -1)])),
        ("ÿÉ", ("ÿÉ", ("ÿ",), {}, [(0, 2), (0, 1)])),
    ]),
    ("K1.bytes.latin1-literals", b'(\xff)?(?(1)\xe9|z)', 2, 1, [
        (b'\xff\xe9', (b'\xff\xe9', (b'\xff',), {}, [(0, 2), (0, 1)])),
        (b'z', (b'z', (None,), {}, [(0, 1), (-1, -1)])),
        (b'\xff\xc9', None),
    ]),
    ("K1.text.verbose", "(?x)(a)? (?(1) b # yes\n | c )", 0, 1, [
        ("ab", ("ab", ("a",), {}, [(0, 2), (0, 1)])),
        ("c", ("c", (None,), {}, [(0, 1), (-1, -1)])),
    ]),
    ("K1.bytes.verbose", b'(?x)(a)? (?(1) b # yes\x0a | c )', 0, 1, [
        (b'ab', (b'ab', (b'a',), {}, [(0, 2), (0, 1)])),
        (b'c', (b'c', (None,), {}, [(0, 1), (-1, -1)])),
    ]),
    ("K1.text.scalar-spans", "(😀)?(?(1)猫|犬)", 0, 1, [
        ("😀猫", ("😀猫", ("😀",), {}, [(0, 2), (0, 1)])),
        ("犬", ("犬", (None,), {}, [(0, 1), (-1, -1)])),
        ("😀犬", None),
    ]),
]

for label, pattern, flags, groups, cases in CASES:
    compiled = re.compile(pattern, flags)
    assert compiled.groups == groups and compiled.pattern is pattern
    observed = []
    for subject, expected in cases:
        match = compiled.fullmatch(subject)
        if match is not None:
            assert match.re is compiled and match.string is subject
        actual = snapshot(match)
        assert actual == expected
        observed.append(actual)
    print(label, compiled.groups, observed)
