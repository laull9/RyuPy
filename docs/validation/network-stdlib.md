# Python 网络标准库有限纵切验收

日期：2026-11-13。基线：CPython 3.12.13、macOS arm64、.NET 10。
设计见模块独立 [ADR-0175](../adr/0177-network-stdlib-bcl.md)。主代理的公共分发 ADR 使用相同编号，合并时可重编号本模块 ADR。

## 1. 架构与调用合同

七个模块 `urllib / urllib.parse / urllib.request / urllib.error / http / http.client / ssl`
通过 `PyNativeModules` 显式静态注册。请求、响应、SSLContext、头集合走
`PyNativeObject`；URL 结果保留真实 tuple 布局。普通调用、保存方法、异常捕获和
context manager 共用 Python 属性分派，NativeAOT 无运行时 Python 或反射加载要求。
HIR/MIR 只增加内建 `TimeoutError` 名称；codegen 与 driver 部署逻辑保持原样。
网络实现类型全部为 internal，复用既有公开对象 ABI；公开签名总数仍为 1737。

URL 字符串操作由小型语义适配层完成。实际 HTTP 使用 BCL `HttpClient / SocketsHttpHandler`，
TLS 使用其 `SslStream` 路径和系统密码库。`System.Uri` 验证地址和 authority，传输时保留
点段、百分号路径与 query，fragment 不发送。没有引入 HTTP/TLS 自制协议栈、libpython 或密码库替代实现。

## 2. urllib.parse 逐 API

以下入口均有 CPython stdout 差分；结果类型、索引、方法另有 C# 定向测试。

| API | 已验合同 | 明确范围 |
| --- | --- | --- |
| `urlsplit` | scheme 大小写、authority、path/query/fragment、控制符清理、allow_fragments、user/password/hostname/port、IPv6/IPvFuture/NFKC 拒绝 | URL 与默认 scheme 为 str；bytes 结果族未开放 |
| `urlparse` | 六字段及最后路径段 params、allow_fragments、geturl | 与 urlsplit 相同的 str 域 |
| `SplitResult / ParseResult` | tuple/result 身份、repr/str、len、索引、count/index、保存 geturl | 直接构造、`_replace/_asdict/encode` 未开放 |
| `urljoin` | 43 项普通/异常相对引用、点段、绝对/网络路径、query/fragment、gopher/svn+ssh/mms/mailto | str 域，CPython 3.12 固定 relative/netloc/params scheme 集合 |
| `quote` | UTF-8 文本、bytes 输入、safe str/bytes、非 ASCII safe、latin-1 | encoding 为 UTF-8/ASCII/Latin-1；errors 为 strict/replace/ignore |
| `quote_plus` | 空格 → `+`、字面 `+` → `%2B` | 同 quote |
| `unquote` | 畸形 `%` 原样保留、UTF-8 替代字符、文本与转义混合、latin-1 | str/bytes 输入，输出 str；严格错误为 UnicodeDecodeError |
| `unquote_plus` | `+` → 空格，转义 `+` 保留 | 输入 str |
| `urlencode` | dict、二元 pair 序列、重复键、数值、`doseq` list/tuple、空序列、safe/encoding/errors | 自定义 quote_via 明确 NotImplementedError；任意自定义 mapping/sequence 协议未验收 |
| `parse_qs` | 重复键、空值、flag、UTF-8/plus、separator、strict_parsing、max_num_fields | str → dict[str,list[str]]；bytes 查询族未开放 |

纯本地 e2e：`tests/e2e/phase3/stdlib_network_parse.py`。
未实现的模块属性和结果方法抛 AttributeError，已暴露工厂的未实现构造抛 NotImplementedError。

## 3. 请求、响应及错误逐入口

