"""CPython 在正文中反向导入本模块，须观察同一半初始化对象。"""
from bridge_fixture.native_import_contract import entered, during_cycle

entered(__name__)
state = ['before']
during_cycle(__name__, state)
state.append('after')
