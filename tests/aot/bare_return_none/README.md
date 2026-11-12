# 裸 return 的真实 Native 兼容验收

固定 Linux x64、CPython 3.12.13、SDK 10.0.403、锁定 pyfiglet 1.0.2。通过统一项目公开构建原 CLI，复用独立源码/PE/metadata/完整 553 资源 receipt，然后隔离源码，在原产物位置和复制搬迁位置分别运行：

- `-f standard 'hi hi'`
- `-f standard 'hello world'`
- `-f standard` 加单参数 `The quick brown fox jumps over the lazy dog. ` 重复 20 次，保留尾空格，不改变列宽。

每次 exit/stdout/stderr 必须与固定 CPython 原入口完整输出一致。另以 `generic.py` 验证裸 return 经分支及转发后仍是 Python None，独立公开 AOT 构建、ELF/清单检查和原位/搬迁运行。后者不是 pyfiglet receipt，也不将包特判视为通用语义修复。

只证明上述输入和平台；不运行 Nuitka，不报告性能提升，也不是无 Python/.NET 干净机证明。失败原件完整保留。
