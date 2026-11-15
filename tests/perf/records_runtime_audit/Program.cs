// 审计实际编译输入/发布Runtime，不加载执行被审程序集。
using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using System.Security.Cryptography;
using System.Text.Json;
if (args.Length != 2 || args[1] is not ("normal" or "diagnostic")) throw new ArgumentException("DLL normal|diagnostic");
using var stream = File.OpenRead(args[0]);
using var pe = new PEReader(stream);
var md = pe.GetMetadataReader();
var handles = md.TypeDefinitions.Where(h => {
    var t = md.GetTypeDefinition(h);
    return md.GetString(t.Namespace) == "RyuPy.Runtime" && md.GetString(t.Name) == "PyRecordsIncrement";
}).ToArray();
if (handles.Length != 1) throw new InvalidDataException("guard type missing/duplicate");
var type = md.GetTypeDefinition(handles[0]);
var fields = type.GetFields().Select(h => md.GetString(md.GetFieldDefinition(h).Name)).Order().ToArray();
var methods = type.GetMethods().Select(h => md.GetString(md.GetMethodDefinition(h).Name)).Order().ToArray();
string[] diagnosticFields = ["DiagnosticsReady", "_diagnosticFast", "_diagnosticFallback"];
string[] diagnosticMethods = ["RegisterDiagnostics", "Observe"];
bool diagnostic = args[1] == "diagnostic";
foreach (var name in diagnosticFields) if (fields.Contains(name) != diagnostic) throw new InvalidDataException(name);
foreach (var name in diagnosticMethods) if (methods.Contains(name) != diagnostic) throw new InvalidDataException(name);
// 编译器生成的ProcessExit lambda只有诊断配置可存在；普通guard仅有缓存cctor与TryMaterialized。
if (!diagnostic && methods.Any(n => n != ".cctor" && n != "TryMaterialized")) throw new InvalidDataException("unexpected normal guard method");
Console.WriteLine(JsonSerializer.Serialize(new { mode=args[1], sha256=Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))).ToLowerInvariant(), fields, methods }));
