import sys
print('ARM empty')
try:
    from empty_pkg import child
except Exception as error:
    print('ERROR', type(error).__name__, str(error))
    print('DETAIL', getattr(error, 'name', None), getattr(error, 'path', None), error is None, type(error.__context__).__name__, error.__cause__ is None, error.__suppress_context__)
else:
    print('VALUE unexpected')
parent = sys.modules.get('empty_pkg')
print('CACHE', 'empty_pkg.child' in sys.modules, parent is not None, hasattr(parent, 'child'))
