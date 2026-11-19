"""冻结 ExplorerPro 原入口按相对路径读取的版本、翻译和图标数据。"""
import json


# 按上游 version.py、translator.py 和 main.load_app_icon() 的路径列出，不递归收集无关资产。
DATA = (
    'pyproject.toml',
    'locales/translations.json',
    'ExplorerPro.ico',
    'DesktopIcon.ico',
    'icon.ico',
    'icon.png',
    'assets/ExplorerPro.ico',
    'assets/icon.ico',
    'assets/icon.png',
)


def bundle_manifest(tree, manifest):
    """固定源少任何一项就失败，不能把缺资源的结果当成产品通过。"""
    for relative in DATA:
        assert (tree / relative).is_file(), relative
    manifest.write_text(json.dumps({
        'schema_version': 1,
        'root': str(tree),
        'files': [{'source': relative} for relative in DATA],
    }, ensure_ascii=False), encoding='utf-8')
    return manifest
