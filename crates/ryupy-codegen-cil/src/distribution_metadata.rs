//! 分发载荷静态注册：仅传递driver数据与真实生成类型token，不解析内容或查路径。
use dotnetdll::prelude::*;
pub(crate) fn registration(
    res: &mut Resolution<'static>,
    owner: TypeIndex,
    system: AssemblyRefIndex,
    runtime: AssemblyRefIndex,
    payload: &str,
    unavailable: bool,
) -> Vec<Instruction> {
    let ty = res.push_type_reference(type_ref! { System.Type in #system });
    let ty: MethodType = BaseType::class(ty).into();
    let handle = res.push_type_reference(type_ref! { System.RuntimeTypeHandle in #system });
    let handle: MethodType = BaseType::valuetype(handle).into();
    let snapshot =
        res.push_type_reference(type_ref! { RyuPy.Runtime.PyDistributionSnapshot in #runtime });
    let snapshot: MethodType = BaseType::class(snapshot).into();
    let get_type =
        res.push_method_reference(method_ref! { static @ty @ty::GetTypeFromHandle(@handle) });
    let configure = if unavailable {
        res.push_method_reference(
            method_ref! { static void @snapshot::ConfigureAutoUnavailable(@ty, string) },
        )
    } else {
        res.push_method_reference(method_ref! { static void @snapshot::Configure(@ty, string) })
    };
    vec![
        Instruction::LoadTokenType(BaseType::class(owner).into()),
        Instruction::call(get_type),
        Instruction::load_string(payload),
        Instruction::call(configure),
    ]
}

/// #US token为24bit偏移。按所有ldstr逐个计入（不依赖去重），超限明确拒绝。
/// 单载荷最多4MiB UTF-8/8MiB UTF-16；新增方法体恒定20字节，分块不减少heap。
pub(crate) fn validate_heap(
    program: &ryupy_mir::LinkedProgram,
    res: &Resolution<'_>,
) -> Result<(), crate::CodegenError> {
    let payloads = [
        &program.distribution_metadata,
        &program.metadata_unavailable,
        &program.optional_sources,
    ];
    if payloads.iter().all(|payload| payload.is_none()) {
        return Ok(());
    }
    if payloads
        .iter()
        .filter_map(|payload| payload.as_ref())
        .any(|payload| payload.len() > 4 * 1024 * 1024)
        || program.trace_product.is_some()
    {
        return Err(crate::CodegenError::Unsupported(
            "冷来源载荷超过4MiB或与trace V1冲突".into(),
        ));
    }
    let mut size = 1usize;
    for method in res.type_definitions.iter().flat_map(|ty| &ty.methods) {
        if let Some(body) = &method.body {
            for instruction in &body.instructions {
                if let Instruction::LoadString(text) = instruction {
                    size = size.saturating_add(text.len().saturating_mul(2).saturating_add(5));
                }
            }
        }
    }
    if size > 0x00ff_ffff {
        return Err(crate::CodegenError::Unsupported(
            "分发产品的完整#US预算超过24bit".into(),
        ));
    }
    Ok(())
}

#[cfg(test)]
mod tests {
    use super::*;
    #[test]
    fn distribution_metadata_uses_real_owner_and_preserves_main() {
        let mut program = crate::exact_i64_tests::program();
        let options = crate::EmitOptions {
            assembly_name: "metadata_owner".into(),
            entry_source_path: None,
        };
        let baseline = crate::emit_assembly(&program, &options).unwrap();
        // 大于64KiB的常量不增加方法指令体；不用反射动态构建或旁边文件。
        let payload = "x".repeat(70_000);
        program.distribution_metadata = Some(payload.clone());
        let bytes = crate::emit_assembly(&program, &options).unwrap();
        let parsed = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let before = Resolution::parse(&baseline, ReadOptions::default()).unwrap();
        let methods: Vec<_> = parsed
            .type_definitions
            .iter()
            .flat_map(|ty| &ty.methods)
            .collect();
        let startup = methods
            .iter()
            .find(|method| method.name == "__init_functions")
            .unwrap()
            .body
            .as_ref()
            .unwrap();
        let instructions = &startup.instructions;
        let Instruction::LoadTokenType(owner) = &instructions[0] else {
            panic!("owner token required")
        };
        assert!(owner.show(&parsed).contains("RyuPy.Generated.Module"));
        assert!(
            instructions[1]
                .show(&parsed)
                .contains("System.Type.GetTypeFromHandle")
        );
        let Instruction::LoadString(actual) = &instructions[2] else {
            panic!("static payload required")
        };
        assert_eq!(actual.len(), payload.len());
        assert!(
            instructions[3]
                .show(&parsed)
                .contains("PyDistributionSnapshot.Configure")
        );
        let main = methods
            .iter()
            .find(|method| method.name == "Main")
            .unwrap()
            .body
            .as_ref()
            .unwrap();
        let baseline_main = before
            .type_definitions
            .iter()
            .flat_map(|ty| &ty.methods)
            .find(|method| method.name == "Main")
            .unwrap()
            .body
            .as_ref()
            .unwrap();
        assert_eq!(main.instructions.len(), baseline_main.instructions.len());
        let body::DataSection::ExceptionHandlers(actual_handlers) = &main.data_sections[0] else {
            panic!("EH required")
        };
        let body::DataSection::ExceptionHandlers(expected_handlers) =
            &baseline_main.data_sections[0]
        else {
            panic!("EH required")
        };
        assert_eq!(actual_handlers.len(), expected_handlers.len());
        for (actual, expected) in actual_handlers.iter().zip(expected_handlers) {
            assert_eq!(
                (
                    actual.try_offset,
                    actual.try_length,
                    actual.handler_offset,
                    actual.handler_length
                ),
                (
                    expected.try_offset,
                    expected.try_length,
                    expected.handler_offset,
                    expected.handler_length
                )
            );
            let (
                body::ExceptionKind::TypedException(actual_type),
                body::ExceptionKind::TypedException(expected_type),
            ) = (&actual.kind, &expected.kind)
            else {
                panic!("typed EH required")
            };
            assert_eq!(actual_type.show(&parsed), expected_type.show(&before));
        }
        assert_eq!(
            main.instructions
                .iter()
                .map(|i| i.show(&parsed))
                .collect::<Vec<_>>(),
            baseline_main
                .instructions
                .iter()
                .map(|i| i.show(&before))
                .collect::<Vec<_>>()
        );
    }
}
