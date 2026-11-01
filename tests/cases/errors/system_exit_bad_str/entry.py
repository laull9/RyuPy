import sys


class Code:
    def __str__(self):
        raise ValueError("bad code")


sys.exit(Code())
