# ADR0176：zlib/gzip 原生压缩与有界文件协议

- 日期：2026-11-13
- 状态：已实施；验收命令和结果见 [独立验收记录](../validation/zlib-gzip.md)。
- 归属：压缩模块独立决定。ADR0175 留给主代理的公共联合分发。

## 背景

HTTP 响应、base64 二进制数据和持久化应用需要可以跨 CPython 解压的 zlib/gzip
容器，也需要真实文件上下文。直接照搬 `.NET` 流的默认读取会宽松接受部分截断输入；
gzip 多成员和尾随数据还需要独立的容器边界。目标是复用成熟压缩算法，明确这些可观察合同。

## 决定

### 1. 算法交给 .NET，容器与 Python 协议自己负责

- `ZLibStream` 处理 RFC1950 zlib，`DeflateStream` 处理 RFC1951 raw DEFLATE，
  `GZipStream` 产生 RFC1952 gzip。使用 `.NET 10` 的 `ZLibCompressionOptions` 真正传递
  `CompressionLevel=-1..9`，没有把十一种输入折算成几档枚举。
- gzip 读取用独立 RFC1952 头尾解析器包住 `DeflateStream`，负责成员、CRC32、ISIZE 和错误类型。
  实现没有 DEFLATE 编解码器。
- `.NET` 从未写入正文的流可能不产生压缩数据。空输入使用固定的合法空终止块
  `03 00` 及对应 zlib/gzip 包装常量，覆盖空 API 结果和空文件关闭。
- CRC32 为 IEEE 多项式 `0xedb88320`，Adler32 为模 65521。共享内部入口
  `PyCompression.Crc32` 可在数据处理分支合并后复用；当前没有任何 `binascii` 类型依赖。

### 2. 逐 API 支持范围

| API | 合同 |
| --- | --- |
| `zlib.compress(data, /, level=-1, wbits=15)` | `level=-1..9`；`wbits=15/-15/31` 分别生成 zlib/raw/gzip；level、wbits 可按位置或关键字传入 |
| `zlib.decompress(data, /, wbits=15, bufsize=16384)` | `15/-15/31`；另支持 `0` 从 zlib 头确定、`47` 自动识别 zlib/gzip；bufsize 必须正整数，作为初始容量提示使用固定小缓冲实现 |
| `zlib.adler32(data, value=1, /)`、`zlib.crc32(data, value=0, /)` | 无符号 32 位结果；初始整数取低 32 位；支持链式计算 |
| `gzip.compress(data, compresslevel=9, *, mtime=None)` | 压缩级别 `-1..9`；显式时间戳写入头部四字节；默认当前 Unix 秒 |
| `gzip.decompress(data)` | 完整解压全部 gzip 成员；空输入返回空 bytes |
| `gzip.open(filename, mode='rb', compresslevel=9, encoding=None, errors=None, newline=None)` | 真实路径文件；`r/w/a/x` 可附 `b/t`；`a` 写新成员；构造前验证参数 |

数据接收 `bytes`、`bytearray` 和已有运行时连续 `IPyBufferView`（包括 memoryview）；
视图读取原始 buffer，释放后的视图抛 `ValueError`。字符串、整数列表等对象抛 `TypeError`。
返回值为 `bytes`。校验种子支持大整数和 `__index__`。

gzip 文件支持 `read/readline/readlines/write/writelines/flush/close`、迭代和 `with`。
二进制读取计数按字节，文本读取和写入计数按 Unicode 码点。文本支持
UTF-8、ASCII、Latin-1 的 strict 编解码，以及 `None/''/'\n'/'\r'/'\r\n'` newline。
读取时 compresslevel 只做合法性校验，实际解压由输入容器决定。

### 3. 时间戳与压缩输出只保留可观察合同

- `mtime=None` 使用当前时间；整数在 `0..4294967295` 内；有限非负浮点数向零取整。
  显式时间戳绝不静默丢弃。文件写入采用打开时的当前时间。
- 负数、非有限数或超过 uint32 的时间抛 `OverflowError`；错误类型和范围属于本轮明确边界。
  CPython 3.12 对部分越界值使用 `struct.error`，对 NaN 使用 `ValueError`，且可能接受
  向零取整后为零的小负浮点数。当前拒绝这些输入，避免隐藏截断和无效头部。
- gzip 的 MTIME、有效 CRC32/ISIZE、标准容器与跨解压结果是合同。
  OS、XFL、FNAME、DEFLATE 块布局、压缩长度和完整压缩字节允许与 CPython 不同。
  需要稳定业务身份时，应使用原始业务数据，而不是依赖压缩器的输出布局。
