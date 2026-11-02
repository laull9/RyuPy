//! 读回真实 PE 的完整 AssemblyRef，锁宿主编译/AOT身份及向量引用按需建立。
//! 文本 CIL 转储不打印版本与 token，不能用快照代替此元数据断言。
use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{MirType, Operand, Rvalue, Statement};

use super::exact_i64_tests::{function, local, program, single_block};

fn assert_identity(res: &Resolution<'_>, name: &str, major: u16, token: Option<&[u8]>) {
    let references: Vec<_> = res
        .assembly_references
        .iter()
        .filter(|r| r.name == name)
        .collect();
    assert_eq!(references.len(), 1, "{name}");
    let reference = references[0];
    assert_eq!(
        (
            reference.version.major,
            reference.version.minor,
            reference.version.build,
            reference.version.revision
        ),
        (major, 0, 0, 0)
    );
    assert_eq!(reference.public_key_or_token.as_deref(), token);
    assert!(!reference.has_full_public_key);
    assert!(
        reference
            .culture
            .as_ref()
            .is_none_or(|culture| culture.is_empty())
    );
}

#[test]
fn serialized_assembly_references_have_target_identities_and_lazy_intrinsics() {
    for vector in [false, true] {
        let mut program = program();
        if vector {
            let mut locals = Arena::default();
            let supported = local(&mut locals, MirType::Bool);
            program.functions.alloc(function(
                "vector_supported",
                single_block(
                    locals,
                    vec![Statement::Assign {
                        dest: supported,
                        value: Rvalue::IsVector128Supported,
                    }],
                    Some(Operand::Local(supported)),
                ),
                Vec::new(),
                MirType::Bool,
            ));
        }
        let bytes = crate::emit_assembly(
            &program,
            &crate::EmitOptions {
                assembly_name: "assembly_identity".into(),
                entry_source_path: None,
            },
        )
        .unwrap();
        let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let major = crate::runtimeconfig::TARGET_FRAMEWORK_MAJOR as u16;
        assert_identity(
            &res,
            "System.Runtime",
            major,
            Some(&[0xb0, 0x3f, 0x5f, 0x7f, 0x11, 0xd5, 0x0a, 0x3a]),
        );
        assert_identity(&res, "RyuPy.Runtime", 1, None);
        if vector {
            assert_identity(
                &res,
                "System.Runtime.Intrinsics",
                major,
                Some(&[0xcc, 0x7b, 0x13, 0xff, 0xcd, 0x2d, 0xdd, 0x51]),
            );
        } else {
            assert!(
                !res.assembly_references
                    .iter()
                    .any(|r| r.name == "System.Runtime.Intrinsics")
            );
        }
    }
}
