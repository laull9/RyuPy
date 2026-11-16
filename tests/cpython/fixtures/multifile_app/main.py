"""普通多文件业务入口：本地包负责业务，第三方包负责配置与资源。"""
from pathlib import Path
import sys
from invoice import render

receipt = render(sys.argv[1], sys.stdin.read())
Path("收据.txt").write_text(receipt)
print(receipt)
print("多文件业务完成", file=sys.stderr)
