# ryupy: pending
# 真实zipfile.Path的临时解包与退出删除仍等待zip资源协议实现。
from importlib.resources import as_file
from zipfile import ZipFile, Path
with ZipFile('resources.zip', 'w') as archive:
    archive.writestr('inside.txt', b'zip resource')
with ZipFile('resources.zip') as archive:
    with as_file(Path(archive, 'inside.txt')) as path:
        assert path.read_bytes() == b'zip resource'
        saved = path
    assert not saved.exists()
print('resources-as-file-zip: ok')
