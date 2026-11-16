"""真实 SanBudget 原生窗口合同：窗口、表单、Treeview、SQLite 与回调。

驱动复制原应用到私有目录，保留 class ExpenseTracker(tk.Tk) 和原方法。
创建真实窗口并处理平台事件，检查表单保存与编辑后关闭窗口。
"""
from app import ExpenseTracker

window = ExpenseTracker()
try:
    window.update()
    assert window.winfo_exists() == 1
    assert 'SanBudget' in window.title()
    window.date_var.set('2026-11-16')
    window.description_var.set('兼容性测试')
    window.amount_var.set('12.50')
    window.save_button.invoke()
    window.update()
    rows = window.db.list()
    assert len(rows) == 1 and rows[0]['amount'] == 12.5
    item = str(rows[0]['id'])
    assert len(window.tree.get_children()) == 1
    window.tree.selection_set(item)
    window.load_selected()
    window.amount_var.set('25.75')
    window.save_button.invoke()
    window.update()
    assert window.db.list()[0]['amount'] == 25.75
    window.db.delete(int(item))
    window.refresh()
    assert len(window.tree.get_children()) == 0
    print('sanbudget: native window callback form SQLite tree edit passed')
finally:
    window._on_close()
