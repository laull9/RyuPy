# ryupy: pending
# 默认模块__name__现有绑定可变且不要求str；不把真实来源身份误当公开元数据。
def make():
    class C:
        def read(self):
            return __class__
    return C

original = make()
__name__ = 'changed module'
changed = make()
print(changed.__module__, original.__module__ != changed.__module__)
__name__ = 17
nonstring = make()
print(nonstring.__module__, nonstring().read() is nonstring)
