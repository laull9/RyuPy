// 单个 urlopen 的传输资源所有者；信任根随连接池释放，严格验证回调只记录失败类别。
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace RyuPy.Runtime;

internal sealed class PyHttpClient : HttpClient
{
    private readonly List<X509Certificate2> certificates;
    internal bool CertificateRejected { get; private set; }
    internal bool Connecting { get; private set; }
    internal bool ConnectTimedOut { get; private set; }
    internal PyHttpClient(SocketsHttpHandler handler, PySslContext? context) : base(handler)
    {
        Timeout = System.Threading.Timeout.InfiniteTimeSpan;
        certificates = context?.Configure(handler.SslOptions) ?? [];
        handler.ConnectCallback = Connect;
        // 使用 BCL 已计算的证书链和 hostname 结果。仅 None 可通过，绝不降级验证。
        handler.SslOptions.RemoteCertificateValidationCallback = (_, _, _, errors) =>
        {
            CertificateRejected = errors != SslPolicyErrors.None;
            return !CertificateRejected;
        };
    }
    // 只委托 BCL 建立 TCP；区分连接阶段和响应阶段的超时，维持 urllib 的异常包装合同。
    private async ValueTask<Stream> Connect(SocketsHttpConnectionContext context, CancellationToken cancellation)
    {
        Connecting = true;
        var socket = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        try
        {
            await socket.ConnectAsync(context.DnsEndPoint, cancellation).ConfigureAwait(false);
            return new NetworkStream(socket, ownsSocket: true);
        }
        catch (OperationCanceledException) { ConnectTimedOut = true; socket.Dispose(); throw; }
        catch { socket.Dispose(); throw; }
        finally { Connecting = false; }
    }
    protected override void Dispose(bool disposing)
    {
        try { base.Dispose(disposing); }
        finally
        {
            if (disposing) { foreach (var certificate in certificates) certificate.Dispose(); certificates.Clear(); }
        }
    }
}
