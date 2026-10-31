# 终端：termcolor 的属性组合（非 tty 时应返回纯文本）。
from termcolor import colored

print(repr(colored("warn", "yellow")))
print(repr(colored("warn", "yellow", attrs=["bold"])))
print(repr(colored("plain", None)))
