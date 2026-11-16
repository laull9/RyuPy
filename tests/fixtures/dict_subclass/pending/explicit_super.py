# ryupy: pending
# 模块级显式两参super是既有独立缺口，不能以默认布局成功掩盖。
class Policy(dict):
    def get(self, key, default=None):
        return 'business-get'
p = Policy(sku=3)
saved = super(Policy, p).get
assert saved.__self__ is p
p['sku'] = 6
assert saved('sku') == 6 and p.get('sku') == 'business-get'
print('pending.explicit-super', saved('sku'))
