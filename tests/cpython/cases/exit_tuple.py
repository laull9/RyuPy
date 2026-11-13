"""CP1b：SystemExit元组是单个code对象，保持repr和退出1。"""
import sys
sys.exit((2, '退出 元组'))
