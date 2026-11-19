"""真实 Cocoa ExplorerProApp 薄驱动：只隔离路径/发现范围/剪贴板，调用原业务。

全部 monkeypatch 集中在业务对象创建前：Path.home、Qt 配置/驱动根、云发现、
剪贴板拒绝和导航根防线。UI、SQLite、预览、线程实现保持原版，退出检查原清理结果。
"""
import builtins
import gc
import json
from pathlib import Path
import sqlite3
import sys
import threading
import time
import traceback

profile, evidence = map(lambda p: Path(p).resolve(), sys.argv[1:3])


def startup_phase(name):
    """记录真实单调时钟，给外部驱动拆分进程启动与窗口创建时间。"""
    now = time.monotonic_ns()
    with (evidence / 'startup.tsv').open('a', encoding='utf-8') as stream:
        stream.write(f'{name}\t{now}\n')


startup_phase('entry')
source = Path.cwd()
assert sys.version_info[:3] == (3, 12, 13)
assert profile.is_dir() and profile != Path('/Users/laull')
Path.home = classmethod(lambda cls: profile)
sys.path[:0] = [str(source / 'src'), str(source)]
from PySide6 import __version__ as pyside_version
from PySide6.QtCore import (QDir, QFileInfo, QSettings, QStandardPaths, QThread,
                            QThreadPool, QTimer, QCoreApplication, QEvent, qVersion)
from PySide6.QtWidgets import QApplication
from PySide6.QtTest import QTest
startup_phase('qt-imports')

# macOS QStandardPaths 不完全服从 HOME；明确覆盖业务使用的路径解析。
def private_location(location):
    target = profile / 'qt-paths' / str(location.value)
    target.mkdir(parents=True, exist_ok=True)
    return str(target)
QStandardPaths.writableLocation = staticmethod(private_location)
QStandardPaths.standardLocations = staticmethod(lambda location: [private_location(location)])
QDir.homePath = staticmethod(lambda: str(profile))
QDir.drives = staticmethod(lambda: [QFileInfo(str(profile))])
QSettings.setDefaultFormat(QSettings.Format.IniFormat)
for scope in (QSettings.Scope.UserScope, QSettings.Scope.SystemScope):
    QSettings.setPath(QSettings.Format.IniFormat, scope, str(profile / 'qsettings'))
def forbidden_clipboard(*args):
    raise RuntimeError('本合同禁止访问系统剪贴板')
QApplication.clipboard = staticmethod(forbidden_clipboard)

# 原云发现可探测用户/挂载目录；限定为本门的空云目录集，不启动外部扫描。
import core.cloud_locations
core.cloud_locations.find_cloud_locations = lambda: []
from core.settings_manager import SettingsManager
from gui.browser.file_browser import FileBrowser
original_navigate = FileBrowser.navigate_to
def private_navigate(self, path):
    assert Path(path).resolve().is_relative_to(profile), path
    return original_navigate(self, path)
FileBrowser.navigate_to = private_navigate

app = QApplication([])
startup_phase('qapplication')
assert app.platformName() == 'cocoa', app.platformName()
app.setApplicationName('ExplorerPro')
app.setOrganizationName('ExplorerPro')
app.setQuitOnLastWindowClosed(False)
app.setStyle('Fusion')
settings = SettingsManager.instance()
assert settings._config_path.resolve().is_relative_to(profile)
assert Path(QSettings('ExplorerPro', 'ExplorerPro').fileName()).resolve().is_relative_to(profile)
fixtures = profile / '私有 Unicode 空格'
fixtures.mkdir()
text = fixtures / 'Überblick 中文.txt'
text.write_text('RyuPy真实业务链\nÄpfel und Grüße\n', encoding='utf-8')
settings.set('general', 'start_folder', str(fixtures))
settings.set('general', 'language', 'en')
settings.set('privacy', 'enable_clipboard_monitor', False)
settings.set('index', 'auto_index', False)
settings.set('index', 'index_on_startup', False)
settings.save()
startup_phase('application-import-start')
from main import (configure_application_language, install_qt_translations,
                  install_runtime_translation, load_app_icon, set_application_version)
