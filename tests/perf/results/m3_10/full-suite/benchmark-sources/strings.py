# bench: category=strings
# bench: desc=字符串方法 split / join / replace / find / upper / strip、f-string 与 str()
line = "the quick brown fox jumps over the lazy dog"
total = 0
for i in range(50000):
    parts = line.split(" ")
    total += len(parts)
    joined = "-".join(parts)
    total += len(joined)
for i in range(50000):
    s = line.replace("o", "0")
    total += s.find("0") + len(s.upper())
for i in range(50000):
    t = "  value " + str(i) + "  "
    total += len(t.strip())
out = []
for i in range(100000):
    out.append(f"{i}:{i * 2:>8}|{i / 7:.3f}")
total += len("".join(out))
count = 0
for ch in line * 2000:
    if ch == "o":
        count += 1
print(total, count)
