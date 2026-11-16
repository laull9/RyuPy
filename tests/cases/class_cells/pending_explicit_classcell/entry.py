import subject
assert subject.Explicit().read() is subject.Explicit
assert '__classcell__' not in subject.Explicit.__dict__
print('pending explicit classcell namespace protocol')
