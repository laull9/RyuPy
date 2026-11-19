// ssl 的有限客户端上下文：系统验证默认开启，自定义 CA 仅改变信任根，绝不绕过主机名验证。
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace RyuPy.Runtime;

/// <summary>面向 urllib HTTPS 的最小强验证 SSLContext。</summary>
internal static class PySsl
{
    private static readonly PyNativeObject ServerAuth = new Purpose();
    /// <summary>注册真实可用的上下文工厂及验证常量。</summary>
    public static Dictionary<string, object?> CreateAttributes()
    {
        var purpose = new PyNativeType("Purpose", "ssl", null, value => value is Purpose);
        purpose.SetAttr("SERVER_AUTH", ServerAuth);
        return new(StringComparer.Ordinal)
        {
            ["CERT_REQUIRED"] = 2L, ["CERT_NONE"] = 0L, ["CERT_OPTIONAL"] = 1L,
            ["Purpose"] = purpose,
            ["SSLError"] = PyType.Get("SSLError"), ["SSLCertVerificationError"] = PyType.Get("SSLCertVerificationError"),
            ["SSLContext"] = new PyNativeType("SSLContext", "ssl", null, value => value is PySslContext),
            ["create_default_context"] = PyNativeFunctions.Create("create_default_context",
                ["purpose", "cafile", "capath", "cadata"], [ServerAuth, null, null, null], 0, args =>
                {
                    if (!ReferenceEquals(args[0], ServerAuth)) throw new PyNotImplementedError("only SERVER_AUTH contexts are implemented");
                    var context = new PySslContext(); context.Load(args[1], args[2], args[3]); return context;
                }, ["pos", "kwonly", "kwonly", "kwonly"]),
        };
    }
    private sealed class Purpose : PyNativeObject
    {
        public override string TypeName => "Purpose";
        public override object? GetAttr(string name) => name == "name" ? "SERVER_AUTH" : throw Missing(name);
    }
}

/// <summary>可信 CA 配置；拒绝验证降级及未实现的 TLS 参数。</summary>
internal sealed class PySslContext : PyNativeObject
{
    private readonly List<byte[]> roots = [];
    /// <inheritdoc/>
    public override string TypeName => "SSLContext";
    internal void Load(object? cafile, object? capath, object? cadata)
    {
        if (!PyUrlParse.None(capath)) throw new PyNotImplementedError("SSL capath is not implemented; use cafile or PEM cadata");
        var certificates = new X509Certificate2Collection();
        try
        {
            if (!PyUrlParse.None(cafile)) certificates.ImportFromPem(File.ReadAllText(PyUrlParse.Text(cafile)));
            if (!PyUrlParse.None(cadata))
            {
                if (cadata is not string pem) throw new PyNotImplementedError("SSL cadata currently requires PEM str");
                certificates.ImportFromPem(pem);
            }
            if ((!PyUrlParse.None(cafile) || !PyUrlParse.None(cadata)) && certificates.Count == 0)
                throw new PySSLError("no certificate or crl found");
            foreach (X509Certificate2 certificate in certificates) roots.Add(certificate.RawData);
        }
        catch (CryptographicException) { throw new PySSLError("invalid CA certificate data"); }
        catch (IOException) { throw new PyOSError("unable to read CA certificate file"); }
        catch (UnauthorizedAccessException) { throw new PyPermissionError("unable to read CA certificate file"); }
        catch (ArgumentException) { throw new PyValueError("invalid CA certificate path or data"); }
        finally { foreach (X509Certificate2 certificate in certificates) certificate.Dispose(); }
    }
    internal List<X509Certificate2> Configure(SslClientAuthenticationOptions options)
    {
        var certificates = new List<X509Certificate2>();
        if (roots.Count == 0) return certificates;
        // BCL 继续执行 hostname、有效期、用途和签名验证；只用明确载入的 CA 信任链。
        var policy = new X509ChainPolicy
        {
            TrustMode = X509ChainTrustMode.CustomRootTrust,
            RevocationMode = X509RevocationMode.NoCheck,
        };
        foreach (byte[] root in roots)
        {
            var certificate = X509CertificateLoader.LoadCertificate(root);
            certificates.Add(certificate); policy.CustomTrustStore.Add(certificate);
        }
        options.CertificateChainPolicy = policy;
        return certificates;
    }
    /// <inheritdoc/>
    public override object? GetAttr(string name) => name switch
    {
        "check_hostname" => true, "verify_mode" => 2L,
        "load_verify_locations" => PyNativeFunctions.Create(name, ["cafile", "capath", "cadata"], [null, null, null], 0,
            args => { if (Array.TrueForAll(args, PyUrlParse.None)) throw new PyTypeError("cafile, capath and cadata cannot be all omitted"); Load(args[0], args[1], args[2]); return null; }),
        _ => throw Missing(name),
    };
    /// <inheritdoc/>
    public override void SetAttr(string name, object? value) => throw new PyNotImplementedError("SSLContext configuration is limited to verified CA loading");
}
