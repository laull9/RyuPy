# ryupy: pending
from importlib import metadata
try:
    metadata.entry_points()
except NotImplementedError:
    print('unconfigured NotImplementedError')
