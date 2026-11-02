//! 固定C ABI发射原型：构造真实ImplMap并从同份PE读回验证，不实现Python绑定/自动root。
use dotnetdll::prelude::*;
use std::borrow::Cow;
use std::error::Error;
use std::fmt::Write as _;
use std::io::Write as _;
use std::path::Path;

const ASSEMBLY: &str = "RyupyCabiProbe";
const LIBRARY: &str = "ryupy_cabi_probe";
const SYMBOL: &str = "ryupy_demo_add_i32";
const CASES: [(i32, i32); 5] = [
    (0, 0),
    (-1, 4),
    (i32::MAX, i32::MAX),
    (i32::MIN, i32::MIN),
    (i32::MIN, i32::MAX),
];

/// 框架引用使用真实.NET10 facade身份，不能以匿名0版本冒充可被宿主引用的程序集。
fn framework(name: &'static str) -> ExternalAssemblyReference<'static> {
    let mut reference = ExternalAssemblyReference::new(name);
    reference.version = Version {
        major: 10,
        minor: 0,
        build: 0,
        revision: 0,
    };
    reference.public_key_or_token = Some(Cow::Borrowed(&[
        0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a,
    ]));
    reference
}

fn emit() -> Result<Vec<u8>, Box<dyn Error>> {
    let mut res = Resolution::new(Module::new(format!("{ASSEMBLY}.dll")));
    res.assembly = Some(Assembly::new(ASSEMBLY));
    let system = res.push_assembly_reference(framework("System.Runtime"));
    let console_assembly = res.push_assembly_reference(framework("System.Console"));
    let object = res.push_type_reference(type_ref! { System.Object in #system });
    let console = res.push_type_reference(type_ref! { System.Console in #console_assembly });
    let console_type: MethodType = BaseType::class(console).into();
    let write =
        res.push_method_reference(method_ref! { static void @console_type::WriteLine(long) });
    let class = res.push_type_definition(TypeDefinition::new(
        Some("RyuPy.CabiProbe".into()),
        "Program",
    ));
    res[class].set_extends(object);
    res[class].flags.accessibility = TypeAccessibility::Public;
    res[class].flags.abstract_type = true;
    res[class].flags.sealed = true;

    let scope = res.push_module_reference(ExternalModuleReference::new(LIBRARY));
    let mut import = PInvoke::new(scope, SYMBOL);
    import.no_mangle = true;
    import.calling_convention = UnmanagedCallingConvention::Cdecl;
    import.character_set = CharacterSet::NotSpecified;
    import.supports_last_error = false;
    let mut method = Method::new(
        Accessibility::Public,
        msig! { static long (int, int) },
        "NativeAdd",
        None,
    );
    method.pinvoke = Some(import);
    method.preserve_sig = true;
    let native = res.push_method(class, method);

    let mut instructions = Vec::new();
    for (left, right) in CASES {
        instructions.push(Instruction::LoadConstantInt32(left));
        instructions.push(Instruction::LoadConstantInt32(right));
        instructions.push(Instruction::call(native));
        instructions.push(Instruction::call(write));
    }
    instructions.push(Instruction::LoadConstantInt32(0));
    instructions.push(Instruction::Return);
    let mut body = body::Method::new(instructions);
    body.header.maximum_stack_size = 2;
    let main = res.push_method(
        class,
        Method::new(
            Accessibility::Public,
            msig! { static int (string[]) },
            "Main",
            Some(body),
        ),
    );
    res.set_entry_point(main);
    // AnyCPU IL映像，不把PE位数当作所调用C库架构；工具链按实际host产出匹配库。
    Ok(res.write(WriteOptions {
        is_32_bit: true,
        is_executable: false,
    })?)
}

fn verify(bytes: &[u8]) -> Result<String, Box<dyn Error>> {
    let res = Resolution::parse(bytes, ReadOptions::default())?;
    let (class_id, class) = res
        .enumerate_type_definitions()
        .find(|(_, class)| {
            class.namespace.as_deref() == Some("RyuPy.CabiProbe") && class.name == "Program"
        })
        .ok_or("missing public Program")?;
    assert!(matches!(
        class.flags.accessibility,
        TypeAccessibility::Public
    ));
    let (native_id, native) = res
        .enumerate_methods(class_id)
        .find(|(_, method)| method.name == "NativeAdd")
        .ok_or("missing PInvoke")?;
    assert!(native.is_static() && native.body.is_none() && native.preserve_sig);
    assert_eq!(native.signature, msig! { static long (int, int) });
    let pinvoke = native.pinvoke.as_ref().ok_or("missing ImplMap")?;
    assert_eq!(res[pinvoke.import_scope].name, LIBRARY);
    assert_eq!(pinvoke.import_name, SYMBOL);
    assert!(pinvoke.no_mangle && !pinvoke.supports_last_error);
    assert!(matches!(
        pinvoke.calling_convention,
        UnmanagedCallingConvention::Cdecl
    ));
    assert!(matches!(pinvoke.character_set, CharacterSet::NotSpecified));
    let (main_id, main) = res
        .enumerate_methods(class_id)
        .find(|(_, method)| method.name == "Main")
        .ok_or("missing Main")?;
    assert_eq!(main.signature, msig! { static int (string[]) });
    assert!(matches!(res.entry_point, Some(EntryPoint::Method(method)) if method == main_id));
    let code = &main.body.as_ref().ok_or("missing Main body")?.instructions;
    assert_eq!(code.len(), CASES.len() * 4 + 2);
    let mut report = format!(
        "schema_version=1\nassembly={ASSEMBLY}\nentry=RyuPy.CabiProbe.Program.Main(string[])->int32\nmodule_ref={LIBRARY}\nimport_name={SYMBOL}\nsignature=int32,int32->int64\ncdecl=true\nexact_spelling=true\npreserve_sig=true\nstatic=true\nbody_absent=true\nlast_error=false\n"
    );
    for (index, (left, right)) in CASES.into_iter().enumerate() {
        let offset = index * 4;
        assert!(matches!(code[offset], Instruction::LoadConstantInt32(value) if value == left));
        assert!(
            matches!(code[offset + 1], Instruction::LoadConstantInt32(value) if value == right)
        );
        assert_eq!(code[offset + 2], Instruction::call(native_id));
        assert!(
            code[offset + 3]
                .show(&res)
                .contains("System.Console.WriteLine(long)")
        );
        writeln!(
            report,
            "case.{index}={left},{right}->{}",
            i64::from(left) + i64::from(right)
        )?;
    }
    assert!(matches!(
        code[code.len() - 2],
        Instruction::LoadConstantInt32(0)
    ));
    assert!(matches!(code[code.len() - 1], Instruction::Return));
    for (index, instruction) in code.iter().enumerate() {
        writeln!(report, "main.il.{index:04x}={}", instruction.show(&res))?;
    }
    report.push_str(
        "readback_verified=true\npython_binding=false\nnative_execution_verified=false\n",
    );
    Ok(report)
}

fn write_new(root: &Path, name: &str, bytes: &[u8]) -> Result<(), Box<dyn Error>> {
    let mut file = std::fs::OpenOptions::new()
        .write(true)
        .create_new(true)
        .open(root.join(name))?;
    file.write_all(bytes)?;
    Ok(())
}
fn main() -> Result<(), Box<dyn Error>> {
    let mut args = std::env::args_os().skip(1);
    let out = args
        .next()
        .ok_or("usage: ryupy-cabi-emit-probe EMPTY_OUTPUT_DIRECTORY")?;
    if args.next().is_some() {
        return Err("unexpected extra argument".into());
    }
    let out = Path::new(&out);
    if !out.is_dir() || std::fs::read_dir(out)?.next().is_some() {
        return Err("output must be an existing empty owned directory".into());
    }
    let bytes = emit()?;
    let report = verify(&bytes)?;
    write_new(out, "RyupyCabiProbe.dll", &bytes)?;
    write_new(out, "RyupyCabiProbe.runtimeconfig.json", br#"{"runtimeOptions":{"tfm":"net10.0","framework":{"name":"Microsoft.NETCore.App","version":"10.0.0"}}}"#)?;
    // 最后才写readback完成标志，失败不伪报成功；目录清理由外层自有harness负责。
    write_new(out, "metadata.txt", report.as_bytes())?;
    println!("emitted-and-readback-verified");
    Ok(())
}

#[cfg(test)]
mod tests {
    #[test]
    fn actual_pe_roundtrip_preserves_explicit_c_abi_and_main_calls() {
        let bytes = super::emit().unwrap();
        let report = super::verify(&bytes).unwrap();
        assert!(report.contains("case.2=2147483647,2147483647->4294967294"));
        assert!(report.contains("case.3=-2147483648,-2147483648->-4294967296"));
        assert!(report.contains("readback_verified=true"));
    }
}
