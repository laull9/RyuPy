# datetime：公历与固定时区

本实现独立使用公历和整数微秒，不从宿主区域设置推断 Python 时间语义。日期范围为公历 1..9999；不借用 DateTimeOffset 的 ±14 小时上限。

## 类型与构造

- date(year, month, day)：必需整数或合法 `__index__` 返回值；日期不合法 ValueError。
- datetime(year, month, day, hour=0, minute=0, second=0, microsecond=0, tzinfo=None, *, fold=0)：datetime 是 date 的原生子类型；fold 必须 0/1。
- time(hour=0, minute=0, second=0, microsecond=0, tzinfo=None, *, fold=0)。日期/时间属性只读。
- timedelta(days=0, seconds=0, microseconds=0, milliseconds=0, minutes=0, hours=0, weeks=0)：整数和浮点数；大整数总微秒。浮点分量先拆整数和小数，缩放小数保留 CPython 的双精度舍入，累计不足微秒部分后一次 ties-to-even 舍入；不能擅自改成全程有理数，否则 0.0005 毫秒等边界会偏离 CPython。days 归一化后限 ±999999999。
- timezone(offset, name=省略)：offset 必须 timedelta，严格 `-24h < offset < 24h`，支持秒/微秒偏移；显式 name 必须字符串，显式 None 不视为省略。无名零偏移返回 timezone.utc。
- tzinfo 仅登记抽象祖先关系，不开放自定义子类构造/DST/平台时区。传入非 timezone 的 tzinfo 明确 NotImplementedError。

提供 MINYEAR/MAXYEAR；date/datetime/time/timedelta 的 min/max/resolution；timezone.utc/min/max。timezone 是 tzinfo 子类型。未知 Python 类型/类属性仍明确缺失，不伪造平台时区。

## 公共方法

- date：isoformat、weekday、isoweekday、toordinal、fromordinal、replace、str/repr。
- datetime：上述日期方法，以及 date、time（移除时区）、timetz（保留时区）、isoformat(sep='T', timespec='auto')、utcoffset/dst/tzname、replace、astimezone、timestamp。
- time：isoformat(timespec='auto')、utcoffset/dst/tzname、replace、str/repr。
- isoformat 支持 auto/hours/minutes/seconds/milliseconds/microseconds；截断显示而不是四舍五入。datetime 分隔符须单码点。
- astimezone 仅允许源和目标都明确为固定时区；省略目标或 naive 源拒绝，不隐式套用机器本地时区。timestamp 同样仅支持 aware。
- timezone：utcoffset/dst/tzname 接受 datetime 或 None；fromutc 要求 datetime.tzinfo 就是该对象。dst 总为 None。
- timedelta：days/seconds/microseconds 归一化属性、total_seconds、str/repr、真值。

## 运算、比较和哈希

日期加减 timedelta 使用其归一化 days，忽略 seconds/microseconds；datetime 使用全部微秒。相同日期类别的相减产生 timedelta。时间差支持加减、单目正负/abs、乘整数或浮点、除整数/浮点或另一时间差；除时间差返回浮点比值。尚无 timedelta 整除、取模、divmod。

date 与 datetime 不直接相等/排序；naive 与 aware datetime/time 不相等，排序或 datetime 相减报 TypeError。aware 对象用固定 offset 校正后的整数键比较/哈希，不对 time 的校正结果取模。fold 不影响固定时区下的相等性；算术结果 fold 复位为 0。timezone 比较只看偏移，不看显示名称。

相等对象哈希一致；哈希具体数值不承诺与另一进程/CPython 一致。日期越界算术报 OverflowError，不泄漏 CLR 范围异常。对象不可变，但当前原生类型不支持 Python 用户继承。

## 范围与成本

尚无 today/now/utcnow、fromtimestamp/utcfromtimestamp、fromisoformat、strptime/strftime、timetuple、isocalendar、combine、zoneinfo 或自定义 tzinfo 协议。特别不使用宿主 DST 近似未支持语义。未列出的接口通过 AttributeError/NotImplementedError 拒绝。

DateTime 仅复用公历字段/ordinal 校验；date/datetime 用 long 微秒，timedelta 用 BigInteger 避免 CLR TimeSpan 范围缩窄。total_seconds 和时间差比值先精确舍入二进制有效位再缩放，避免大分子先转 double 的双舍入；浮点乘除使用二进制有理数和整数 ties-to-even，构造使用上述分量缩放规则；通常整数路径仍有 BigInteger 成本，暂无未经测量的加速主张。

验收：`m3_7_datetime.py`、`m3_7_datetime_edges.py`、原跨模块 `m3_7_helpers.py`；专测 PyDateTimeTests 覆盖负微秒归一化、浮点 ties-to-even、闰日、范围、UTC 等价哈希、naive/aware 拒绝、20 小时固定偏移。
