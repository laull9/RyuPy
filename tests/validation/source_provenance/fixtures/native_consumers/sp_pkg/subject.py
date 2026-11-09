# 显式Instance普通模块：ModuleMain和真实Dynamic同时实际消费来源。
import warnings
from typing import TypedDict

__name__ = '__main__'
Top = TypedDict('SubjectTop', {'value': int})
with warnings.catch_warnings(record=True) as records:
    warnings.warn('subject-main', DeprecationWarning)
    print('subject-main', Top.__module__, len(records), str(records[0].message))

def emit(message):
    with warnings.catch_warnings(record=True) as records:
        warnings.warn(message, DeprecationWarning)
        record = TypedDict('DynamicRecord', {'value': int})
        print('dynamic-warning', len(records), str(records[0].message))
        return record.__module__
