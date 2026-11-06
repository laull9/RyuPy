# ryupy: pending
# 验证合法未绑定代理的真实类型和字段，不将 None 误作缺失首参。
import subject
proxy = subject.C.f(None)
assert type(proxy) is super
assert proxy.__thisclass__ is subject.C
assert proxy.__self__ is None
assert proxy.__self_class__ is None
print('pending None receiver creates an unbound super proxy')
