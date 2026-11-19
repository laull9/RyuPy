"""父包先初始化，属性绑定使用同一子模块对象。"""
from . import events
from bridge_fixture.native_import_contract import entered

entered(__name__)
events.append('child')
token = object()
