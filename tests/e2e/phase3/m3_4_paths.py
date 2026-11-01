# 文件系统副作用只发生在框架隔离工作目录内，最终清除本用例创建的全部路径。
import os
from pathlib import Path

root = Path("ryupy_paths_fixture")
root.mkdir()
try:
    nested = root / "nested"
    nested.mkdir()
    path = nested.joinpath("sample.data.txt")
    print(path.name, path.stem, path.suffix, path.parent == nested)
    print(path.write_text("猫\nline", encoding="utf-8"))
    print(path.read_text(encoding="utf-8"))
    print(path.is_file(), nested.is_dir(), path.exists())
    print(sorted(os.listdir(nested)))
    print(sorted([p.name for p in nested.iterdir()]))
    print(sorted([p.name for p in nested.glob("*.txt")]))
    print(os.path.basename(str(path)), os.path.splitext(path.name))
    print(os.path.normpath("a//b/../c/"), os.path.join("a", "b", "c"))
    print(os.path.dirname("a/b/"), os.path.basename("a/b/"))
    renamed = path.rename(nested / "new.txt")
    print(renamed.name, path.exists())
    raw = nested / "data.bin"
    print(raw.write_bytes(b"abc"), raw.read_bytes())
    print(os.fspath(raw))
    raw.unlink()
    renamed.unlink()
    try:
        renamed.unlink()
    except FileNotFoundError:
        print("missing")
    renamed.unlink(missing_ok=True)
    nested.rmdir()
finally:
    root.rmdir()
