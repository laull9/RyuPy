# 默认模块存储删除后必须对同一模块对象暴露为缺失。
payload = ['old']

def remove():
    global payload
    del payload
