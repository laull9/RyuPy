"""最小真实 Cocoa 回归：事件过滤器未使用的包装不能改变稍后返回对象的真实类型。"""
from PySide6.QtCore import QCoreApplication, QEvent, QObject
from PySide6.QtWidgets import QApplication, QMainWindow, QMenu, QMenuBar, QWidget


class Observer(QObject):
    def __init__(self, parent):
        super().__init__(parent)
        self.saved = []
        self.readers = []

    def eventFilter(self, watched, event):
        # ChildAdded 等构造事件只判断类型，不使用或保存 watched。
        if event.type() == QEvent.Type.Show:
            watched.objectName()
        elif event.type() == QEvent.Type.User:
            # 存入容器和捕获到闭包均要求真实对象逃逸，不允许保存已过期的借值。
            self.saved.append(watched)
            self.retain(watched)
        return False

    def retain(self, value):
        self.readers.append(lambda: value)


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
    victim = QWidget()
    victim.setObjectName('saved receiver')
    QCoreApplication.sendEvent(victim, QEvent(QEvent.Type.User))
    assert observer.saved[-1] is victim and observer.readers[-1]() is victim
    del victim
    assert observer.readers[-1]().objectName() == 'saved receiver'
finally:
    app.removeEventFilter(observer)

print('构造期事件过滤与菜单类型通过')
