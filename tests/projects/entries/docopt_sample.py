# 命令行工具：docopt 的 usage 解析与默认值。
from docopt import docopt

usage = """Usage: demo.py [--verbose] <name>

Options:
  --verbose  more output
"""

arguments = docopt(usage, argv=["--verbose", "alice"])
print(arguments["<name>"], arguments["--verbose"])
print(arguments["--verbose"] is True)
