# 通用库：six 的类型兼容助手。
import six

print(six.text_type("a"), six.integer_types)
print(six.u("text") == "text", isinstance(1, six.integer_types))
print(six.ensure_binary("abc"), six.ensure_str(b"abc"))
