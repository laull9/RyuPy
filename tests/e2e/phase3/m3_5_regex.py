# ryupy: pending
# M3.5：Python 命名组、替换反向引用、Unicode 与零宽匹配。
import re

pattern = re.compile(r"(?P<word>\w+)", re.IGNORECASE)
match = pattern.search("猫 alpha")
print(match.group("word"), match.span())
print(pattern.sub(r"[\g<word>]", "a 猫"))
print(re.findall(r"(?=a)", "aa"))
print(re.fullmatch(r"a+", "aaa") is not None)
try:
    re.compile("[")
except re.error:
    print("invalid pattern")
