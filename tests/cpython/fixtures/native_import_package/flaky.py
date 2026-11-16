"""第一次正文失败后撤销缓存，第二次创建新模块并成功。"""
from bridge_fixture.native_import_contract import entered

attempt = entered(__name__)
if attempt == 1:
    raise RuntimeError('native import retry')
token = object()
