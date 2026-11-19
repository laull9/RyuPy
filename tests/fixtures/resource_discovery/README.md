# 空资源子包候选回归

候选 pending，尚未新 Runtime/JIT/Native 验收。static_entry.py 与 pkg 两源码逐字保原冻结 Static TypedDict 反例；入口文件名不同不改变其内容。CLI验收按原 entry.py 文件名物化，不能改原 stdout oracle。JIT默认及显式include均应登记3模块，但pkg.data仍不在sys.modules；AOT仅显式include纳图。原反例不含资源读取，新资源读取/负例由driver集成测试创建独占输入。
