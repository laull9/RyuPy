import warnings
import once_first
import once_second
with warnings.catch_warnings(record=True) as captured:
    warnings.simplefilter('once')
    once_first.issue()
    once_first.issue()
    once_second.issue()
    once_second.issue()
    print(len(captured))
    print([str(item.message) for item in captured])