| API / 属性 | 已验合同 | 限制 |
| --- | --- | --- |
| `Request(url, data, headers, method)` | GET/POST 自动选择、显式 HEAD/POST、str URL、bytes body、真实可写 headers dict、data/full_url 写入 | data 为 bytes/None，headers 为 dict；非默认 origin_req_host/unverifiable 拒绝 |
| `get_method/get_full_url` | 默认方法与原 URL | 高阶 Request 派生协议未开放 |
| `add_header/add_unredirected_header` | 规范化字段名、值校验 | 非 str、无效 token、CR/LF/NUL 拒绝 |
| `get_header/has_header/remove_header/header_items` | CPython 大小写规则、默认值、实时读取字典、删除 | 直接字典改写在发送前再次校验 |
| `urlopen(url, data=None, timeout=None, context=None)` | loopback GET/POST/HEAD、bytes body、HTTP 1.1 | 仅直接 HTTP(S)，ASCII URL；非 ASCII path 先 quote；不支持 file/ftp/data、URL 内嵌 credentials |
| `timeout` | 响应头/响应体超时 → TimeoutError；各次流读取设期限；真实连接拒绝 → URLError | 连接等待超时实现 URLError(reason=TimeoutError) 映射，尚无确定性黑洞连接差分；timeout=0 明确拒绝；None 无限等待；BCL 响应头阶段采用单次整体期限 |
| `HTTPResponse.status/code/reason/url/version` | 200、404、302、HEAD，HTTPResponse 身份 | version 为 10/11；只发送 HTTP/1.1 |
| `getcode/geturl/info` | code、最终重定向 URL、headers 同一对象 | 无 socket/fileno 底层接口 |
| `read(amt=None)` | bytes、分段、全读、EOF、重复读、HEAD、限长短读、Content-Length 提前结束的 IncompleteRead(partial,expected) | 不开放 readinto/readline/迭代行；普通读失败为 Python OSError/HTTPException |
| `close/closed/__enter__/__exit__` | 幂等 close、正常/异常 with 释放、closed 后 read 空 bytes | 资源需显式 close/with；GC 析构时机遵守项目现有边界 |
| `headers` | 大小写无关索引/get/get_all/contains，items/keys/values/len/iter 入口 | 只读；BCL 按字段归组，跨字段原始线序未承诺 |
| `getheader/getheaders` | getheader 重复字段用 `, ` 合并、缺失 default；getheaders 返回 pair list | HTTPMessage.get/索引仍返回首值 |
| `HTTPError` | URLError/OSError 层次、code/status/reason/msg/url/filename/headers/fp、空 args、可读 body、close/with、动态捕获 | 公开构造 fp 只接受本实现 HTTPResponse/None |
| `URLError` | 保留 reason 对象、args、字符串格式、OSError 捕获 | 连接/DNS失败文案稳定归一化，平台 errno 未开放 |
| `http.client` | HTTPResponse/HTTPMessage 类型、HTTPException/InvalidURL/IncompleteRead、OK/NOT_FOUND | HTTPConnection/HTTPSConnection、socket 控制、完整状态常量表未开放 |

响应体维持原始 bytes，绝不自动解压；供后续“HTTP → 解码/解压 → SQLite”应用显式串联。
每次 urlopen 独立拥有连接池与信任证书，close/with 释放整组资源。

## 4. 重定向与 TLS 安全合同

- 301/302/303：GET 跟随，POST 改 GET 并去掉内容头；307/308：GET 跟随、POST 抛可读 HTTPError。
- 重定向最多 10 次、同一目标最多 4 次；相对 Location 走 urljoin；unsupported scheme 拒绝。
- 跨源移除 Authorization、Cookie、Proxy-Authorization、Host；unredirected headers 不传给重定向。
- HTTPS → HTTP 主动拒绝并抛 URLError。CPython 默认可降级、也可能跨源保留用户凭据；本实现选择强安全合同。
- 系统默认 CA/hostname 验证始终开启。临时私有 CA 默认失败，显式信任后成功，错误 hostname 仍失败。
- `ssl.create_default_context(Purpose.SERVER_AUTH, *, cafile=None, cadata=None)`、`load_verify_locations` 支持 PEM cafile/str cadata。
- 自定义 CA 使用 BCL CustomRootTrust，仅改变信任根；不关闭链、有效期、用途或 hostname 验证；不主动启用 CRL。
- `check_hostname=True`、`verify_mode=CERT_REQUIRED` 可读。任意 SSLContext 属性写入均明确 NotImplementedError。
- capath、DER bytes cadata、任意 protocol/ciphers/client cert/service context、SSLContext 直接构造未开放。
- `urlopen` 的旧 cafile/capath/cadefault 非默认配置明确拒绝，请显式传 context。未知参数由 Python 绑定器拒绝。
- 忽略环境代理，不使用 cookie jar、系统代理、自动 auth handler、自动内容解压。应用要用代理/认证处理器时须独立纵切。

