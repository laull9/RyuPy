"""CPython3.12.13有限extended DOS/UNC词法参考；不声称支持其他设备命名空间。"""
import json
import ntpath
import platform
import sys
from pathlib import PureWindowsPath, Path
assert platform.python_version() == '3.12.13'
paths = ['\\\\?\\C:\\', r'\\?\C:\a\file.', r'\\?\C:\a \b ', r'\\?\C:\a\.\..\b',
         r'\\?\C:\中文 &\file.txt', r'\\?\UNC\server\share', r'\\?\UNC\server\share\dir\..',
         r'\\?\UNC\server\share\tail. ', r'\\?\unc\server\share\file']
joins = [(r'\\?\C:\a', r'\b'), (r'\\?\C:\a', 'C:b'), (r'C:\a', r'\\?\C:\b'),
         (r'\\?\C:\a', 'b'), (r'\\?\UNC\server\share\a', r'\b'),
         (r'\\?\UNC\server\share', 'b'), (r'\\?\C:\a', '')]
paths += ['//?/C:/a', '\\\\?\\C:/a', '//?/UNC/server/share', '//?/unc/server/share/dir', '\\\\?\\UNC/server/share/a']
joins += [('//?/C:/a', '/b'), ('//?/UNC/server/share', 'b'), ('\\\\?\\C:/a', 'b'), ('//?/unc/server/share/a', '/b')]
rows=[]
for text in paths:
 p=PureWindowsPath(text)
 rows.append([text,str(p),p.anchor,p.name,str(p.parent),p.is_absolute(),'|'.join(p.parts),ntpath.basename(text),ntpath.dirname(text)])
result = {'python':platform.python_version(),'paths':rows,'joins':[[*p,ntpath.join(*p)] for p in joins]}
if sys.argv[1:] == ['--check']:
    assert result == json.loads(Path(__file__).with_suffix('.json').read_text(encoding='utf-8'))
    print('extended DOS/UNC CPython3.12.13 reference matched')
else:
    assert not sys.argv[1:]
    print(json.dumps(result,ensure_ascii=False,indent=2))
