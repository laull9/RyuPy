# ryupy: pending
"""最小真实 Cocoa 回归：事件过滤器未使用的包装不能改变稍后返回对象的真实类型。"""
from PySide6.QtCore import QEvent, QObject
from PySide6.QtWidgets import QApplication, QMainWindow, QMenu, QMenuBar


class Observer(QObject):
    def eventFilter(self, watched, event):
        # ChildAdded 等构造事件只判断类型，不使用或保存 watched。
        if event.type() == QEvent.Type.Show:
            watched.objectName()
        return False


app = QApplication([])
assert app.platformName() == 'cocoa'
observer = Observer(app)
app.installEventFilter(observer)
try:
    for _ in range(5):
        window = QMainWindow()
        bar = window.menuBar()
        assert isinstance(bar, QMenuBar), type(bar)
        menu = bar.addMenu('Native menu')
        assert isinstance(menu, QMenu), type(menu)
        assert menu.addSeparator().isSeparator()
        window.close()
finally:
    app.removeEventFilter(observer)

print('构造期事件过滤与菜单类型通过')
