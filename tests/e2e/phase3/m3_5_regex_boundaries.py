# 常用模式的Unicode分类/零宽边界矩阵，确保没有UTF16中间位置或.NET方言渗漏。
import re

patterns = [r"\b", r"\B", r"\w+", r"\W+", r"\d+", r"\D+", r"\s+", r"\S+",
            r"[^\w]", r"[^😀]", r"a{,2}", r"a{,}", r"\100", r"(a)?b", r"a|ab", r"a+?", r"(?P<x>a)(?P=x)"]
texts = ["", "aa", "a\n", "😀a😀", "éİıſK", "²_́‿١", " \x1c\t"]
for pattern in patterns:
    print(pattern, [re.findall(pattern, text) for text in texts])
for pattern in [r"[a-z]", r"[^a-z]", r"[İ]", r"\w"]:
    print(pattern, re.findall(pattern, "aİıſK", re.I), re.findall(pattern, "aİıſK", re.I | re.A))
print([(m.group(), m.span()) for m in re.finditer(r".*?", "😀a")])
compiled = re.compile(r"a*")
for pos, endpos in [(0, 0), (1, 3), (10, 10), (-2, 1)]:
    match = compiled.fullmatch("aaa", pos, endpos)
    print(None if match is None else (match.span(), match.pos, match.endpos))
print(re.sub("a", lambda m: m.group().upper(), "a a"))
print(re.subn(r"x*", "-", "abxd", count=2))
for pattern in [r"\A*", r"[\8]", r"(?<name>a)", r"(?P<x>a)(?P<x>b)", r"\q"]:
    try:
        re.compile(pattern)
    except re.error:
        print("invalid", pattern)

error = re.error("bad")
print(error.msg, error.pattern, error.pos, error.lineno, error.colno, error.args, str(error))
error = re.error("broken", "a\nb", 2)
print(error.msg, error.pattern, error.pos, error.lineno, error.colno, error.args, str(error))
try:
    raise re.error(msg="fail", pattern="😀x", pos=1)
except re.error as error:
    print(error.pos, error.lineno, error.colno, error.args)
try:
    re.sub("x", 1, "no")
except TypeError:
    print("invalid replacement")
# IGNORECASE只扩展类内字面量，不改变CATEGORY操作的Unicode分类。
for pattern in [r"[\w]", r"[\W]", r"[^\w]", r"[\wι]", r"[\Wι]"]:
    print(pattern, [bool(re.fullmatch(pattern, char, re.I)) for char in "\u0345ιΙ"])
# None回调为删去匹配，但仍计次数；回调顺序与每次match公开位置一致。
seen = []
def erase(match):
    seen.append(match.span())
    return None
print(re.sub('a', erase, 'aXa'), seen)
seen.clear()
print(re.subn('a', erase, 'aXa'), seen)
