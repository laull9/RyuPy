"""固定CPython 3.12.13 Windows词法参考；不触碰Windows文件系统或声称实机验收。"""
import json
import ntpath
from pathlib import PureWindowsPath
import sys

CASES = ['', '.', 'a//./b/../.hidden.txt/', 'file.', '.hidden', '..',
         'C:', 'C:.', 'C:foo', 'C:/', 'C:/a//./b/../.hidden.txt/',
         '\\foo', '\\', '//server/share', '//server/share/a/..',
         '//server/share/dir/file.txt', '中文/文件.txt', './C:foo', '/:a', '/:.', '/:/a', '\\:a']

def reference():
    if sys.version_info[:3] != (3, 12, 13):
        raise RuntimeError('参考必须来自固定 CPython 3.12.13')
    rows = []
    for value in CASES:
        path = PureWindowsPath(value)
        rows.append(dict(input=value, clean=str(path), anchor=path.anchor,
                         name=path.name, parent=str(path.parent), parts=list(path.parts),
                         absolute=path.is_absolute(), os_absolute=ntpath.isabs(value), basename=ntpath.basename(value)))
    return dict(python=sys.version, cases=rows)

if __name__ == '__main__':
    print(json.dumps(reference(), ensure_ascii=False, indent=2))
