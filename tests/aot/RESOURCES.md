# 显式资源部署竖切

状态：本地语义/结构门禁通过；真实三平台资源发布尚待 CI，不以此前无资源清单验收替代。
合同见 [ADR0047](../../docs/adr/0047-explicit-aot-resources.md)。

## 使用与有限范围

```sh
ryupy build app/main.py --aot --resources app/resources.json --out-dir out
```

```json
{"schema_version":1,"modules":[{"module":"greetings.english","files":["data.txt"]},{"module":"greetings.french","files":["data.txt"]}]}
```

每个模块必须已在本次编译图中；files 是实际源码文件父目录下的精确相对文件，不
扫描/glob，不自动选择父包。仅部署相对名字/选中锚点限 ASCII，非法名字明确拒绝，
不会改名；宿主绝对源目录/输出目录可以含中文、空格与 &。不部署 Python 源码。
Windows 不合并不同来源路径/拼写的同目标资源，即使 hardlink 等别名合法；同一已
验证来源记录/快照可供多个模块共享。不是对所有文件系统身份或恶意竞争的保证。

JIT 实际 build 的 `__file__` 使用真实源码位置；数据之后修改可见。选择资源的 AOT
模块位置为可执行文件旁 `_ryupy_resources/v1/...` 虚拟 .py 锚点，源码本身不存在。
ILC 前冻结数据到 owned staging，发布后不回原源目录读取；未选择的 AOT 模块仍保
旧 basename 位置。外部 __file__ 写入与内部全局同槽，可保存 None/非字符串对象。
诊断路径与普通 dump 不变；实际构建此全局槽为 Dynamic，成本不称零。

无资源请求保 schema1；请求成功输出 schema2，资源与 artifacts 一一对应，并记录
真实 consumer module_id。`declared_resources_complete` 只表示显式声明处理完毕，
不是全动态依赖、ILC 保根或原生库闭包。请搬迁完整 publish 树而非只复制 executable。

## 固定验证

- `resource_fixtures`：一个两模块程序，输出与 CPython3.12.13 对照；模块本身没有
  __file__ 写入，主程序另断言外部 None/list 重绑的内外身份。
- `resource_smoke.py`：每 RID 仅一次真实资源 publish；JIT 在不同 cwd 读取原/改
  数据，AOT 整树搬迁后读取冻结数据；源和 IL 不可用，缺数据分别验证清单拒绝与
  真实 FileNotFoundError。成功模式的实际 CP/JIT/native 双流逐字比较，不归一化实际
  换行；缺数据负例比较完整帧/行号/函数与 FileNotFoundError 类型，不比较完整异常文案。
- Linux 另在只读、无网络且检查无 dotnet/Python 的容器执行。Mac/Windows hosted
  runner 仍安装 SDK，不能称为完全干净目标机。
- 总预算 600s，publish 单次上限300s、应用30s，均受剩余预算截断；workflow step
  12min 留清理/归档余量，不重试至有利。原七配置/四模式验证不删，不采性能样本。

本地首次验证：runtime位置17项、driver资源23项、HIR/ty/MIR/PE结构7项、实际多模块
JIT差分1项通过；Python20项中19通过、1项Windows实机测试明确跳过。完整make check
为471项C#、228项固定CP基线、202项e2e加30ignored，fmt/clippy/Release通过。
原始日志保本轮临时证据/CI artifact，不提交Git。三平台实际资源结果须另外登记。