from core.gui_gc import install_gui_gc
from core.ui_translator import uninstall_ui_translator
from gui.sidebar.drive_capacity import shutdown_capacity_executor
from app import ExplorerProApp
startup_phase('application-import-end')
translator = configure_application_language()
assert translator.get_language() == 'en' and translator.t('Abbrechen') == 'Cancel'
install_qt_translations(app, translator.get_language())
install_runtime_translation(app, translator)
set_application_version(app)
assert app.applicationVersion() == '1.0.9'
collector = install_gui_gc(app)
window = None
failure = []


def phase(name):
    """单独记录事件循环进度；超时时不依靠 stdout 或 Python traceback 找卡点。"""
    with (evidence / 'phases.log').open('a', encoding='utf-8') as stream:
        stream.write(name + '\n')


def run():
    """通过真正 app.exec 的定时回调推进事件循环，并检验原控件和数据库。"""
    global window
    try:
        phase('construct')
        startup_phase('construct')
        window = ExplorerProApp()
        icon = load_app_icon()
        assert not icon.isNull(), '随包应用图标未加载'
        window.setWindowIcon(icon)
        phase('show')
        startup_phase('show')
        window.show()
        phase('expose')
        startup_phase('show-returned')
        assert QTest.qWaitForWindowExposed(window, 10000)
        assert window.isVisible() and window.windowHandle().isExposed()
        startup_phase('visible')
        QTest.qWait(1500)  # 覆盖上游 800ms 根预加载定时器。
        phase('navigate')
        window.file_browser.navigate_to(str(fixtures))
        deadline = time.monotonic() + 10
        while window.file_browser.current_path != str(fixtures) and time.monotonic() < deadline:
            QTest.qWait(50)
        assert window.file_browser.current_path == str(fixtures)
        phase('preview')
        window.preview_panel.show_preview(str(text))
        QTest.qWait(300)
        assert 'Äpfel' in window.preview_panel.text_preview.toPlainText()
        assert Path(window.file_index.db_path).resolve().is_relative_to(profile)
        phase('index')
        assert window.file_index.index_file(str(text), calculate_hash=False)
        assert any(row['path'] == str(text) for row in window.file_index.search('Überblick'))
        with sqlite3.connect(window.file_index.db_path) as database:
            assert database.execute('select hash from files where path=?', (str(text),)).fetchone() == (None,)
        assert not window.privacy_monitor._connected and window.privacy_monitor.clipboard is None
        assert window.grab().save(str(evidence / 'cocoa-unicode-preview.png'))
        print(json.dumps({'platform': app.platformName(), 'visible': True, 'exposed': True,
                          'pyside': pyside_version, 'qt': qVersion(), 'navigation': True,
                          'unicode_preview': True, 'sqlite_index_search': True,
                          'version': app.applicationVersion(), 'translations': True, 'icon': True,
                          'clipboard_accessed': False, 'settings': str(settings._config_path)},
                         ensure_ascii=False), flush=True)
    except BaseException:
        failure.append(traceback.format_exc())
        # 窗口可能尚未赋值，后续清理也可能失败；先保留真正的业务异常。
        (evidence / 'business-error.log').write_text(failure[-1], encoding='utf-8')
    finally:
        phase('close')
        if window is not None:
            window.close()
        shutdown_capacity_executor()
        assert QThreadPool.globalInstance().waitForDone(5000)
        QTest.qWait(200)
        phase('thread-audit')
        import shiboken6
        # 检查所有可达 QThread 包装，包括没有 QApplication 父对象的业务 worker。
        # 类型筛选留在堆所属域，只把匹配的线程包装跨桥交给同一存活检查。
        threads = builtins.filter(QThread.__instancecheck__, gc.get_objects())
        alive = [obj.objectName() for obj in threads
                 if shiboken6.isValid(obj)
                 and obj != QThread.currentThread() and obj.isRunning()]
        python_threads = [t.name for t in threading.enumerate() if t is not threading.main_thread()]
        if alive or python_threads:
            failure.append(f'退出后线程残留: Qt={alive}, Python={python_threads}')
        if window is not None and window.isVisible():
            failure.append('原窗口关闭失败')
        uninstall_ui_translator(app)
        collector.close()
        print(json.dumps({'window_closed': window is not None and not window.isVisible(),
                          'qt_threads': alive, 'python_threads': python_threads}), flush=True)
        phase('quit')
        app.quit()


QTimer.singleShot(0, run)
QTimer.singleShot(45000, lambda: (failure.append('GUI deadline exceeded'), app.quit()))
app.exec()
if failure:
    raise RuntimeError('\n'.join(failure))
