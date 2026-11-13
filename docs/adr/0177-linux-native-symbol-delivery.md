# ADR-0177：Linux Native 调试符号与交付目录分离

状态：候选。纯身份测试已过；真实应用部署、调试器恢复及完整门待验。未合入 dev/main。

## 背景与边界

真实 binaryornot/pyfiglet Native 部署显示 SDK 默认生成的大型 `.dbg` 被完整复制入 `.dist`。pyfiglet 两次实测入口约6.76MB，符号约10.09MB，完整dist约23.19MB。这是ADR0052的保守设计，不把删除未知旁件当作优化。

本决策仅修订ADR0052的Linux交付符号部分：高层布局在复制完整publish后识别本次host关联的调试文件，将确认的符号从**新候选副本**中移除，原SDK publish及其完整manifest仍留在本轮owned `.build`。native字节不变，未知旁件、外置资源一律保留。Mac/Windows维持旧行为；不存在跨平台全部分离声明。

## 资格与保守回退

使用成熟 `object` ELF parser，不自写二进制格式解析。只接受Linux ELF64/x86-64小端、本次host的精确顶层 `<host>.dbg`，native的`.gnu_debuglink` basename完全相同；全文件CRC32相符，两侧非空GNU build-id相同，符号具有非空`.debug_info`。

不认识、缺字段、错误CRC/build-id、其它符号格式或超过每文件512MiB身份读取预算，均保留完整原布局并记录原因。真实IO错误、链接及身份变化仍失败，不把它们降级为一般未识别。CRC/build-id用于本次构建关联，不是抗恶意伪造的真实性证明。

读同一文件handle前后核dev/ino/mtime/ctime及长度，独立SHA再核内容；分离前后核完整原publish快照。仅处理原manifest标为未分类旁件的精确文件，不按全树扩展名过滤，也不删除用户资源。

## 记录、事务与可调试性

最终dist清单移除该符号项并重新严格核完整文件集合。attempt的`debug-symbols.json`保留相对源路径、native/symbol SHA256、debuglink CRC、build-id、分离/保留原因；CLI额外报告该清单路径。记录不进入可搬迁dist，不是部署运行依赖。低层SDK publish API不改变。

旧dist备份、owner账本、同名锁及失败保旧仍沿原流程；任何分离/记录失败发生在候选安装前。保留符号仍占.build磁盘，不能把部署目录缩小宣传为总磁盘节省。调试器需显式加载保留符号，不承诺搬迁后自动发现。

CPU采样器只向它自己的诊断副本加回已校验符号，产品dist保持无符号。真实CI必须用搬迁后的应用与保留符号，禁debuginfod/自动加载，在gdb命中已知runtime方法并恢复帧；仅nm列出名称不计调试证明。

## 验证计划

- synthetic ELF覆盖正确分离、错CRC/build-id/路径、缺调试信息、未知文件/资源保留、篡改/链接/归属/记录碰撞失败；原publish和native字节不变。
- binaryornot真实47源consumer宿主/搬迁/禁网无Python/.NET容器，追加符号身份与gdb恢复；报告部署字节与保留符号字节，不测执行性能。
- pyfiglet两入口与553字体完整资源部署；Mac/Windows保守回归，必要时检查受影响CPU profiler。
- 完整make check、CLI体积变化与源码同树证明。未验证的项目不因helper绿色晋升。
