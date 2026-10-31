# ryupy: pending
# M2.3：数字键的相等与哈希一致，字典视图保持活性。
mapping = {True: "bool", 1: "int", 1.0: "float"}
print(len(mapping), mapping[True], hash(True) == hash(1) == hash(1.0))
keys = mapping.keys()
mapping["later"] = 2
print(list(keys))
try:
    mapping[[]] = 1
except TypeError:
    print("unhashable")
try:
    for key in mapping:
        mapping["new"] = 3
except RuntimeError:
    print("changed during iteration")
try:
    print(mapping["missing"])
except KeyError as error:
    print(error.args)
