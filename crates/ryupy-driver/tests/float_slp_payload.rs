//! 比较真实编译packed与原scalar函数的逐bit结果，覆盖不同quiet-NaN payload而非nan==nan。
//! 小C#宿主仅供测试注入位模式/读取结果；被测算术仍全部来自Python→MIR→CIL。
use ryupy_driver::{BuildOptions, Session, Stage};
mod support;

#[test]
fn packed_lanes_preserve_scalar_payload_zero_and_operand_order() {
    let root = ryupy_driver::runtime::workspace_root();
    let dir = root.join("target/test_float_slp_payload");
    std::fs::create_dir_all(&dir).unwrap();
    let input = dir.join("payload.py");
    std::fs::write(&input, "def pair(a,b,c,d,e,f):\n    return [a*b+c,d*e+f]\ndef scalar(a,b,c):\n    return a*b+c\ndef pair_sub(a,b,c,d,e,f):\n    return [(a-b)*c,(d-e)*f]\ndef scalar_sub(a,b,c):\n    return (a-b)*c\n").unwrap();
    let mut session = Session {
        experimental_packed_slp: true,
        ..Session::default()
    };
    let mir = support::dump(&mut session, &input, Stage::Mir).unwrap();
    assert!(mir.contains("vector_Mul_f64") && mir.contains("vector_Sub_f64"));
    let symbols = core_symbols(&input);
    let artifact = ryupy_driver::build(
        &mut session,
        &BuildOptions {
            input,
            out_dir: dir.join("generated"),
            aot: false,
            search_paths: vec![],
        },
    )
    .unwrap();
    let host = dir.join("host");
    std::fs::create_dir_all(&host).unwrap();
    let runtime = artifact.dll.parent().unwrap().join("RyuPy.Runtime.dll");
    std::fs::write(host.join("payload_host.csproj"), format!(
        "<Project Sdk=\"Microsoft.NET.Sdk\"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>enable</ImplicitUsings><Nullable>enable</Nullable></PropertyGroup><ItemGroup><Reference Include=\"RyuPy.Runtime\"><HintPath>{}</HintPath></Reference></ItemGroup></Project>", runtime.display()
    )).unwrap();
    let mapping = symbols
        .iter()
        .map(|(name, symbol)| format!("{{\"{name}\", \"{symbol}\"}}"))
        .collect::<Vec<_>>()
        .join(",");
    std::fs::write(
        host.join("Program.cs"),
        HOST.replace("/*CORE_NAMES*/", &mapping),
    )
    .unwrap();
    let built = std::process::Command::new("dotnet")
        .args(["build", "-c", "Release", "-v", "q", "--nologo"])
        .arg(host.join("payload_host.csproj"))
        .output()
        .unwrap();
    assert!(
        built.status.success(),
        "{}{}",
        String::from_utf8_lossy(&built.stdout),
        String::from_utf8_lossy(&built.stderr)
    );
    for hardware_off in [false, true] {
        let mut run = std::process::Command::new("dotnet");
        run.arg(host.join("bin/Release/net10.0/payload_host.dll"))
            .arg(&artifact.dll);
        if hardware_off {
            run.arg("disabled")
                .env("DOTNET_EnableHWIntrinsic", "0")
                .env("COMPlus_EnableHWIntrinsic", "0");
        }
        let result = run.output().unwrap();
        assert!(
            result.status.success(),
            "{}{}",
            String::from_utf8_lossy(&result.stdout),
            String::from_utf8_lossy(&result.stderr)
        );
        assert_eq!(String::from_utf8_lossy(&result.stdout).trim(), "位模式通过");
    }
}

