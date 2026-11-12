# 从真实分发 console_scripts 查找、加载并执行上游同一个 main；不替换 stdout。
import sys
from importlib.metadata import entry_points

entries = entry_points(group='console_scripts', name='pyfiglet')
assert len(entries) == 1
entry = list(entries)[0]
assert entry.value == 'pyfiglet:main'
main = entry.load()
sys.exit(main())
