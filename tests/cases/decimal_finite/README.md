# 有限 Decimal 正合同（待验）

全部清单先为 pending；CPython 成功不等于产品支持，不调整项目通过统计。

- finite_values：有限字符串/int/bool 构造、符号零、保留尾零、极大指数、空格式、类型身份、精确比较与哈希。
- mixed_float_hash：有限 float 与 Decimal 的精确有理数比较、相等哈希和 dict/set 去重；
  仅保护既有 float inf/nan 的比较与键身份，不构造 Decimal 特殊值，也不构造 Decimal(float)。
- extreme_finite：正负最小次正规数、最小正常数、最大有限 float 的精确十进制等值，以及整数哈希模数边界。
- unicode_construction：合法 Unicode 十进制数字（含补充平面）、混合数字脚本、宽松下划线、外围 Unicode 空白。

不把 boundary_observations 中 CP 支持、但首切明确不支持的 Decimal(float)、
Decimal NaN/Infinity、Context 算术、非空格式规格当作本组正合同。
有限首切并非完整 decimal 模块或 Context 语义。真实 toml 测试由项目独立入口验收，
不得以本组通过代替原包、原入口与原黄金输出。
