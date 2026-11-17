"""真实Cocoa持续交互门：原QApplication([])、计时心跳、真实鼠标/回车及原生关闭信号进入CIL。"""
import json
from pathlib import Path
import sys
import time
from PySide6.QtCore import QTimer
from PySide6.QtWidgets import QApplication, QLabel, QLineEdit, QPushButton, QVBoxLayout, QWidget

profile, evidence = map(lambda value: Path(value).resolve(), sys.argv[1:3])
assert profile.is_dir() and evidence.is_dir()
app = QApplication([])
app.setApplicationName('RyuPy Cocoa Interaction')
window = QWidget()
window.setWindowTitle('RyuPy Cocoa 交互验收 - ' + evidence.name)
window.resize(520, 260)
layout = QVBoxLayout(window)
status = QLabel('等待事件循环心跳')
button = QPushButton('验证真实鼠标点击')
editor = QLineEdit()
editor.setPlaceholderText('输入 RyuPy event loop 42 后按回车')
result = QLabel('等待真实键盘输入')
layout.addWidget(status)
layout.addWidget(button)
layout.addWidget(editor)
layout.addWidget(result)
state = {'heartbeats': 0, 'clicks': 0, 'text': '', 'submitted': False,
         'last_window_closed': False, 'about_to_quit': False, 'timeout': False,
         'gaps': [], 'events': []}
last_tick = time.monotonic()

def save_state():
    (evidence / 'interactive-state.json').write_text(json.dumps(state, ensure_ascii=False, indent=2))

def heartbeat():
    global last_tick
    now = time.monotonic()
    state['gaps'].append(now - last_tick)
    last_tick = now
    state['heartbeats'] += 1
    status.setText('事件循环心跳 ' + str(state['heartbeats']))
    save_state()

def clicked():
    state['clicks'] += 1
    state['events'].append('native-click-to-cil')
    button.setText('鼠标回调成功 ' + str(state['clicks']))
    save_state()

def submitted():
    state['text'] = editor.text()
    state['submitted'] = state['text'] == 'RyuPy event loop 42'
    state['events'].append('native-return-to-cil')
    result.setText('键盘回调成功：' + state['text'])
    assert window.grab().save(str(evidence / 'interactive-window.png'))
    save_state()

def closed():
    state['last_window_closed'] = True
    state['events'].append('native-close-to-cil')
    save_state()

def quitting():
    state['about_to_quit'] = True
    state['events'].append('native-quit-to-cil')
    save_state()

def timeout():
    state['timeout'] = True
    save_state()
    window.close()

button.clicked.connect(clicked)
editor.returnPressed.connect(submitted)
app.lastWindowClosed.connect(closed)
app.aboutToQuit.connect(quitting)
timer = QTimer()
timer.timeout.connect(heartbeat)
timer.start(100)
QTimer.singleShot(120000, timeout)
window.show()
window.raise_()
window.activateWindow()
save_state()
app.exec()
assert state['heartbeats'] >= 20, state
assert state['clicks'] >= 1 and state['submitted'], state
assert state['last_window_closed'] and state['about_to_quit'] and not state['timeout'], state
assert max(state['gaps'][1:]) < 3.0, state
print(json.dumps({'platform': app.platformName(), 'heartbeat': True, 'mouse': True,
                  'keyboard': True, 'native_close': True}), flush=True)