const HOST: &str = r#"
using System.Reflection;
using System.Runtime.Intrinsics;
using RyuPy.Runtime;
var assembly = Assembly.LoadFrom(args[0]);
var entry = assembly.EntryPoint!;
object? code = entry.Invoke(null, entry.GetParameters().Length == 0 ? null : new object?[] { Array.Empty<string>() });
if (code is int n && n != 0) throw new Exception("入口失败");
if (args.Length > 1 && Vector128.IsHardwareAccelerated) throw new Exception("未关闭硬件intrinsic");
var type = assembly.GetType("RyuPy.Generated.Module")!;
const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
var module = PyModules.Import("__main__");
var coreNames = new Dictionary<string,string> { /*CORE_NAMES*/ };
(MethodInfo Core, PyFunction Owner) Function(string name, int arity) {
    // 入口已执行；从真实模块公开绑定取owner，绝不new假函数或传null占位。
    var owner = module.GetAttr(name) as PyFunction ?? throw new Exception($"未发布函数{name}");
    var core = type.GetMethod(coreNames[name], flags) ?? throw new Exception($"缺少core{name}");
    if (!core.IsStatic || core.ReturnType != typeof(object)
        || core.GetParameters().Length != arity + 1
        || core.GetParameters().Any(parameter => parameter.ParameterType != typeof(object))
        || owner.Name != name || owner.Signature.Names.Length != arity || owner.Env is not null
        || owner.Body.Method.DeclaringType != type)
        throw new Exception($"Dynamic core/已发布owner ABI不匹配{name}");
    return (core, owner);
}
long Bits(object? value) => BitConverter.DoubleToInt64Bits((double)value!);
ulong[] patterns = [0UL, 0x8000000000000000UL, 0x3ff0000000000000UL, 0xbff0000000000000UL,
    0x7ff0000000000000UL, 0xfff0000000000000UL, 0x7ff8000000000011UL, 0xfff8000000000022UL,
    1UL, 0x7fefffffffffffffUL];
foreach (string suffix in new[] { "", "_sub" }) {
    var (pair, pairOwner) = Function("pair" + suffix, 6);
    var (scalar, scalarOwner) = Function("scalar" + suffix, 3);
    for (int i = 0; i < patterns.Length; i++)
    for (int j = 0; j < patterns.Length; j++)
    for (int k = 0; k < patterns.Length; k++) {
        object a = BitConverter.Int64BitsToDouble(unchecked((long)patterns[i]));
        object b = BitConverter.Int64BitsToDouble(unchecked((long)patterns[j]));
        object c = BitConverter.Int64BitsToDouble(unchecked((long)patterns[k]));
        var result = (PyList)pair.Invoke(null, [a,b,c,c,b,a,pairOwner])!;
        long left = Bits(scalar.Invoke(null, [a,b,c,scalarOwner]));
        long right = Bits(scalar.Invoke(null, [c,b,a,scalarOwner]));
        if (Bits(result.GetItem(0)) != left || Bits(result.GetItem(1)) != right)
            throw new Exception($"位模式改变{suffix}/{i}/{j}/{k}: {Bits(result.GetItem(0)):x16}/{left:x16}");
    }
}
Console.WriteLine("位模式通过");
"#;

// 与当前源同一次前中端规则取得链接符号，禁止以Python可见名猜反射core。
fn core_symbols(input: &std::path::Path) -> Vec<(String, String)> {
    let source = std::fs::read_to_string(input).unwrap();
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("payload.py", &source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, &source);
    assert!(diagnostics.is_empty());
    let info = ryupy_hir::ModuleInfo {
        name: "__main__".into(),
        file: "payload.py".into(),
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, &source);
    assert!(diagnostics.is_empty());
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty());
    let (program, exports, diagnostics) = ryupy_mir::build_program(&hir, &types);
    assert!(diagnostics.is_empty());
    let linked = ryupy_mir::link(
        vec![(
            program,
            ryupy_mir::UnitMeta {
                instance_capabilities: Default::default(),
                execution: ryupy_mir::ModuleExecutionKind::LegacySingleton,
                environment_reason: "test".into(),
                location: ryupy_mir::ModuleLocation::LegacyDiagnostic,
                file_backed_by_global: true,
                is_package: false,
                protected_exports: vec![],
                name: "__main__".into(),
                package: String::new(),
                file: "payload.py".into(),
                exports,
            },
        )],
        0,
    );
    ["pair", "scalar", "pair_sub", "scalar_sub"]
        .iter()
        .map(|name| {
            let found: Vec<_> = linked
                .functions
                .iter()
                .filter(|(_, f)| f.name == *name)
                .collect();
            assert_eq!(found.len(), 1);
            ((*name).to_owned(), found[0].1.codegen_name.clone())
        })
        .collect()
}
