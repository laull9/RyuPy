# 文件自动化：binaryornot 的文本 / 二进制判定。
from binaryornot.check import is_binary

with open("text_sample.txt", "w", encoding="utf-8") as handle:
    handle.write("plain text\n")

with open("binary_sample.bin", "wb") as handle:
    handle.write(b"\x00\x01\x02\xff")

print(is_binary("text_sample.txt"), is_binary("binary_sample.bin"))
