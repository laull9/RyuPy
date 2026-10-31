# 命令行工具：pyfiglet 的文本渲染。
from pyfiglet import Figlet

renderer = Figlet(font="standard")
art = renderer.renderText("hi").splitlines()
print(len(art), art[0].rstrip())