- `mtime=0` 在当前同版本运行时可重复生成；跨 SDK/平台/压缩实现升级没有逐字节承诺。

### 4. 截断、多成员和尾随数据

- zlib 模块只处理首个流或首个 gzip 成员；完整流之后的尾随字节忽略，与 CPython 单次
  `zlib.decompress` 一致。输入结束但解码器尚未完成时抛 `zlib.error`。
- gzip 模块处理全部成员、成员后的零填充；开头的零填充、非零垃圾尾部拒绝。
  成员头尾截断为 `EOFError`；magic、CRC32、ISIZE 错误为 `gzip.BadGzipFile`；
  DEFLATE 本体损坏为 `zlib.error`。
- 支持 FEXTRA、FNAME、FCOMMENT、FHCRC。gzip 模块按 CPython 3.12 跳过 FHCRC 并忽略
  头保留位；zlib 的 gzip 模式核对 FHCRC 并拒绝保留位。
- 包装输入每次向 .NET 解码器供给一个字节，阻止预读吞掉尾部/下一成员，并在非完成状态
  源 EOF 时主动报错。该选择增加每个压缩字节的托管调用成本；后续优化必须继续满足
  全前缀截断、多成员、尾随数据矩阵。

### 5. 资源与内存

- 每次一次性解压、每个 gzip 文件的累计解压量上限为 **64 MiB**，包含所有成员；
  超限抛 `MemoryError`。文件分段 read 也无法绕过累计限制。
- FNAME、FCOMMENT 各上限 **1 MiB**，超限抛 `MemoryError`。读取使用 8 KiB 小缓冲。
- 64 MiB 约束累计解压字节。内存流扩容、返回 bytes 的复制、输入 buffer 副本仍有额外
  内存成本；这项限制直接阻止无界解压，未提供固定 RSS 预算。
- 构造参数失败先于建文件；包装构造失败、正常/异常 `with` 退出、显式重复 close 都释放句柄。
  写流 close 完成压缩尾部；错误 read 在 `with` 中退出也会关闭底层文件。
- 普通路径错误保留 `FileNotFoundError/NotADirectoryError/IsADirectoryError/FileExistsError` 等
  既有运行时映射。关闭后的操作抛 `ValueError`。

### 6. 明确拒绝与未提供项

- 小窗口 `8..14/-14..-8/24..30` 和解压自动小窗口 `40..46` 抛 `NotImplementedError`；
  不合法 wbits、level 抛 `zlib.error`。没有接受参数后仍固定使用不相符窗口。
- 不提供 `compressobj/decompressobj`、字典、strategy、增量 flush、`GzipFile` 构造器、fileobj。
  未登记属性按通常 `AttributeError/ImportError` 失败；未知关键字抛 `TypeError`。
- 文件 seek/truncate 和文本 tell 抛 `io.UnsupportedOperation`，`seekable()` 为假；
  二进制 tell 返回累计解压/写入位置。没有伪造 seek 或 text cookie。
- 文本非 strict 错误策略抛 `NotImplementedError`；不支持的编码抛 `LookupError`；
  更新模式、重复/混合标记抛 `ValueError`。
- 两个模块异常可构造、捕获并查询类型身份；本轮没有扩展原生异常的用户派生布局。

## 接线与验收

运行时原生模块注册、异常工厂/继承/身份匹配、HIR 内建与导入异常认证、MIR/codegen 异常
引用表保持同步。没有新增 RuntimeFn 调用，没有修改 driver 部署逻辑。
压缩/文件帮助类保持 internal，公开 CLR ABI 仅新增三个真实异常及四个构造签名（共七项）；
精确 ABI 允许表继续拒绝缺项、改签、重复与未知项。

新 e2e 先 pending，固定 CPython 3.12.13 差分成功后转正；独立 C# 测试验证 .NET 互操作、
所有截断前缀和内存上限。独立脚本再执行 CP→JIT/Native 解压及反向解压，并把真实
NativeAOT 分发搬到中文含空格目录，删除本轮源码与原构建，在新 cwd、清理运行时环境变量
和空工具 PATH 后启动。验收按串行运行，结果见独立记录。

参考规范：CPython 3.12 `zlib`/`gzip` API、RFC1950/1951/1952、
.NET 10 `System.IO.Compression` 与 `ZLibCompressionOptions`。
