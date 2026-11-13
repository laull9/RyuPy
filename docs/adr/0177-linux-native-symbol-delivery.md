# ADR-0177：Linux Native 调试符号与交付目录分离

状态：有限验收通过（2026-11-13）。Linux x64交付分离、真实搬迁调试恢复与三平台应用回归已核；仅独立验证分支，未合入 dev/main。

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


## 2026-11-13 实际验收

产品提交本地 `f8696099` / 远端 `8095410c`，树 `4775ee88`。随后仅修审计脚本与workflow，产品源码不变；解析修复本地 `e5d65d31` / 远端 `932f85a0`，树 `faf2e98f`。完整 `make check` 退出0：C# 3053通过/5跳过，e2e 454通过/283 ignored，不把跳过项算兼容。生成产物与ZIP不进入Git。

- [binaryornot CI 43339677617](https://github.com/laull9/RyuPy/actions/runs/43339677617)：47源consumer，宿主原位/搬迁8次及禁网无Python/.NET容器8次通过。独立readelf/CRC/build-id/清单审计通过；搬迁ELF显式加载保留符号，GDB实际命中`PyBinaryDecode__Invoke`并恢复正确顶帧、输出False、正常退出，原件前后未变。
- 此应用原publish为 **21,424,519字节**，交付dist为 **11,101,529字节**，少 **10,322,990字节（约48.18%）**；其中保留符号为10,322,728字节，余差来自清单/入口名。native SHA一致。不是执行性能提升，也不是总磁盘节省。
- [Linux项目 CI 43335125471](https://github.com/laull9/RyuPy/actions/runs/43335125471)：pyfiglet原CLI/metadata入口，原项目/项目搬迁后共4次公开AOT、各553资源、12次无Python/.NET禁网容器运行通过。原CLI交付为13,095,235字节，metadata入口为13,099,406字节；保留约10.09MB符号。
- [平台 CI 43335125423](https://github.com/laull9/RyuPy/actions/runs/43335125423)：macOS arm64、Windows x64各两入口、原位/搬迁12次语义运行通过，原有资源/输出边界和Windows路径/metadata检查保留。两者仍为带SDK宿主的精简环境，不宣称干净机；本决策不分离它们的符号。
- [热点 CI 43335125457](https://github.com/laull9/RyuPy/actions/runs/43335125457)：诊断副本加回匹配符号后获得496样本/482 native样本；产品交付目录未加回符号。不把采样wall或热点占比当作性能A/B数据。

失败历史保留：初次binaryornot workflow YAML条件写法未进入产品执行，修正后`43335274625`真实构建和部署通过，但GDB已实际命中时输出为`Thread 1 "scanner" hit Breakpoint ...`，旧解析只接受行首`Breakpoint`而误判。修复后新增实际命中/仅创建断点/错误函数/错误顶帧/异常退出负例；`43339677617`整轮成功。最终又收紧顶帧函数位置，拒绝前缀函数或参数字符串内伪匹配，并用两轮原始GDB输出和7项合同复核；这项测试-only收紧未再重建产品，不伪称在新文档提交上重跑所有CI。
