# 终端：colorama 的 ANSI 常量与 init。
from colorama import Fore, Style, init

init(strip=False)
print(Fore.RED + "red" + Style.RESET_ALL)
print(Style.BRIGHT + "bright" + Style.RESET_ALL)
print(repr(Fore.GREEN))
