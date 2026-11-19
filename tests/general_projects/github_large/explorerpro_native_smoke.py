"""真实Cocoa最小入口：空argv保持原QApplication调用形态，验证C扩展能读取当前调用现场。"""
import json
from pathlib import Path
import sys
import traceback
from PySide6.QtCore import QTimer
from PySide6.QtWidgets import QApplication, QLabel
profile, evidence = map(lambda p: Path(p).resolve(), sys.argv[1:3])
assert profile.is_dir() and evidence.is_dir()
app = QApplication([])
assert app.platformName() == 'cocoa'
window = QLabel('RyuPy原生调用现场')
window.resize(360, 120)
window.show()
failures = []
inspected = []
def inspect_window():
    try:
        assert window.isVisible() and window.windowHandle().isExposed()
        assert window.grab().save(str(evidence / 'native-caller.png'))
        print(json.dumps({'platform': app.platformName(), 'visible': True, 'exposed': True}), flush=True)
        inspected.append(True)
    except BaseException:
        failures.append(traceback.format_exc())
    finally:
        window.close()
        app.quit()
QTimer.singleShot(300, inspect_window)
QTimer.singleShot(10000, app.quit)
app.exec()
assert inspected and not failures, failures
