import sys
from binaryornot.check import is_binary
for path in sys.argv[1:]:
    print(is_binary(path))
