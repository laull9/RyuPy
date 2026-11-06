# 在删除前导入的消费者，属性读取是实时的，from import 则保存旧值。
import subject
from subject import payload as saved

def read():
    return subject.payload
