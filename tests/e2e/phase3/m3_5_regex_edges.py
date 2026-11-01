# 受支持正则子集：Python组编号、Unicode码点、非空重试与替换解析。
import re

print(re.findall(r"\w+", "a\u0301\u203f²猫𝟙"))
print(bool(re.fullmatch(r"\s", "\x1c")))
print(re.search(r"猫", "😀猫").span())
print(re.findall(r".", "😀猫"))
for char in "İıſK":
    print(bool(re.fullmatch("[a-z]", char, re.IGNORECASE)))
print(re.findall("[a-z]+", "ſCat", re.ASCII | re.IGNORECASE))
match = re.match(r"(?P<first>a)(b)(?P<last>c)", "abc")
print(match.group(1, 2, 3), match.groups(), match.groupdict())
print(re.sub(r"(?P<first>a)(b)", r"\2-\g<first>", "ab ab"))
for pattern in [".*?", "|a", "a*", "(?=a)"]:
    print(pattern, re.findall(pattern, "aa"), re.sub(pattern, "-", "aa"))
print(re.findall(r"(a)?b", "b ab"))
print(re.compile("a$").search("a\n").span())
print(re.fullmatch("a$", "a\n") is None)
print(re.findall("^a", "a\na", re.MULTILINE))
print(re.findall(".", "a\nb", re.DOTALL))
print(re.compile("a # comment\n b", re.VERBOSE).fullmatch("ab").group())