证书失败 → URLError(reason=SSLCertVerificationError)，其它握手失败 → URLError(reason=SSLError)。
平台 TLS/连接错误文本归一化。TLS 握手期间的超时目前进入 TimeoutError，尚未单独区分握手与响应头阶段。

## 5. 可重跑验收与资源清理

```sh
export CARGO_TARGET_DIR="$PWD/target/network/cargo"
export TMPDIR="$PWD/target/network/tmp"
export RUST_TEST_THREADS=1
export PYTHON="$(uv python find 3.12.13)"
export RYUPY_FOREACH_PYTHON="$PYTHON"
export PATH="$(dirname "$PYTHON"):$PATH"
mkdir -p "$TMPDIR"
make runtime
cargo build -p ryupy-cli
cargo test -p ryupy-e2e -- stdlib_network --test-threads=1
dotnet test runtime/RyuPy.Runtime.Tests --filter FullyQualifiedName~PyNetworkTests
"$(uv python find 3.12.13)" tests/network/run_network.py \
  --ryupy "$CARGO_TARGET_DIR/debug/ryupy" --aot --report target/network/http-tls-aot.json
make check
```

所有构建与测试串行。本 harness 仅绑定 127.0.0.1，由 openssl 在私有临时目录生成
短期 CA 和 localhost SAN 证书；不会修改系统信任，不连接外网，不关闭验证。
两台 HTTP 和一台 TLS server 的连接超时为 3 秒，工作线程在 shutdown/server_close/join 后退出。
命令默认 90 秒、openssl 30 秒、编译 600 秒期限；POSIX 超时清理完整独立进程组。
异常退出也清理临时目录和监听 socket，非预期服务器异常使测试失败。

NativeAOT 用本 worktree CLI 编译到 `client.dist`，复制到带中文和空格的新目录；
删除本轮 Python 源码、CIL 构建目录及原始 AOT 构建/部署目录；清理 DOTNET_/RYUPY_ 环境变量，
在新 cwd 直接运行原生文件，比较与同次 CPython 的完整 stdout，再跑安全拒绝合同。
发布目录随原生文件整体搬迁。当前平台验收为 macOS arm64，Linux/Windows 留待相同 harness 平台运行。

本任务未新增普通文件 hash 验证流程，未更改 driver 部署、公共 TODO、roadmap、stdlib 清单。

## 6. 本轮实际结果（2026-11-13）

- `make check` 最终退出 0：fmt、clippy、Rust 单元/集成/文档测试通过。
- C# 全套 3036 通过、2 跳过；原有四项公开 ABI 产物合同全部通过，签名总数保持 1737。
- CPython 3.12.13 单文件基线 723 通过；清单自检 9 通过、项目门禁自检 22 通过。
- `cases-selftest` 726 通过；正式 e2e 446 通过、284 跳过，两个网络用例均已去 pending 并通过真实差分。
- 网络 C# 定向 11/11 通过；全套回归再次运行该组。
- 最终 `run_network.py --aot` 退出 0，报告为 `cil=passed`、`boundaries=passed`、`relocated_native_aot=passed`。
  正例完整 stdout 与同次 CPython 相等；安全拒绝合同在 CIL 与搬迁原生进程间相等。
- 本轮实际平台仅 macOS arm64。跨模块 HTTP → 解码/解压 → SQLite 应用及整合评审由主代理执行。

本 worktree 本地日志为 `target/network/check.log`、`target/network/unit.log`、
`target/network/aot.log`，结构化网络结果为 `target/network/http-tls-aot.json`。
测试 CA、私钥、监听服务、Python 源副本及编译/搬迁临时目录在 harness 退出时清理。
现有门禁的 4 项旧 frame/diamond 实验和 2 项历史 ABI 基线缺省跳过维持原配置。
已知范围仍按逐 API 表执行：连接黑洞超时尚无确定性差分，TLS 握手超时专用包装未开放；
代理、cookie/auth handlers、HTTPConnection、URL bytes 结果族与高级 SSL 参数保持明确未开放。
