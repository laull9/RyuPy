# 包资源入口贯穿files/joinpath/as_file，文件、目录、缺失路径都保持原Path身份。
import assetpkg
from importlib.resources import files, as_file

root = files(assetpkg)
p = root.joinpath('data.txt')
for path in [root, p, root.joinpath('missing.txt')]:
    with as_file(path) as actual:
        assert actual is path
        if actual.is_file():
            print(actual.read_text(encoding='utf-8'), end='')
try:
    with as_file(p) as actual:
        assert actual is p
        raise ValueError('资源体异常')
except ValueError as exc:
    assert str(exc) == '资源体异常'
assert p.is_file() and p.read_text(encoding='utf-8') == '原生资源 & Path\n'
assert not root.joinpath('missing.txt').exists()
print('resources-as-file-package: ok')
