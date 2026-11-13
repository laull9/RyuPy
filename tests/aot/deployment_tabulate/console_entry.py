import sys
from importlib.metadata import entry_points
point = entry_points().select(group='console_scripts', name='tabulate')['tabulate']
sys.exit(point.load()())
