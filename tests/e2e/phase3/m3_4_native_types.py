# Path构造器必须是实际类型对象；absolute只前缀cwd，不消除父目录词法分量。
from pathlib import Path, PosixPath
import os

path = Path("a/../b")
print(isinstance(path, Path), isinstance(path, PosixPath))
print(type(path) is PosixPath, issubclass(PosixPath, Path))
print(Path.__name__, Path.__module__, PosixPath.__name__)
print(isinstance(Path.cwd(), Path), Path.cwd().is_absolute())
print(str(path.absolute()) == str(Path.cwd() / "a/../b"))
absolute = Path("/a/../b")
print(str(absolute.absolute()))
print(os.path.abspath("/a/../b"))
