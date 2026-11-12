# ADR-0172：有限标准输出 detach 与二进制所有权

状态：实现及 managed 差分通过；真实应用 Native 交付另行验收。

## 背景

pyfiglet 1.0.2 原 CLI 已完成解析与字体渲染，却在 `sys.stdout.detach()` 停止。仅返回新流或把 bytes 解码后写回文本会破坏缓冲顺序、对象身份和任意字节。该缺口属于通用标准流协议，不通过修改上游或替换 stdout 绕过。

## 决策

真实进程 stdout/stderr 的 `PyStandardStream` 关联一个稳定的内部 `PyStandardBinaryOutput`。它直接向 raw Stream 写入 bytes，不作文本解码或换行翻译；任意二进制数据不丢失。新载体及构造保持内部可见，不增加公开 ABI；已有三参文本包装构造不变。

`buffer` 在附着期间返回同一对象。`detach()` 先检查状态、刷新文本，成功后才将旧 wrapper 标记 detached，并返回该 buffer；刷新失败保持附着状态。detached 不等于 closed，也不关闭二进制对象。调用方通常将 `sys.stdout` 重新绑定为返回对象。

CPython 3.12.13 合同：detach 后旧 wrapper 的 `buffer` 为 `None`，`encoding` 仍可读；`name`、`closed`、写入、刷新、关闭、再次 detach 和能力查询抛 `ValueError: underlying buffer has been detached`。二进制 `write` 返回字节数、拒绝 text 和关键字参数。closed 二进制的 `readable()` 仍为 False；写入、刷新及其它能力查询遵守各自已验证的关闭异常。

## 关闭与宿主所有权

raw OS 句柄由进程/宿主拥有，Python carrier 的 close 只关闭其逻辑状态并刷新，不销毁借用句柄。关联 wrapper 立即观察 buffer 的 closed 状态；wrapper close 也关闭关联 carrier。detached wrapper 无权通过 close 再关闭已移交 carrier。

此策略不承诺操作系统文件描述符已被关闭，也不开放 fileno/raw/seek/read 等未实现能力。禁止用“逻辑 closed”声称完整 CPython fd 所有权等价。宿主直接使用句柄不在 Python carrier 的逻辑状态管理范围内。

退出刷新保留当前流重绑语义，跳过已逻辑关闭的输出，不能因旧 `__stdout__` wrapper 已 detached 而误伤正常 binary 重绑。若用户仅 detach 却未重绑当前 stdout，实际产物与 CP 均退出 120；CLI `run` 对非零子进程仍归一为 1。错误正文相同，但沿用既有运行时 `Exception ignored while flushing standard streams:` 前缀，未模拟 CP 对象 repr 前缀；这一负图不计作逐字节差分通过。

## 明确边界

- stdin 存在文本预读与待处理换行状态，本切不提供无损 raw 转交。
- 只有 TextWriter 的宿主自定义包装没有可靠 raw carrier，`buffer`/`detach` 明确拒绝，不伪造编码桥。
- 磁盘 `PyIOTextFile.detach`、完整 BufferedWriter/一般 io 协议不随本切开放。
- 不修改全局退出错误报告格式，不加入项目名特判。

## 验证

持久差分入口见 `tests/cases/standard_output_detach/`：stdout 协议、stderr 协议和 closed 状态。新增 C# 测试覆盖稳定身份、失败刷新不半转交、关闭联动、任意 bytes 与宿主句柄仍存活。

本轮完整 C# 为 3010 通过、2 个既有 skip；公开正图双 PIC 与 CP 的完整双流一致。独立 raw 字节图包含 NUL、0xff、0x80，按字节比较，不先解码。pyfiglet 原 CLI 与真实 metadata entry 的标准字体完整渲染、默认 site 下 `--list_fonts` 549 行均在 managed 双 PIC 完整匹配 CP；不是 Native 完成声明。所有失败原件（含已修 ABI 总数失败和 ENOSPC）保留，不以成功前缀代替整图。
