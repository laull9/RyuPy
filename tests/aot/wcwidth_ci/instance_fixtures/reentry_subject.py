# 同名普通 import 重入应命中当前正在初始化的对象，不再次执行正文。
import lifecycle_state
lifecycle_state.seen.append('enter')
import reentry_subject
same = reentry_subject
def read():
    return same
