# 已知primitive接收者的属性只在实际运行时读取，不把现有内建方法误报为静态错误。
version = '5.2.0'
print(version.split('.'))

def first_component(value: str) -> str:
    return value.split('.')[0]

def uppercase(value: str) -> str:
    return value.upper()

def bound_upper(value: str) -> str:
    action = value.upper
    return action()

def missing_attribute(value: str) -> str:
    return value.no_such_attribute

print(first_component('1.2.3'), uppercase('mixed'), bound_upper('bound'))
try:
    missing_attribute('text')
except AttributeError:
    print('缺属性按运行时协议失败')

# 标注不是强制转换：不同实际接收者的用户方法照常调用/传播异常。
class Other:
    def upper(self):
        print('用户upper')
        return 'other'
class Failure:
    def upper(self):
        raise ValueError('用户upper错误')
def user_upper(value: str) -> str:
    return value.upper()
print(user_upper(Other()))
try:
    user_upper(Failure())
except ValueError as error:
    print(str(error))

# 旁边无属性访问的数值函数仍应可走Static（另有结构单测）。
def numeric_add(left: int, right: int) -> int:
    return left + right
print(numeric_add(20, 22))
