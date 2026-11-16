"""真实普通包，正文只执行一次，子模块必须观察同一状态。"""
from bridge_fixture.native_import_contract import entered

events = ['parent']
entered(__name__)
