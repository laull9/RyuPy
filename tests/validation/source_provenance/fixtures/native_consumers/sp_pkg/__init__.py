# Legacy ModuleMain：TD取入口快照，warning取已改活globals。
import warnings
from typing import TypedDict

__name__ = '__main__'
Top = TypedDict('PackageTop', {'value': int})
with warnings.catch_warnings(record=True) as records:
    warnings.warn('package-main', DeprecationWarning)
    print('package-main', Top.__module__, len(records), str(records[0].message))
