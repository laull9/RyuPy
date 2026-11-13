//! Enum/Flag创建与发布精确结构，替代三项生成IR golden；不将新编译产物加入Git。
use ryupy_mir::{Callee, Constant, Operand, RuntimeFn, Rvalue, Statement};
mod support;

#[test]
fn ordered_declarations_raw_body_and_publication_survive_enum_lowering_and_cil() {
    for (index, source, members, auto_count) in [
        (
            0,
            "from enum import Enum\nclass Color(Enum):\n    RED = 1\n    ALIAS = RED\n    BLUE = 2\nprint(Color(1) is Color.RED)\n",
            vec!["RED", "ALIAS", "BLUE"],
            0,
        ),
        (
            1,
            "from enum import Flag\nclass Bits(Flag):\n    A = 1\n    B = 2\n    BOTH = A | B\n",
            vec!["A", "B", "BOTH"],
            0,
        ),
        (
            2,
            "from enum import Enum, auto\nclass Values(Enum):\n    A = auto()\n    B = A\n    C = auto()\n",
            vec!["A", "B", "C"],
            2,
        ),
    ] {
        let mut db = ryupy_base::SourceDb::default();
        let (parsed, errors) = ryupy_syntax::parse(db.add("enum_structure.py", source), source);
        assert!(errors.is_empty());
        let (hir, errors) = ryupy_hir::lower_module_with_native_exceptions(
            &parsed,
            &Default::default(),
            source,
            &["enum"],
        );
        assert!(errors.is_empty(), "{errors:?}");
        let (types, errors) = ryupy_ty::check_module(&hir);
        assert!(errors.is_empty(), "{errors:?}");
        let (program, _, errors) = ryupy_mir::build_program(&hir, &types);
        assert!(errors.is_empty(), "{errors:?}");
        let statements: Vec<_> = program.functions[program.entry]
            .body
            .blocks
            .iter()
            .flat_map(|(_, b)| &b.stmts)
            .collect();
        let calls = |wanted| -> Vec<_> {
            statements
                .iter()
                .enumerate()
                .filter_map(|(i, s)| {
                    let v = match s {
                        Statement::Assign { value, .. } | Statement::Eval(value) => value,
                        _ => return None,
                    };
                    match v {
                        Rvalue::Call {
                            callee: Callee::Runtime(f),
                            args,
                        } if *f == wanted => Some((i, args)),
                        _ => None,
                    }
                })
                .collect()
        };
        let creates = calls(RuntimeFn::EnumCreate);
        let finishes = calls(RuntimeFn::EnumFinalize);
        assert_eq!((creates.len(), finishes.len()), (1, 1));
        let (begin, args) = creates[0];
        let (end, finish_args) = finishes[0];
        let expected = Operand::Array(
            members
                .iter()
                .map(|n| Operand::Const(Constant::Str((*n).into())))
                .collect(),
        );
        assert_eq!(args.len(), 3);
        assert_eq!(args[2], expected);
        assert_eq!(finish_args[1], expected);
        let writes: Vec<_> = calls(RuntimeFn::SetAttr).into_iter().filter(|(_,a)| matches!(&a[1], Operand::Const(Constant::Str(n)) if members.contains(&n.as_str()))).collect();
        assert_eq!(writes.len(), members.len());
        for ((position, args), name) in writes.iter().zip(&members) {
            assert!(begin < *position && *position < end);
            assert_eq!(args[1], Operand::Const(Constant::Str((*name).into())));
        }
        let body_calls: Vec<_> = statements
            .iter()
            .enumerate()
            .filter(|(i, s)| {
                begin < *i
                    && *i < end
                    && matches!(
                        s,
                        Statement::Assign {
                            value: Rvalue::DynCallValue { .. },
                            ..
                        }
                    )
            })
            .collect();
        assert_eq!(body_calls.len(), auto_count);
        if auto_count == 2 {
            assert!(
                body_calls[0].0 < writes[0].0
                    && writes[1].0 < body_calls[1].0
                    && body_calls[1].0 < writes[2].0
            );
        }
        let class = hir.classes.iter().next().unwrap().1;
        let binding = class.decorated_binding.unwrap();
        let public_name = format!("__mv{}_{}", binding.into_raw().into_u32(), class.name);
        let public = program
            .globals
            .iter()
            .find(|(_, g)| g.name == public_name)
            .unwrap()
            .0;
        let publishes: Vec<_> = statements.iter().enumerate().filter(|(_,s)| matches!(s, Statement::SetGlobal { dest,value } if *dest == public && !matches!(value,Operand::Const(Constant::Null)))).collect();
        assert_eq!(publishes.len(), 1);
        assert!(end < publishes[0].0);
        let path = std::env::temp_dir().join(format!(
            "ryupy_enum_structure_{}_{}.py",
            std::process::id(),
            index
        ));
        std::fs::write(&path, source).unwrap();
        let mut session = ryupy_driver::Session::default();
        let cil = support::dump(&mut session, &path, ryupy_driver::Stage::Cil).unwrap();
        assert!(session.diagnostics.is_empty());
        let create = "Call(static object [RyuPy.Runtime]RyuPy.Runtime.PyEnum.Create(string, object[], object))";
        let finish =
            "Call(static object [RyuPy.Runtime]RyuPy.Runtime.PyEnum.Finalize(object, object[]))";
        assert_eq!(cil.matches(create).count(), 1);
        assert_eq!(cil.matches(finish).count(), 1);
        assert!(cil.find(create).unwrap() < cil.find(finish).unwrap());
        std::fs::remove_file(path).unwrap();
    }
}
