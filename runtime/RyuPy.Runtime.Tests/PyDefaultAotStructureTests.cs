// 只测v3纯结构，不模拟Native平台、注册对象或声称父路径执行通过。
using System.Text.Json;
using System.Text.Json.Nodes;
using RyuPy.Runtime;
using Xunit;
namespace RyuPy.Runtime.Tests;
public class PyDefaultAotStructureTests
{
    private static JsonObject Plan() => JsonNode.Parse("""
        {"schema_version":3,"roots":["sample"],"dependencies":{},"locations":{"sample":
          {"package":"","file":"sample.py","kind":2,"path":"_internal/resources/v1/modules/sample.py",
           "is_package":false,"registration":"deferred"}},"deferred":{"sample":"unsupported"},
         "outside":[],"protected":[],"closed_unplanned":[],"external_dependencies":[]}
        """)!.AsObject();
    private static void Validate(JsonObject value)
    {
        using var doc = JsonDocument.Parse(value.ToJsonString());
        PyModules.ValidateDefaultSourceStructure(doc.RootElement);
    }
    [Fact]
    public void V3PureShapeAcceptsValidStructureWithoutGrantingAuthority() => Validate(Plan());
    [Theory]
    [InlineData("package")]
    [InlineData("file")]
    [InlineData("path")]
    [InlineData("registration")]
    public void NullSourceFieldsAreRejectedBeforePlatformResolution(string field)
    {
        var plan = Plan(); plan["locations"]!["sample"]![field] = null;
        Assert.Throws<PyValueError>(() => Validate(plan));
        Validate(Plan());
    }
    [Fact]
    public void OldKindAndUnknownFieldsCannotAcquireV3Shape()
    {
        var plan = Plan(); plan["locations"]!["sample"]!["kind"] = 1;
        Assert.Throws<PyValueError>(() => Validate(plan));
        plan = Plan(); plan["extra"] = true;
        Assert.Throws<PyValueError>(() => Validate(plan));
        plan = Plan(); plan["locations"]!["sample"]!["package"] = "wrong";
        Assert.Throws<PyValueError>(() => Validate(plan));
    }
}
