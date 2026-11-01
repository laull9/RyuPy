# handler吞普通emit错误但不吞递归错误；诊断与lastResort通过模块属性真实配置。
import io
import logging
import sys

class Broken:
    def write(self, text):
        raise ValueError("sink failed")
    def flush(self):
        pass

logger = logging.getLogger("fixture.errors")
logger.setLevel(logging.DEBUG)
logger.propagate = False
handler = logging.StreamHandler(Broken())
logger.addHandler(handler)
old_errors = logging.raiseExceptions
logging.raiseExceptions = False
outer = KeyError("outer")
try:
    raise outer
except KeyError:
    logger.info("discarded")
    try:
        raise
    except KeyError as restored:
        print("restored", restored is outer)

class Recursive:
    def write(self, text):
        raise RecursionError("recursive")
    def flush(self):
        pass
handler.setStream(Recursive())
try:
    logger.info("recursive")
except RecursionError:
    print("recursion propagated")
logger.removeHandler(handler)
old_fallback = logging.lastResort
old_stderr = sys.stderr
try:
    capture = io.StringIO()
    sys.stderr = capture
    logger.warning("fallback")
    print(capture.getvalue(), end="")
    logging.lastResort = None
    logger.error("silent")
    print(capture.getvalue() == "fallback\n")
finally:
    sys.stderr = old_stderr
    logging.lastResort = old_fallback
    logging.raiseExceptions = old_errors
