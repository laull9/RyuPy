# ryupy: pending
# 自定义Traversable由CP实际解包临时文件；RyuPy明确拒绝此待实现域。
from importlib.resources import as_file
class Resource:
    name = 'custom.txt'
    def is_dir(self): return False
    def read_bytes(self): return b'custom resource'
with as_file(Resource()) as path:
    assert path.read_bytes() == b'custom resource'
    saved = path
assert not saved.exists()
print('resources-as-file-traversable: ok')
