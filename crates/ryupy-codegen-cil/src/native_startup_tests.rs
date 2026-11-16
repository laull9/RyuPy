//! 真实PE入口的有限启动需求；只改变初始化目标，参数、退出与EH必须逐指令保留。
use dotnetdll::prelude::*;

#[test]
fn frozen_inspect_need_only_changes_the_actual_initializer_target() {
    let mut bodies = Vec::new();
    for inspect in [true, false] {
        let mut program = crate::exact_i64_tests::program();
        assert!(program.native_modules.inspect, "手工MIR默认保原完整启动");
        program.native_modules.inspect = inspect;
        let bytes = crate::emit_assembly(
            &program,
            &crate::EmitOptions {
                assembly_name: "NativeStartupPe".into(),
                entry_source_path: None,
            },
        )
        .unwrap();
        let resolution = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let method = resolution
            .type_definitions
            .iter()
            .flat_map(|ty| &ty.methods)
            .find(|method| method.name == "Main")
            .unwrap();
        let body = method.body.as_ref().unwrap();
        assert_eq!(body.instructions[3], Instruction::LoadArgument(0));
        let Instruction::Call {
            param0: MethodSource::User(UserMethod::Reference(reference)),
            ..
        } = body.instructions[4]
        else {
            panic!("真实Main必须静态调用初始化器");
        };
        assert_eq!(
            resolution[reference].name,
            if inspect {
                "Initialize"
            } else {
                "InitializeCompiledBase"
            }
        );
        assert_eq!(
            resolution[reference].signature,
            msig! { static void(string, string[]) }
        );
        let mut instructions = body.instructions.clone();
        instructions.remove(4); // 比较其余序列化指令；产品只替换同签名调用的目标。
        bodies.push((
            instructions,
            format!("{:?}", body.data_sections),
            format!("{:?}", body.header),
        ));
    }
    assert_eq!(
        bodies[0], bodies[1],
        "两种启动不能移动参数、异常表或退出协议"
    );
}
