# 单值参数解析、类型转换和受控帮助/错误流，不调用宿主进程退出。
import argparse
import io
import sys

parser = argparse.ArgumentParser(prog="fixture", description="Tiny command.")
parser.add_argument("name", help="name to print")
parser.add_argument("-n", "--count", type=int, default=1, help="repeat count")
parser.add_argument("--verbose", action="store_true")
for args in [["cat"], ["--count=3", "cat", "--verbose"], ["cat", "--cou", "-2"], ["--", "-cat"]]:
    result = parser.parse_args(args)
    print(result.name, result.count, result.verbose)
print(parser.format_help(), end="")
oldout = sys.stdout
olderr = sys.stderr
for args in [["--help"], [], ["cat", "--count", "bad"], ["cat", "--missing"], ["cat", "--count"]]:
    output = io.StringIO()
    errors = io.StringIO()
    sys.stdout = output
    sys.stderr = errors
    code = None
    try:
        parser.parse_args(args)
    except SystemExit as error:
        code = error.code
    finally:
        sys.stdout = oldout
        sys.stderr = olderr
    print(code, repr(output.getvalue()), repr(errors.getvalue()))
oldargs = sys.argv
sys.argv = ["fixture", "dog", "-n", "4"]
try:
    result = parser.parse_args()
    print(result.name, result.count)
finally:
    sys.argv = oldargs

# 原生属性必须返回真正 Python None，模块赋值后不能误当未绑定槽。
empty = argparse.ArgumentParser(prog="empty")
action = empty.add_argument("--optional")
default = action.default
print(default is None)
help_text = action.help
print(help_text is None)
description = empty.description
print(description is None)
options = empty.parse_args([])
optional = options.optional
print(optional is None)
