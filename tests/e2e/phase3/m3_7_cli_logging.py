# M3.7：参数解析与日志流，避免时间和默认 stderr 格式影响差分。
import argparse
import io
import logging

parser = argparse.ArgumentParser(prog="fixture")
parser.add_argument("name")
parser.add_argument("--count", type=int, default=1)
options = parser.parse_args(["cat", "--count", "3"])
print(options.name, options.count)
buffer = io.StringIO()
handler = logging.StreamHandler(buffer)
handler.setFormatter(logging.Formatter("%(levelname)s:%(message)s"))
logger = logging.getLogger("ryupy.fixture")
logger.handlers.clear()
logger.propagate = False
logger.setLevel(logging.INFO)
logger.addHandler(handler)
try:
    logger.debug("hidden")
    logger.info("count=%s", options.count)
    handler.flush()
    print(buffer.getvalue(), end="")
finally:
    logger.removeHandler(handler)
    handler.close()
