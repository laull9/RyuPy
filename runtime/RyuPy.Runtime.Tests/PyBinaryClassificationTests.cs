// ASCII分类的值域与活读专项；黄金位图和32样本来自唯一固定CP3.12.13原流，不用实现公式生成期待。
using System.Text;
using Xunit;

namespace RyuPy.Runtime.Tests;

[Collection("生成器帧栈")]
public sealed class PyBinaryClassificationTests
{
    private static readonly (string Name, string Bitmap)[] SingleByteCases =
    [
        ("isalpha", "0000000000000000000000000000000000000000000000000000000000000000011111111111111111111111111000000111111111111111111111111110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"),
        ("isalnum", "0000000000000000000000000000000000000000000000001111111111000000011111111111111111111111111000000111111111111111111111111110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"),
        ("isdigit", "0000000000000000000000000000000000000000000000001111111111000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"),
        ("isspace", "0000000001111100000000000000000010000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"),
        ("isascii", "1111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"),
        ("islower", "0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000111111111111111111111111110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"),
        ("isupper", "0000000000000000000000000000000000000000000000000000000000000000011111111111111111111111111000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"),
        ("istitle", "0000000000000000000000000000000000000000000000000000000000000000011111111111111111111111111000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000"),
    ];
    private static readonly string[] MixedHex =
    [
        "", "61626358595a", "616263313233", "30313233343536373839", "20090a0b0c0d", "1c1d1e1f", "007f", "80ff", "85a0", "6180", "41ff", "6141", "613021", "413021", "31323321", "4162204364", "4142", "6162", "412042", "413142", "413162", "4162ff4364", "4162ff6364", "410042", "410062", "415f42", "415f62", "412762", "412742", "61626300", "3961", "610a",
    ];
    private static readonly (string Name, string Bitmap)[] MixedCases =
    [
        ("isalpha", "01000000000100001100000000000000"),
        ("isalnum", "01110000000100001101100000000010"),
        ("isdigit", "00010000000000000000000000000000"),
        ("isspace", "00001000000000000000000000000000"),
        ("isascii", "11111110000111111111100111111111"),
        ("islower", "00100000010010000100000000000111"),
        ("isupper", "00000000001001001011000101001000"),
        ("istitle", "00000000001001010011010101001000"),
    ];
    private static readonly (string Name, string Bitmap)[] LiveCases =
    [
        ("isalpha", "11000"),
        ("isalnum", "11100"),
        ("isdigit", "00000"),
        ("isspace", "00000"),
        ("isascii", "11111"),
        ("islower", "10000"),
        ("isupper", "01100"),
        ("istitle", "01101"),
    ];

    private static object Receiver(bool mutable, byte[] bytes) => mutable ? new PyByteArray(bytes) : new PyBytes(bytes);
    private static PyBytes B(string value) => new(Encoding.Latin1.GetBytes(value));
    private static object? Call(object value, string name) => PyOps.CallMethod(value, name, [], [], []);
    private static object? Saved(object method) => PyOps.CallValue(method, [], [], []);
    private static void Routes(object value, string name, bool expected)
    {
        object owner = PyObject.TypeOf(value);
        object?[] results = [Call(value, name), PyPic.CallMethod0(1760, value, name),
            Saved(PyObject.GetAttr(value, name)!),
            PyOps.CallMethod(owner, name, [value], [], []),
            PyOps.CallValue(PyObject.GetAttr(owner, name), [value], [], [])];
        foreach (object? result in results)
        {
            Assert.Equal(expected, Assert.IsType<bool>(result));
            Assert.True(PyOps.Is(result, expected));
            Assert.False(PyOps.Is(result, !expected));
        }
    }

    [Fact]
    public void All256BytesAgreeWithReferenceAcrossBothCarriersAndFiveRuntimeRoutes()
    {
        foreach (bool mutable in new[] { false, true })
            foreach ((string name, string bitmap) in SingleByteCases)
            {
                Assert.Equal(256, bitmap.Length);
                for (int value = 0; value < 256; value++)
                    Routes(Receiver(mutable, [(byte)value]), name, bitmap[value] == '1');
            }
    }

