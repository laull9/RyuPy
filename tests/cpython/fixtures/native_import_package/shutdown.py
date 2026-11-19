"""首次导入发生在 atexit；CIL 与原生导入类型仍须可用。"""
from bridge_fixture.native_import_contract import entered

entered(__name__)
value = 'shutdown import alive'
