"""独立复核已部署Mach-O闭包：真实LC_ID/版本/架构、loader-relative runpath及潜在祖先歧义。

本模块只读产品映像，不使用编译器计划或库名规则。@rpath按当前映像命令顺序取第一命中；
全部拥有映像作为潜在祖先，任何同leaf其它命中均拒绝。外部/未知runpath不能获得豁免。
"""
from dataclasses import dataclass
from pathlib import Path
import platform
import re
import subprocess


MAGIC = {b'\xcf\xfa\xed\xfe', b'\xfe\xed\xfa\xcf', b'\xce\xfa\xed\xfe',
         b'\xfe\xed\xfa\xce', b'\xca\xfe\xba\xbe', b'\xbe\xba\xfe\xca',
         b'\xca\xfe\xba\xbf', b'\xbf\xba\xfe\xca'}
DYLIB = re.compile(r'^(.+) \(compatibility version (\d+\.\d+\.\d+), current version (\d+\.\d+\.\d+)(?:,.*)?\)$')


@dataclass(frozen=True)
class Library:
    name: str
    compatibility: tuple
    current: tuple


@dataclass(frozen=True)
class Image:
    identity: object
    dependencies: tuple
    rpaths: tuple
    architectures: frozenset


def parse_image(loads, identities, commands, architectures):
    """解析otool真实字段；ID仅跳过一次，后续同名self-load仍是加载边。"""
    ids = [line.strip() for line in identities.splitlines()[1:] if line.strip()]
    assert len(ids) <= 1, ('多个LC_ID', ids)
    identity, dependencies = None, []
    for line in loads.splitlines():
        match = DYLIB.fullmatch(line.strip())
        if not match:
            assert 'compatibility version' not in line, ('损坏依赖版本行', line)
            continue
        name, compatibility, current = match.groups()
        lib = Library(name, tuple(map(int, compatibility.split('.'))), tuple(map(int, current.split('.'))))
        if ids and name == ids[0] and identity is None:
            identity = lib
        else:
            dependencies.append(lib)
    assert not ids or identity is not None, ('LC_ID缺少版本', ids)
    rpaths, active = [], False
    for line in commands.splitlines():
        line = line.strip()
        if line.startswith('cmd '):
            active = line == 'cmd LC_RPATH'
        elif active and line.startswith('path '):
            value, separator, offset = line[5:].rpartition(' (offset ')
            assert separator and offset.endswith(')'), ('损坏LC_RPATH', line)
            rpaths.append(value)
            active = False
    assert architectures, '原生映像没有架构'
    return Image(identity, tuple(dependencies), tuple(rpaths), frozenset(architectures))


def read_image(path):
    """按本机实际slice读取装载命令；同时保留lipo架构事实供加载边ABI核验。"""
    machine = platform.machine().lower()
    arch = 'arm64' if machine in ('arm64', 'aarch64') else 'x86_64'
    def query(flag):
        result = subprocess.run(['/usr/bin/otool', '-arch', arch, flag, str(path)],
                                capture_output=True, check=True, timeout=60)
        return result.stdout.decode('utf-8')
    architectures = subprocess.run(['/usr/bin/lipo', '-archs', str(path)], capture_output=True,
                                   check=True, timeout=60).stdout.decode().split()
    assert arch in architectures, ('当前平台架构不在映像中', path, architectures)
    return parse_image(query('-L'), query('-D'), query('-l'), architectures)


def loader_path(image, value, root, *, must_exist=True):
    """只接受真实loader相对根，规范解析后仍须在本次部署树中。"""
    assert value == '@loader_path' or value.startswith('@loader_path/'), ('未知/外部runpath', image, value)
    path = (image.parent / value[len('@loader_path'):].lstrip('/')).resolve(strict=must_exist)
    assert path.is_relative_to(root), ('loader路径逃出部署根', image, value)
    return path


def audit_images(root, images):
    """对冻结拥有集证明每条边的唯一目标；不从文件后缀推断映像身份。"""
    root = Path(root).resolve(strict=True)
    images = {Path(path).resolve(strict=True): info for path, info in images.items()}
    roots = {}
    for path, info in images.items():
        assert path.is_relative_to(root), ('拥有映像逃出部署根', path)
        # dyld可跳过不存在的搜索根；规范后仍须内部，实际加载边另证第一命中。
        local = [loader_path(path, value, root, must_exist=False) for value in info.rpaths]
        assert all(not directory.exists() or directory.is_dir() for directory in local), ('runpath不是目录', path)
        roots[path] = local
    all_roots = [directory for values in roots.values() for directory in values]
    for path, info in images.items():
        for dependency in info.dependencies:
            name = dependency.name
            if name.startswith(('/usr/lib/', '/System/Library/')):
                continue
            if name.startswith('@loader_path/'):
                target = loader_path(path, name, root)
            elif name.startswith('@rpath/'):
                leaf = name[len('@rpath/'):]
                assert leaf and not Path(leaf).is_absolute() and all(part not in ('.', '..') for part in leaf.split('/')), ('非法rpath后缀', path, name)
                hits = []
                for directory in roots[path]:
                    candidate = directory / leaf
                    if candidate.exists():
                        hits.append(candidate.resolve(strict=True))
                        break
                assert hits, ('rpath缺少当前映像第一命中', path, name)
                target = hits[0]
                for directory in all_roots:
                    candidate = directory / leaf
                    if candidate.exists():
                        assert candidate.resolve(strict=True) == target, ('潜在祖先rpath命中冲突', path, name, candidate, target)
                assert target in images and images[target].identity and images[target].identity.name == name, ('rpath目标LC_ID不匹配', path, name, target)
            else:
                raise AssertionError(('分发之外的原生加载边', path, name))
            assert target.is_relative_to(root) and target in images, ('加载目标未由部署树拥有', path, name, target)
            actual = images[target]
            assert actual.identity, ('加载目标没有LC_ID', path, name, target)
            assert actual.identity.name.startswith(('@rpath/', '@loader_path/')), ('加载目标LC_ID不可搬迁', target, actual.identity)
            assert actual.identity.current >= dependency.compatibility, ('加载边ABI兼容版本不足', path, dependency, target, actual.identity)
            assert info.architectures <= actual.architectures, ('加载目标缺少调用方架构', path, target)


def audit_macho_paths(dist):
    """扫描一次真实拥有集合，再统一证明本地与潜在祖先runpath闭包。"""
    dist = Path(dist).resolve(strict=True)
    images = {}
    for path in dist.rglob('*'):
        if not path.is_file():
            continue
        with path.open('rb') as stream:
            if stream.read(4) not in MAGIC:
                continue
        canonical = path.resolve(strict=True)
        assert canonical.is_relative_to(dist), ('映像链接逃出部署树', path)
        if canonical not in images:
            images[canonical] = read_image(canonical)
    audit_images(dist, images)
