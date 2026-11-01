# 警告类别仍是真实Python异常：统一构造、typedcatch、继承与一等类型值。
for category in [Warning, UserWarning, DeprecationWarning, PendingDeprecationWarning,
                 RuntimeWarning, SyntaxWarning, FutureWarning, ImportWarning,
                 UnicodeWarning, BytesWarning, ResourceWarning]:
    value = category('message')
    print(type(value).__name__, str(value), value.args, isinstance(value, Warning), isinstance(value, Exception))

try:
    raise UserWarning('typed')
except UserWarning as value:
    print(type(value).__name__, str(value))

try:
    raise ResourceWarning('resource')
except Warning as value:
    print(type(value).__name__, str(value))

class ProjectWarning(UserWarning):
    pass

try:
    raise ProjectWarning('project')
except UserWarning as value:
    print(type(value).__name__, str(value), isinstance(value, ProjectWarning))

print(issubclass(ProjectWarning, UserWarning), issubclass(UserWarning, Warning))
print(issubclass(Warning, Exception), issubclass(UnicodeWarning, UnicodeError))

# 构造不提前调用用户str；生成的用户警告CLR子类也保留延迟消息。
log = []
class Message:
    def __str__(self):
        log.append('str')
        return 'message-value'
class DerivedWarning(ProjectWarning):
    pass
message = Message()
warning = DerivedWarning(message)
print(log, warning.args[0] is message)
print(str(warning), log)

class BadMessage:
    def __str__(self):
        raise ValueError('format')
warning = ProjectWarning(BadMessage())
print(type(warning).__name__)
try:
    str(warning)
except ValueError as error:
    print(str(error))
