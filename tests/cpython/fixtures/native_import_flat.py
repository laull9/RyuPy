"""普通非包模块的真实首次导入。"""
from bridge_fixture.native_import_contract import entered

entered(__name__)
value = 'flat module'
