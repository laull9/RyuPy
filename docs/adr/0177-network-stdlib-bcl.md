# ADR-0175：URL 与 HTTP/TLS 的静态 BCL 纵切

日期：2026-11-13。状态：本地逐 API、HTTP/TLS 差分与 NativeAOT 搬迁验收完成。

## 决定

URL 拆分、合并、转义保留 CPython 的字符串可观察语义；System.Uri 仅用于实际
HTTP 传输地址验证，禁止用其规范化结果替换 urlsplit/urlparse 的输出。
请求使用 HttpClient/SocketsHttpHandler 与系统 TLS，禁止 libpython、重写协议、
自动关闭证书验证。显式 SSLContext 仅开放可信 CA 与强制主机名校验，未实现参数明确拒绝。

模块与对象使用现有 PyNativeFunctions/PyNativeObject 静态分派；异常是真实 CLR
Python 异常族，保持 HTTPError 的可读响应体及 URLError/OSError 继承。
网络适配类型全部为 internal，经既有对象入口访问，保持 runtime 的 1737 条公开 ABI 合同。
重定向显式处理，限制次数、只接受 HTTP(S)，跨源移除敏感凭据。
响应按流读取，close/with 释放响应、客户端与取消令牌，超时覆盖响应体读取。

## 范围与验收

逐 API 状态、未开放参数及本地 HTTP/TLS/NativeAOT 搬迁证据写入
`docs/validation/network-stdlib.md`。不修改部署驱动与公共路线清单。
Python URL bytes 结果族、代理/cookie/auth handlers、HTTPConnection 底层接口、
TLS 服务端与任意 verify_mode 均独立后续纵切。
