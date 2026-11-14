# 真实完整包图；不得以成功导入或旧Native绿色替代四个消费者实际输出。
import sp_pkg
import sp_pkg.subject

subject = sp_pkg.subject
subject.emit.__module__ = 'reflection-native'
subject.__name__ = '__main__'
print('dynamic-result', subject.emit('native-message'))
print('NATIVE done')
