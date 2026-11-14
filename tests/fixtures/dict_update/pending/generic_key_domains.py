# ryupy: pending
# CP合法的扩展键域；当前generic更新只支持精确scalar，不能把NI记作CP成功。
# 旧exact/raw custom-key路径仍应保留，下面的CP原流不宣称已修复hash缓存/重入。
log = []
class Key:
    def __hash__(self):
        log.append('key.hash')
        return 17
    def __eq__(self, other):
        log.append('key.eq')
        return self is other
    def __repr__(self):
        return 'Key()'
key = Key()
value = {}
value.update([(key, 1)])
print('generic-user-key', value, log)
log.clear()
class Mapping:
    def keys(self):
        log.append('mapping.keys')
        return [key]
    def __getitem__(self, item):
        log.append('mapping.getitem')
        return 2
value = dict(Mapping())
print('generic-mapping-user-key', value, log)
value = {}
value.update([(('tuple', 1), 3), (b'bytes', 4)])
print('generic-tuple-bytes', value)
source = {key: 5}
log.clear()
value = {}
value.update(source)
print('existing-raw-user-key', value, log)
log.clear()
value.update([('scalar', 6)])
print('generic-custom-target', value, log)
value = {}
def pairs():
    value[key] = 7
    yield ('scalar', 8)
log.clear()
value.update(pairs())
print('generic-reentrant-key-insert', value, log)
