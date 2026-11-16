from importlib.resources import files
import cycle_events
import cyclepkg
cycle_events.events.append(files('cyclepkg').joinpath('data.txt').read_text(encoding='utf-8').strip())
