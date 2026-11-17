# oct名字可保存、用户遮蔽并在删除模块绑定后恢复默认内建。
saved = oct
print('original', saved(0o755))
oct = lambda value: 'user binding'
print('shadow', oct(8))
del oct
print('fallback', oct(8), saved(8))
def local():
    oct = lambda value: 'local binding'
    return oct(8)
print(local(), saved is oct)
