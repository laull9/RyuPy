# ryupy: pending
from importlib import metadata
points = metadata.entry_points()
print(len(points), bool(points))