    [Fact]
    public void EmptyMixedAndTitleBoundariesUseReferenceVectors()
    {
        Assert.Equal(32, MixedHex.Length);
        foreach (bool mutable in new[] { false, true })
            foreach ((string name, string bitmap) in MixedCases)
            {
                Assert.Equal(MixedHex.Length, bitmap.Length);
                for (int index = 0; index < MixedHex.Length; index++)
                    Routes(Receiver(mutable, Convert.FromHexString(MixedHex[index])), name, bitmap[index] == '1');
            }
    }

    [Fact]
    public void SavedMethodsReadCurrentBytearrayAfterWritesAndResizesWithoutLeases()
    {
        foreach ((string name, string bitmap) in LiveCases)
        {
            var value = new PyByteArray([97]);
            object method = PyObject.GetAttr(value, name)!;
            Assert.Equal(bitmap[0] == '1', Saved(method));
            value.SetItem(0L, 65L); Assert.Equal(bitmap[1] == '1', Saved(method));
            value.Append(49L); Assert.Equal(bitmap[2] == '1', Saved(method));
            value.DelItem(new PySlice(PyNone.Instance, PyNone.Instance, 1L));
            Assert.Equal(bitmap[3] == '1', Saved(method));
            value.Extend(B("Ab Cd")); Assert.Equal(bitmap[4] == '1', Saved(method));
            Assert.Same(value, PyObject.GetAttr(method, "__self__"));
        }
    }

    [Fact]
    public void ViewAliasWritesRemainVisibleAndClassificationDoesNotOwnAnExport()
    {
        var value = new PyByteArray([65]);
        var view = (PyMemoryView)PyMemoryView.Create([value], [], []);
        object method = PyObject.GetAttr(value, "isalpha")!;
        view.SetItem(0L, 49L);
        Assert.Equal(false, Saved(method)); Assert.Equal(false, Call(value, "isalpha"));
        Assert.Throws<PyBufferError>(() => value.Append(65L));
        view.Release(); value.Append(65L);
        Assert.Equal(false, Saved(method)); Assert.Equal(new byte[] { 49, 65 }, value.Bytes);
    }

    [Fact]
    public void MethodMetadataReportsActualRootAndBoundRuntimeTypes()
    {
        foreach (bool mutable in new[] { false, true })
            foreach ((string name, _) in SingleByteCases)
            {
                object value = Receiver(mutable, [65]); object owner = PyObject.TypeOf(value);
                string ownerName = mutable ? "bytearray" : "bytes";
                object root = PyObject.GetAttr(owner, name)!;
                object bound = PyObject.GetAttr(value, name)!, again = PyObject.GetAttr(value, name)!;
                Assert.Same(root, PyObject.GetAttr(owner, name));
                Assert.Same(owner, PyObject.GetAttr(root, "__objclass__"));
                Assert.Equal("method_descriptor", PyObject.GetAttr(PyObject.TypeOf(root), "__name__"));
                Assert.Equal("builtin_function_or_method", PyObject.GetAttr(PyObject.TypeOf(bound), "__name__"));
                Assert.Equal(name, PyObject.GetAttr(root, "__name__"));
                Assert.Equal(ownerName + "." + name, PyObject.GetAttr(root, "__qualname__"));
                Assert.Equal(name, PyObject.GetAttr(bound, "__name__"));
                Assert.Equal(ownerName + "." + name, PyObject.GetAttr(bound, "__qualname__"));
                Assert.Same(value, PyObject.GetAttr(bound, "__self__")); Assert.NotSame(bound, again);
                Assert.True(PyHash.Equal(bound, again)); Assert.Equal(PyHash.Of(bound), PyHash.Of(again));
            }
    }

    [Fact]
    public void ExistingBinaryMethodsStillUseTheirPriorImplementations()
    {
        object bytes = B("Ab"); object array = new PyByteArray([65, 98]);
        Assert.Equal("4162", Call(bytes, "hex"));
        foreach (object value in new[] { bytes, array })
        {
            Assert.Equal("Ab", Call(value, "decode"));
            Assert.Equal(true, PyOps.CallMethod(value, "startswith", [B("A")], [], []));
            object saved = PyObject.GetAttr(value, "endswith")!;
            Assert.Equal(true, PyOps.CallValue(saved, [B("b")], [], []));
        }
    }
}
