# M3.4：文件用例必须在隔离工作目录运行，异常离开仍关闭流。
import io
import os
from pathlib import Path

text = io.StringIO()
print("猫", file=text, end="!")
print(text.getvalue())
raw = io.BytesIO(b"abc")
raw.seek(1)
print(raw.read())
path = Path("ryupy_file_fixture.txt")
try:
    try:
        with open(path, "w", encoding="utf-8", newline="\n") as stream:
            stream.write("猫\nsecond")
            raise ValueError("leave")
    except ValueError:
        print(stream.closed)
    print(path.read_text(encoding="utf-8").splitlines())
    print(os.path.basename(str(path)), path.exists())
finally:
    path.unlink(missing_ok=True)
