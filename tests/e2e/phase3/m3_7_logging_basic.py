# 基础配置只在根没有处理器时生效；force 不接管用户流所有权。
import io
import logging
import sys

print(type(logging.root).__name__, isinstance(logging.root, logging.Logger))
print(type(logging.lastResort).__name__, isinstance(logging.lastResort, logging.StreamHandler))
first = io.StringIO()
second = io.StringIO()
logging.basicConfig(level="INFO", format="%(levelname)s:%(message)s", stream=first)
logging.debug("hidden")
logging.info("value=%d", 3)
logging.basicConfig(level="DEBUG", stream=second, unknown=True)
logging.debug("still hidden")
logging.warning(msg="warning")
logging.disable(logging.ERROR)
logging.error("disabled")
logging.critical("critical")
logging.disable(logging.NOTSET)
logging.basicConfig(force=True, level="DEBUG", stream=second)
logging.log(logging.DEBUG, "fresh")
print(repr(first.getvalue()), first.closed)
print(repr(second.getvalue()))
for value in [10, 30, 51, "WARN", "FATAL", "missing", None]:
    print(logging.getLevelName(value))
try:
    logging.basicConfig(force=True, stream=second, unknown=True)
except ValueError:
    print("unknown", len(logging.root.handlers))
old = sys.stderr
sys.stderr = io.StringIO()
logging.root.handlers.clear()
logging.warning("automatic")
print(repr(sys.stderr.getvalue()))
sys.stderr = old
logging.root.handlers.clear()
# 默认处理器捕获当时的 stderr；更换 sys.stderr 不重定向旧处理器。
captured = io.StringIO()
replacement = io.StringIO()
sys.stderr = captured
logging.info("capture")
sys.stderr = replacement
logging.error("same stream")
sys.stderr = old
print(repr(captured.getvalue()), repr(replacement.getvalue()))
logging.root.handlers.clear()
