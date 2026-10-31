# M2.5：预热后替换方法、property 和实例属性不得命中旧缓存。
class Item:
    value = 1

    def read(self):
        return self.value


def replacement(self):
    return self.value + 10


item = Item()
for _ in range(5):
    print(item.read())
Item.read = replacement
print(item.read())
item.value = 4
print(item.read())
del item.value
Item.value = 9
print(item.read())
Item.read = property(replacement)
print(item.read)
