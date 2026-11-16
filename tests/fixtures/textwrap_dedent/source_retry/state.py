# 失败模块以外的独立已完成依赖，保原异常和循环逃逸owner。
marker = RuntimeError("source import failed")
attempts = 0
escaped = []
user_value = []
