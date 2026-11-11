//! 工厂竖切来源正反例：模板不持singleton，owner/namespace/cell发布身份不能替换。
use crate::*;
const SIMPLE: &str = "def make(x):\n    class C:\n        value = x\n        def read(self, initial=x): return initial, x, __class__\n    return C\na = make(1)\nb = make(2)\nprint(a is not b)\n";

fn compile(source: &str, instance: bool) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("lexical.py", source);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, source);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (hir, diagnostics) = ryupy_hir::lower_module(
        &parsed,
        &ryupy_hir::ModuleInfo {
            instance_globals: instance,
            ..Default::default()
        },
        source,
    );
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    validate::validate(&program).unwrap();
    program
}
fn plan(program: &Program) -> (ClassDefId, ClassCellDefinition) {
    program
        .classes
        .iter()
        .find_map(|(id, class)| {
            class
                .class_cell
                .as_ref()
                .filter(|cell| cell.lexical.is_some())
                .cloned()
                .map(|cell| (id, cell))
        })
        .unwrap()
}
#[test]
fn lexical_class_pipeline_keeps_actual_owner_fresh_cells_and_dynamic_values() {
    for instance in [false, true] {
        for source in [
            SIMPLE,
            "def make():\n    class Outer:\n        class Inner:\n            value = __class__\n    return Outer\n",
            "def make(x):\n    class C:\n        if x:\n            def read(self): return __class__\n        else:\n            def read(self): return x, __class__\n    return C\n",
            "def make():\n    global C\n    class C:\n        def read(self): return __class__\n    return C\n",
            "def enclosing():\n    C = None\n    def make():\n        nonlocal C\n        class C:\n            def read(self): return __class__\n        return C\n    return make\n",
        ] {
            let mut program = compile(source, instance);
            let (_, cell) = plan(&program);
            assert_ne!(cell.owner, program.entry);
            let dump = pretty::dump(&program);
            assert!(dump.contains("LexicalClassPrepare"));
            assert!(dump.contains("LexicalClassFinish"));
            passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
            validate::validate(&program).unwrap();
        }
    }
}
#[test]
fn lexical_class_rejects_namespace_substitution_and_wrong_owner() {
    for owner in [false, true] {
        let mut program = compile(SIMPLE, false);
        let (class, cell) = plan(&program);
        let plan = program.classes[class].class_cell.as_mut().unwrap();
        if owner {
            plan.owner = program.entry;
        } else {
            plan.lexical.as_mut().unwrap().namespace = cell.original_class;
        }
        assert!(validate::validate(&program).is_err());
    }
}
#[test]
fn lexical_class_rejects_publication_and_direct_cell_alias_forgery() {
    let mut program = compile(SIMPLE, false);
    let (class, cell) = plan(&program);
    program.classes[class]
        .class_cell
        .as_mut()
        .unwrap()
        .lexical
        .as_mut()
        .unwrap()
        .publication = LexicalClassPublication::Local(cell.original_class);
    assert!(validate::validate(&program).is_err());
    let mut program = compile(
        "def make():\n    class Outer:\n        class Inner:\n            value = __class__\n    return Outer\n",
        false,
    );
    let (_, cell) = plan(&program);
    for (_, block) in program.functions[cell.owner].body.blocks.iter_mut() {
        for statement in &mut block.stmts {
            if let Statement::Assign { value, .. } = statement
                && crate::lexical_cell_read::is_direct_read(value, &cell)
                && let Rvalue::Call { args, .. } = value
            {
                args[1] = Operand::Const(Constant::I64(1));
            }
        }
    }
    assert!(validate::validate(&program).is_err());
}

#[test]
fn dead_class_publication_accounts_for_eh_and_hidden_operand_reads() {
    use crate::passes::MirPass;
    let source = "def make():\n    try:\n        class C:\n            def read(self): return __class__\n    except ValueError:\n        pass\n";
    let mut program = compile(source, false);
    let (_, cell) = plan(&program);
    let LexicalClassPublication::Local(target) = cell.lexical.as_ref().unwrap().publication else {
        panic!("预期普通local发布")
    };
    let owner = &mut program.functions[cell.owner];
    assert!(crate::lexical_class_cells::dead_local_publication(
        owner, &cell
    ));
    crate::passes::dead_values::DeadValues.run(owner);
    assert!(!owner.body.blocks.iter().any(|(_, block)| {
        block
            .stmts
            .iter()
            .any(|stmt| crate::lexical_class_cells::is_publication(stmt, &cell))
    }));
    validate::validate(&program).unwrap();
    let owner = &mut program.functions[cell.owner];
    owner.body.regions[0].exc_local = Some(target);
    assert!(!crate::lexical_class_cells::dead_local_publication(
        owner, &cell
    ));
    let errors = validate::validate(&program).unwrap_err();
    assert!(
        errors
            .iter()
            .any(|error| error.contains("唯一实际类绑定发布")),
        "{errors:?}"
    );
    for kind in 0..4 {
        let mut program = compile(source, false);
        let (_, cell) = plan(&program);
        let LexicalClassPublication::Local(target) = cell.lexical.as_ref().unwrap().publication
        else {
            unreachable!()
        };
        let owner = &mut program.functions[cell.owner];
        let block = &mut owner.body.blocks[owner.body.entry];
        match kind {
            0 => block.terminator = Terminator::Return(Some(Operand::Local(target))),
            1 => {
                block.terminator = Terminator::Raise {
                    value: Some(Operand::Local(target)),
                    cause: None,
                }
            }
            _ => block.stmts.push(Statement::Eval(Rvalue::InstanceZeroCall {
                callee: Operand::Const(Constant::Null),
                frame: ZeroCallFrame {
                    first_parameter: (kind == 2).then_some(target),
                    class: if kind == 3 {
                        SuperClassSource::Lexical {
                            environment: target,
                            index: 0,
                        }
                    } else {
                        SuperClassSource::Missing
                    },
                },
            })),
        }
        assert!(!crate::lexical_class_cells::dead_local_publication(
            owner, &cell
        ));
    }
}

#[test]
fn namespace_unsupported_builtin_fallback_survives_pipeline_without_opening_api() {
    for source in [
        "def make():\n    class C:\n        eval = 1\n        copied = eval\n    return C.copied\n",
        "def make(flag):\n    class C:\n        if flag: eval = 1\n        copied = eval\n    return C.copied\n",
        "eval = 7\ndef make(flag):\n    class C:\n        if flag: eval = 1\n        copied = eval\n    return C.copied\n",
    ] {
        let mut program = compile(source, false);
        passes::run_pipeline(&mut program, &passes::default_pipeline()).unwrap();
        validate::validate(&program).unwrap();
    }
}
