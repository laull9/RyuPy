# ryupy: pending
# target存在于源码目录但不include，快照仍完整发现，直到load才失败。
from importlib import metadata
points = metadata.entry_points()
print('discovered', len(points))
print(points['same'].name)
try:
    points['same'].load()
except ModuleNotFoundError:
    print('missing include ModuleNotFoundError')
