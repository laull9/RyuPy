# str.translate：通用整数索引表、删除、Unicode与可观察查表副作用。
print("mapping", "abc😀é".translate({97: "XY", 98: None, 99: 0x1F642, 0x1F600: "", 233: "é"}))
print("string-table", "\x00\x01AZ".translate("xy"))
print("list-table", "\x00\x01\x02".translate(["long", None, 65]))
print("empty", "".translate(None))
print("bool", repr("a".translate({97: True})))
print("surrogate", ord("a".translate({97: 0xD800})))
print("maximum", ord("a".translate({97: 0x10FFFF})))
print("string-key", "a".translate({"a": "b"}))
saved = "aba".translate
print("saved", saved({97: "A"}))
print("root", str.translate("abc", {98: None}))


class Text(str):
    def __str__(self):
        raise ValueError("must not call str")


result = Text("abc").translate({97: Text("Z")})
print("subtype", result, type(result) is str)


class Table:
    def __init__(self, replacement):
        self.replacement = replacement
        self.calls = []

    def __getitem__(self, key):
        self.calls.append(key)
        if key == 97:
            return self.replacement
        raise KeyError(key)


for source in ["ababa", "aéa"]:
    for replacement in ["A", None, "", "XY", "é", 0x1F600]:
        table = Table(replacement)
        print("calls", repr(source.translate(table)), table.calls)


class Missing(LookupError):
    pass


class MissingTable:
    def __getitem__(self, key):
        raise Missing("leave unchanged")


print("lookup-subclass", "aé😀".translate(MissingTable()))


class BrokenTable:
    def __getitem__(self, key):
        raise ValueError("mapping failed")


try:
    "abc".translate(BrokenTable())
except ValueError as error:
    print("propagate", str(error))


class IndexOnly:
    def __index__(self):
        print("unexpected-index")
        return 65


for table in [{97: -1}, {97: 0x110000}, {97: 10**100}, {97: 1.5}, {97: []}, {97: IndexOnly()}, None, 1]:
    try:
        print("invalid", "a".translate(table))
    except (ValueError, TypeError) as error:
        print("error", type(error).__name__, str(error))

for invoke in [lambda: "a".translate(), lambda: "a".translate({}, {}),
               lambda: "a".translate(table={}), lambda: str.translate(),
               lambda: str.translate(1, {})]:
    try:
        invoke()
    except TypeError as error:
        print("binding", str(error))


class MissingDict(dict):
    def __missing__(self, key):
        return "?"


print("dict-missing", "ab".translate(MissingDict({97: "A"})))


class IndexedTable:
    def __getitem__(self, key):
        raise IndexError("absent")


print("index-miss", "aé".translate(IndexedTable()))


class ProbeTable:
    def __init__(self, value):
        self.value = value
        self.calls = []

    def __getitem__(self, key):
        self.calls.append(key)
        if key == 97:
            return None
        if len(self.calls) == 2:
            return self.value
        return "Z"


for value in ["XY", 0x110000, []]:
    probe = ProbeTable(value)
    try:
        print("delete-probe", "aé".translate(probe), probe.calls)
    except (TypeError, ValueError) as error:
        print("delete-error", type(error).__name__, str(error), probe.calls)


class RaiseSame:
    def __init__(self, error):
        self.error = error

    def __getitem__(self, key):
        raise self.error


failure = ValueError("same-error")
try:
    "a".translate(RaiseSame(failure))
except ValueError as error:
    print("same-error", error is failure, error.__context__ is None)


# 被吞的用户回调异常不能积累trace帧或导致伪RecursionError。
print("long-lookup", len(("é" * 1500).translate(MissingTable())))
try:
    "a".translate({97: -1})
except ValueError as error:
    print("after-misses", str(error), error.__context__ is None)


try:
    raise RuntimeError("outer")
except RuntimeError as outer:
    print("active-long-lookup", len(("é" * 1500).translate(MissingTable())))
    for table in [None, 1, {97: []}, {97: -1}, BrokenTable()]:
        try:
            "a".translate(table)
        except (TypeError, ValueError) as error:
            print("active-context", type(error).__name__, error.__context__ is outer)
