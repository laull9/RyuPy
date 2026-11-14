// 独立CLR迭代器探针：Python的__next__不能分开观察Next/Current，因此从公开既有ABI注入。
// 加载本轮实际PE，不改运行时、不模拟holder，验证未使用目标仍读取Current并保留异常顺序。
using System.Reflection;
using System.Text.Json;
using RyuPy.Runtime;

if (args.Length != 2) throw new ArgumentException("需要本轮生成的DLL路径和场景名");
var assembly = Assembly.LoadFrom(Path.GetFullPath(args[0]));
var main = assembly.EntryPoint ?? throw new Exception("PE缺少入口");
main.Invoke(null, main.GetParameters().Length == 0 ? null : [Array.Empty<string>()]);
var subject = PyModules.Import("subject");
var iterator = new ProbeIterator(subject, args[1]);
object seed = (long)1234567;
object? result = null;
string? failure = null;
try { result = PyOps.CallValue(subject.GetAttr("fold"), [seed, iterator], [], []); }
catch (PyValueError error) { failure = error.Message; }
var expected = args[1] switch {
    "normal" => new[] {"next:0", "current:0", "next:1", "current:1", "next:2", "current:2", "next:3"},
    "current-error" => new[] {"next:0", "current:0", "next:1", "current:1"},
    "next-error" => new[] {"next:0", "current:0", "next:1"},
    "empty" => new[] {"next:0"},
    _ => throw new Exception("未知场景"),
};
if (!iterator.Events.SequenceEqual(expected)) throw new Exception("协议顺序错误");
if (args[1] == "normal" && !Equals(result, (long)1234627)) throw new Exception("Current后的全局读取被移动");
if (args[1] == "empty" && !ReferenceEquals(seed, result)) throw new Exception("空循环破坏seed对象身份");
var expectedFailure = args[1] switch { "current-error" => "current explosion", "next-error" => "next explosion", _ => null };
if (failure != expectedFailure) throw new Exception("异常位置或内容错误");
Console.WriteLine(JsonSerializer.Serialize(new { scenario = args[1], events = iterator.Events, failure,
    result = result?.ToString(), same_seed = ReferenceEquals(seed, result) }));

sealed class ProbeIterator(PyModule subject, string scenario) : IPyIterator
{
    int index = -1;
    public List<string> Events { get; } = [];
    public bool MoveNext()
    {
        index++;
        Events.Add($"next:{index}");
        if (scenario == "next-error" && index == 1) throw new PyValueError("next explosion");
        return scenario != "empty" && index < 3;
    }
    public object? Current()
    {
        Events.Add($"current:{index}");
        subject.SetAttr("TOKEN", (long)((index + 1) * 10));
        if (scenario == "current-error" && index == 1) throw new PyValueError("current explosion");
        return new object(); // loop目标完全不用；仍不能省去本调用。
    }
}
