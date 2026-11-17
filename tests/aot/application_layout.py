"""发布目录的明确布局标记与路径；新产品集中internal，旧手写宿主保留旧路径合同。"""
from __future__ import annotations
from dataclasses import dataclass
import json
from pathlib import Path

from manifest_files import ManifestError, plain_stat

MARKER = 'internal/.ryupy-layout.json'
CANONICAL = {'schema_version': 1, 'kind': 'ryupy-application-layout',
             'payload_mode': 'canonical-v1'}


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ManifestError('布局标记含重复JSON键')
        result[key] = value
    return result


@dataclass(frozen=True)
class ApplicationLayout:
    root: Path
    canonical: bool
    entry_name: str | None = None
    entry_kind: str | None = None

    @property
    def dependencies(self):
        return self.root / 'internal' if self.canonical else self.root

    @property
    def manifest(self):
        return self.dependencies / 'ryupy-artifact.json'

    @property
    def relative_manifest(self):
        return self.manifest.relative_to(self.root).as_posix()

    def check_public_root(self, entry):
        """依赖存在还不够：新公开根必须仅有入口与一个依赖目录。"""
        if not self.canonical:
            raise ManifestError('本次应用缺少canonical布局标记')
        entry = Path(entry)
        plain_stat(entry)
        if not entry.parent.samefile(self.root):
            raise ManifestError('应用入口未位于公开根')
        if entry.name != self.entry_name:
            raise ManifestError('应用入口与布局标记不一致')
        actual = {path.name for path in self.root.iterdir()}
        if actual != {entry.name, 'internal'}:
            raise ManifestError(f'公开根存在散落项：{sorted(actual)}')

    def check_artifact_paths(self, entries):
        if not self.canonical:
            return
        markers = [row for row in entries if row['role'] == 'application_layout']
        if len(markers) != 1 or markers[0]['relative_path'] != MARKER:
            raise ManifestError('布局标记必须以唯一明确角色列入清单')
        for row in entries:
            path = row['relative_path']
            if path != self.entry_name and not path.startswith('internal/'):
                raise ManifestError(f'清单含散落在公开根的依赖：{path}')


def application_layout(root, *, require_canonical=False):
    root = Path(root).absolute()
    plain_stat(root, directory=True)
    marker = root / MARKER
    if not marker.exists() and not marker.is_symlink():
        if require_canonical:
            raise ManifestError('缺少internal布局标记')
        return ApplicationLayout(root, False)
    plain_stat(root / 'internal', directory=True)
    if plain_stat(marker).st_size > 4096:
        raise ManifestError('布局标记超过读取预算')
    raw = marker.read_bytes()
    if len(raw) > 4096:
        raise ManifestError('布局标记读取期间增长')
    try:
        value = json.loads(raw, object_pairs_hook=unique_object)
    except (ValueError, UnicodeError) as error:
        raise ManifestError('布局标记不是合法JSON') from error
    if (not isinstance(value, dict) or set(value) != set(CANONICAL) | {'entry', 'entry_kind'}
            or any(value[key] != expected for key, expected in CANONICAL.items())
            or type(value['schema_version']) is not int):
        raise ManifestError('未知应用布局版本或模式')
    entry = value['entry']
    if (not isinstance(entry, str) or not entry or Path(entry).name != entry
            or entry in ('.', '..') or entry.lower() == 'internal' or '\\' in entry or '/' in entry
            or any(ord(char) < 32 or 0x7f <= ord(char) <= 0x9f for char in entry)
            or value['entry_kind'] not in ('apphost', 'native_aot')):
        raise ManifestError('布局标记含无效入口')
    return ApplicationLayout(root, True, entry, value['entry_kind'])


def canonical_artifacts(artifacts):
    """结构解析按明确标记记录选择路径；文件核验随后验证真实标记内容。"""
    return isinstance(artifacts, list) and any(
        isinstance(row, dict) and row.get('relative_path') == MARKER
        and row.get('role') == 'application_layout' for row in artifacts)
