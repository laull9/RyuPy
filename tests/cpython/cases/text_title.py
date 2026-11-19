"""逐 API 文本桥合同：Unicode title、保存绑定方法、根描述符及错误参数。"""
for text in ['', 'date of birth', "they're ready", 'ǳ ß İ', 'a1b_hello', '中文 hello']:
    saved = text.title
    print(saved(), str.title(text), text.title())
for invalid in [1, None]:
    try:
        'title'.title(invalid)
    except TypeError as error:
        print(type(error).__name__, str(error))
try:
    'title'.title(value=1)
except TypeError as error:
    print(type(error).__name__, str(error))
