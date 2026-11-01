# logging：首批同步流日志

本模块为独立编写的原生适配，不复制 CPython 实现；目标是项目常见日志路径，不是完整 logging 框架。

## API 与身份

- 常量：NOTSET、DEBUG、INFO、WARNING/WARN、ERROR、CRITICAL/FATAL。
- `getLogger(name=None)`：空名和 `root` 返回根记录器；同名驻留。创建较晚的父名会更新子记录器父链。
- `Logger(name, level=0)`：直接构造不登记到名字表。属性 name、level、disabled、propagate、parent、handlers；后者是活动列表。
- Logger 方法：setLevel、getEffectiveLevel、isEnabledFor、addHandler、removeHandler；debug/info/warning/warn/error/critical/fatal/log。
- `disable(level=CRITICAL)`：全局抑制小于等于给定等级的日志；`disable(NOTSET)` 解除通常的正等级抑制。
- `getLevelName(level)`：已知整数到标准名称、已知名称到整数；未知可哈希值返回 `Level ...`。不支持注册自定义等级名。
- 根对象类型为 RootLogger（继承 Logger）；默认 lastResort 类型为 _StderrHandler（继承 StreamHandler），后者动态读取 stderr。对应构造器分别接受必需 level 和默认 NOTSET 的 level。

## 基础配置与模块入口

`basicConfig` 仅接受关键字，支持 level、format、stream、force。根已有处理器且未 force 时不改变配置，也不校验剩余未知键。force 按列表快照逐个移除、调用 close，不关闭用户流。无处理器时创建 StreamHandler；默认格式 `%(levelname)s:%(name)s:%(message)s`。空 format 使用 Formatter 的默认消息格式。

未知键在安装处理器、设置等级后抛 ValueError，错误并不回滚已完成配置。filename/filemode/handlers/datefmt/style/encoding/errors 明确 NotImplementedError，不模拟文件或批量处理器配置。

模块级 debug/info/warning/warn/error/critical/fatal/log 在根无处理器时先默认 basicConfig，再委派根方法；即使最终日志被等级过滤也会完成默认配置。默认 StreamHandler 捕获创建时的 stderr，传入 stream 捕获对象引用；之后替换 sys.stderr 不改变已有普通处理器。

## 过滤、延迟格式与传播

调用签名先绑定：msg 缺失或重复在过滤前报 TypeError；msg 可用关键字。等级过滤通过后才创建记录、转换 msg 字符串和展开消息参数。`log` 等级须整数；已启用调用的额外 exc_info/extra/stack 等关键字明确不支持，关闭等级的调用不检查这些选项。

记录沿父链传播，应用各 handler 等级，不重复应用祖先 Logger 等级或 disabled。propagate=False 截断。每个处理器重新格式化；不缓存用户对象的字符串。当前 isEnabledFor 复用等级解析器，额外接受已知等级字符串；disabled/propagate 赋值时立即取真值，不承诺任意用户真值对象的延迟求值。

## StreamHandler 与 Formatter

- StreamHandler(stream=None)：setLevel、setFormatter、setStream、flush、close；属性 stream、level、formatter、terminator。
- close 不关闭流。setStream 先 flush 旧流，身份相同不操作。flush 调用当前流可用的 flush 方法。
- Formatter(fmt=None, datefmt=None, style='%', validate=True, *, defaults=None)：目前只支持默认辅助选项；其他值明确拒绝。
- 格式字段仅 name、levelname、levelno、message；消息 `%s/%r/%d/%%` 与 `%(key)` 映射版本可用。宽度/精度及其他转换尚不支持，不伪称完整 Python `%`；此实现不扩展全局字符串取模。
- 构造校验要求至少一个合法映射占位符；空 fmt 使用消息默认值。部分复杂格式可通过构造校验、在实际格式化时明确不支持。
- 暂无公开 LogRecord、Formatter.format(record)、Handler.emit/handle/format/handleError(record)、过滤器、异常/堆栈格式、FileHandler、配置文件接口。

## 错误与回退

格式化后才读取 stream；一次 write 写消息加 terminator，随后 flush 再读取当前 stream。普通 Python Exception 在 emit 中捕获；raiseExceptions 为真时向当前 stderr 写诊断，为假时静默。RecursionError 及 BaseException 的退出/中断类不吞掉。显式 flush 调用的错误正常传播。

诊断包含标记和异常 traceback，但尚未复制 CPython 的额外 Call stack/Message/Arguments 文本。未找到任何处理器时使用可配置 lastResort；默认 WARNING 阈值、动态当前 stderr。lastResort=None 且 raiseExceptions 为真时最多提示一次无处理器；自定义非 StreamHandler 回退对象暂不支持。

## 成本与验证

名字登记长期持有记录器；父链更新在线性扫描注册表时执行。关闭等级避免记录与格式化，方法属性读取仍创建可调用包装。每次启用记录按父链/处理器数量处理，字符串由处理器逐次生成；不引入异步队列或后台线程。

差分：`m3_7_logging.py`、`m3_7_logging_errors.py`、`m3_7_logging_basic.py`；专测：PyLoggingTests、PyLoggingConfigTests。覆盖身份、晚建父链、过滤时序、流切换、错误上下文、默认回退、重复配置/强制重配、关键字绑定及等级映射。完整 CLI 组合仍由原 `m3_7_cli_logging.py` 验收，不以单独 logging 通过冒称 argparse 已完成。
