# ryupy: pending
# 已登记未支持：大字典用户键仍需CPython分块缓存hash合并；不可按标量路径转正。
log = []
class Key:
    def __init__(self):
        log.append('key')
    def __repr__(self):
        log.append('repr')
        return 'key'
    def __hash__(self):
        log.append('hash')
        return 0
    def __eq__(self, other):
        log.append('eq')
        return False
def value():
    log.append('value')
    return 0
try:
    result = {Key(): value(),
              1: 1,
              2: 2,
              3: 3,
              4: 4,
              5: 5,
              6: 6,
              7: 7,
              8: 8,
              9: 9,
              10: 10,
              11: 11,
              12: 12,
              13: 13,
              14: 14,
              15: 15,
              16: 16,
              17: 17,
              18: 18,
              19: value()}
except NotImplementedError:
    print('unsupported', log)
else:
    print('accepted', log)
