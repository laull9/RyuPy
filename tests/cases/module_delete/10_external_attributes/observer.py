# 属性读取跟随原模块当前绑定，from import保留先前对象。
import subject
from subject import payload as saved

def read():
    return subject.payload
