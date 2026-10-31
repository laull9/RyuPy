# bench: category=project
# bench: desc=项目式切片：生成文本 → 解析记录对象 → 分组聚合 → 排序 → 格式化报告
class Record:
    def __init__(self, user, action, amount):
        self.user = user
        self.action = action
        self.amount = amount

    def weight(self):
        if self.action == "buy":
            return self.amount * 2
        return self.amount


def generate(n):
    users = ["ann", "bob", "cyd", "dee", "eve", "fay", "gus", "hal"]
    actions = ["buy", "view", "cart", "refund"]
    lines = []
    x = 7
    for i in range(n):
        x = (x * 1103515245 + 12345) % 2147483648
        lines.append(f"{users[x % 8]},{actions[(x // 8) % 4]},{x % 997}")
    return "\n".join(lines)


def parse(text):
    records = []
    for line in text.split("\n"):
        fields = line.split(",")
        records.append(Record(fields[0], fields[1], int(fields[2])))
    return records


def by_total(item):
    return item[1]


def summarize(records):
    totals = {}
    counts = {}
    for r in records:
        totals[r.user] = totals.get(r.user, 0) + r.weight()
        key = (r.user, r.action)
        counts[key] = counts.get(key, 0) + 1
    ranking = sorted(totals.items(), key=by_total, reverse=True)
    lines = []
    for user, total in ranking:
        lines.append(f"{user:<5}{total:>10}{counts.get((user, 'buy'), 0):>6}")
    return lines


text = generate(60000)
records = parse(text)
for _ in range(4):
    report = summarize(records)
for line in report:
    print(line)
