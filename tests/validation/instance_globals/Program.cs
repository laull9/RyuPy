// 从实际PE入口token 0600000d/签名0001081d0e绑定：public static int Main(string[])。
extern alias Product;
using System;

static class Program
{
    static int Main(string[] args)
    {
        if (args.Length != 2) throw new InvalidOperationException("mode metadata-prefix");
        int result = Product::RyuPy.Generated.Module.Main(Array.Empty<string>());
        if (result != 0) return result;
        InstanceGlobalsHost.Run(args[0], args[1]);
        return 0;
    }
}
