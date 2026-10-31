# 文本与数据处理：chardet 的编码识别。
from chardet import detect

result = detect("café".encode("utf-8"))
print(result["encoding"], result["confidence"])
print(detect(b"\xff\xfe\x00")["encoding"])
