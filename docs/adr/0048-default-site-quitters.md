# ADR-0048：默认 site 启动的有限 Quitter 对象

- 日期：2026-11-03
- 状态：本地候选，专项及完整门禁通过；远端仍待验证，不是项目兼容门完成。

## 背景与边界

固定项目基线使用 CPython 3.12.13 的默认启动（不传 `-S`）。该模式由 `site` 注入
`exit` / `quit`；固定解释器的 `-S` 探针确实没有这两个名字。
RyuPy 当前没有 no-site 启动开关，本切只对齐默认模式的两个辅助对象，不实现 `site` 模块，
也不将交互辅助名称误称为裸 CPython 的固有内建。将来增加 no-site 模式必须独立控制注入。

## 决策

- 两名字映射到同一真实 `PyClass("Quitter")` 的两个不同、稳定 `PyInstance`。
  不用假 builtin_function，不直接别名 `sys.exit`；直接调用与一等值均经实例正常调用。
- `name` / `eof` 是普通可写槽，repr 先读取两个属性，再按 Python 字符串转换顺序处理。
  类型 `__call__` 重绑仍影响直接/别名调用；普通同名用户绑定优先。
- `code` 可位置或关键字，参数绑定错误在关闭 stdin 前抛出。绑定成功后获取当前 stdin.close
  并调用一次；吞 Python BaseException，恢复原活动异常及帧，再抛出新 SystemExit。
  保原 code 对象；零参数也是 `args == (None,)`，不能沿用 sys.exit 的 CLR null 简写。
- 类定义模块使用仅该原生类设置的内部元数据标记，保 `_sitebuiltins` 身份及动态
  `__module__` 覆盖/删除行为。既有 `PyClass(string)` CLR 构造 ABI、普通类默认路径不变。
- Windows EOF 提示取 `Ctrl-Z plus Return`，其它平台取 `Ctrl-D (i.e. EOF)`。
  这是固定 CP13 `site.py` 的平台规则；本机 Linux 结果不冒 Windows 实机验证。

## 成本与不承诺项

首次读取时冷构造一个类、两个实例和三个真实方法，后续复用身份；正常调用保既有 binder
及实例动态分派，不新增 MIR RuntimeFn、CIL ABI 或通用 FFI。本轮没有性能测量或提速承诺。
不包含 `_sitebuiltins` 公开导入、完整 site、pickle、类继承/反射全部长尾；普通类已有
`__bases__` 等限制不因 Quitter 新对象而宣称补齐。不能吞任意宿主致命异常或直接结束 .NET 宿主。

## 本轮验证与首失败

固定 CP13 已核默认/site-disabled区别、None/code原身份、参数先行、四类 close 异常和
原 context、有限重入、非字符串字段/转换错误、类调用槽重绑。真实专项已过：
10 C#、1 HIR 名称遮蔽结构、3 driver（核心差分、原 caller 两帧、实际退出码/stderr/finally）。

保留初次问题：一 C# 测试误用未支持的 CallBuiltin(type)，改正式 TypeOf 后保原身份断言；
首 driver 使用尚未刷新的 Release runtime，不作为新源码结果；刷新后缺 close 场景撞到
既有 object() 未实现，改用同样无 close 的自有空实例，原 code/args/context断言未删除。
执行器一次 transport 断连后先核活进程/日志并备份源码，再继续，不假定原命令成功。

正常完整 `make check` 已 exit0：556 C#、228 固定 CP 基线、202 e2e + 30 ignored；
未新跑任何固定项目、Quitter AOT产物或性能矩阵。
历史 pyfiglet 的 exit 首阻断只是排期线索，不能把两个名字实现计为 pyfiglet 原生通过。
