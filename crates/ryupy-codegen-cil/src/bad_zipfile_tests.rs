//! 序列化PE同时锁真实catch类型和生成用户异常的CLR父类/三参构造，不只匹配Python名字。
use dotnetdll::prelude::*;
use ryupy_mir::{BasicBlock, Region, RegionKind, Rvalue, Statement, Terminator};
#[test]
fn bad_zipfile_is_real_clr_catch_and_generated_subclass_base() {
    let mut program = crate::exact_i64_tests::program();
    program.classes.alloc(ryupy_mir::ClassDef {
        class_cell: None,
        name: "ArchiveError".into(),
        codegen_name: "ArchiveError".into(),
        fields: vec![],
        is_value_type: false,
        is_dataclass: false,
        base: None,
        builtin_base: Some("zipfile.BadZipFile".into()),
        is_exception: true,
        factory_global: None,
    });
    let init = program.modules[program.entry].init;
    let body = &mut program.functions[init].body;
    let caught = crate::exact_i64_tests::local(&mut body.locals, ryupy_mir::MirType::Dynamic);
    let protected = body.entry;
    let handler = body.blocks.alloc(BasicBlock {
        stmts: vec![Statement::Assign {
            dest: caught,
            value: Rvalue::CatchException,
        }],
        terminator: Terminator::Unreachable,
    });
    let after = body.blocks.alloc(BasicBlock {
        stmts: vec![],
        terminator: Terminator::Return(None),
    });
    body.blocks[protected].terminator = Terminator::Goto(after);
    body.blocks[handler].terminator = Terminator::Goto(after);
    body.regions.push(Region {
        source_handler_chain: None,
        kind: RegionKind::Catch {
            type_names: vec!["zipfile.BadZipFile".into()],
        },
        try_start: protected,
        try_len: 1,
        handler,
        exc_local: Some(caught),
    });
    let bytes = crate::emit_assembly(
        &program,
        &crate::EmitOptions {
            assembly_name: "zip_exception".into(),
            entry_source_path: None,
        },
    )
    .unwrap();
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let reference = res
        .type_references
        .iter()
        .find(|t| t.name == "PyBadZipFile")
        .unwrap();
    assert_eq!(reference.namespace.as_deref(), Some("RyuPy.Runtime"));
    let ResolutionScope::Assembly(assembly) = reference.scope else {
        panic!("真实runtime程序集类型引用")
    };
    assert_eq!(res[assembly].name, "RyuPy.Runtime");
    let expected = "[RyuPy.Runtime]RyuPy.Runtime.PyBadZipFile";
    let init_method = res
        .type_definitions
        .iter()
        .flat_map(|t| &t.methods)
        .find(|m| m.name == "__main__")
        .unwrap();
    let clauses: Vec<_> = init_method
        .body
        .as_ref()
        .unwrap()
        .data_sections
        .iter()
        .filter_map(|s| match s {
            body::DataSection::ExceptionHandlers(clauses) => Some(clauses),
            _ => None,
        })
        .flatten()
        .collect();
    assert!(clauses.iter().any(|clause| match &clause.kind {
        body::ExceptionKind::TypedException(ty) => ty.show(&res).contains(expected),
        _ => false,
    }));
    let child = res
        .type_definitions
        .iter()
        .find(|t| t.name == "_ArchiveError")
        .unwrap();
    assert!(
        child
            .extends
            .as_ref()
            .unwrap()
            .show(&res)
            .contains("PyBadZipFile")
    );
    let constructor = child.methods.iter().find(|m| m.name == ".ctor").unwrap();
    let code = &constructor.body.as_ref().unwrap().instructions;
    assert!(code[2].show(&res).contains("zipfile.BadZipFile"));
    assert!(code[4].show(&res).contains("UserExceptionMessageForBase"));
    assert!(matches!(code[5], Instruction::LoadArgument(2)));
    // 直接核PE中的调用目标与完整实例签名，不依赖显示器的成员分隔符。
    let Instruction::Call {
        param0: MethodSource::User(UserMethod::Reference(base_ctor)),
        ..
    } = &code[6]
    else {
        panic!(
            "必须直接调用真实runtime基类构造函数: {}",
            code[6].show(&res)
        )
    };
    let base_ctor = &res[*base_ctor];
    assert_eq!(base_ctor.name, ".ctor");
    let MethodReferenceParent::Type(parent) = &base_ctor.parent else {
        panic!("构造函数必须属于真实runtime类型")
    };
    assert_eq!(parent.show(&res), expected);
    assert_eq!(
        base_ctor.signature,
        msig! { void (string, string, object[]) }
    );
}

#[test]
fn syntax_and_xml_parse_error_emit_real_typed_catch_clauses() {
    for (python, clr) in [
        ("SyntaxError", "PySyntaxError"),
        ("xml.etree.ElementTree.ParseError", "PyXmlParseError"),
    ] {
        let mut program = crate::exact_i64_tests::program();
        let init = program.modules[program.entry].init;
        let body = &mut program.functions[init].body;
        let caught = crate::exact_i64_tests::local(&mut body.locals, ryupy_mir::MirType::Dynamic);
        let protected = body.entry;
        let handler = body.blocks.alloc(BasicBlock {
            stmts: vec![Statement::Assign {
                dest: caught,
                value: Rvalue::CatchException,
            }],
            terminator: Terminator::Unreachable,
        });
        let after = body.blocks.alloc(BasicBlock {
            stmts: vec![],
            terminator: Terminator::Return(None),
        });
        body.blocks[protected].terminator = Terminator::Goto(after);
        body.blocks[handler].terminator = Terminator::Goto(after);
        body.regions.push(Region {
            source_handler_chain: None,
            kind: RegionKind::Catch {
                type_names: vec![python.into()],
            },
            try_start: protected,
            try_len: 1,
            handler,
            exc_local: Some(caught),
        });
        let bytes = crate::emit_assembly(
            &program,
            &crate::EmitOptions {
                assembly_name: "xml_exception".into(),
                entry_source_path: None,
            },
        )
        .unwrap();
        let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
        let expected = format!("[RyuPy.Runtime]RyuPy.Runtime.{clr}");
        let method = res
            .type_definitions
            .iter()
            .flat_map(|t| &t.methods)
            .find(|m| m.name == "__main__")
            .unwrap();
        let clauses: Vec<_> = method
            .body
            .as_ref()
            .unwrap()
            .data_sections
            .iter()
            .filter_map(|s| match s {
                body::DataSection::ExceptionHandlers(clauses) => Some(clauses),
                _ => None,
            })
            .flatten()
            .collect();
        assert!(clauses.iter().any(|c| matches!(&c.kind, body::ExceptionKind::TypedException(ty) if ty.show(&res) == expected)), "真实EH不是仅有TypeRef");
        assert!(
            method
                .body
                .as_ref()
                .unwrap()
                .instructions
                .iter()
                .any(|i| matches!(i, Instruction::StoreLocal(_))),
            "catch异常必须实际落槽"
        );
        assert!(
            !res.type_definitions
                .iter()
                .any(|t| t.name.starts_with("_Child")),
            "新族未开放用户CLR派生"
        );
    }
}
