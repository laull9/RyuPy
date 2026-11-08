//! 固定旧负例的有限合同：Legacy名称目标可降级；Instance与Call目标仍在HIR拒绝。
//! 这里只验证真实IR与阶段边界，不代替JIT/Native运行或原frames验收。
use super::*;
use ryupy_driver::{ModuleEnvironmentPolicy, Session, Stage};
use std::path::Path;

fn session(instance: bool) -> Session {
    Session {
        module_environment_policy: ModuleEnvironmentPolicy::ExplicitOnly,
        instance_modules: if instance { vec!["__main__".into()] } else { vec![] },
        ..Default::default()
    }
}

fn reject_at_hir(input: &Path, instance: bool, message: &str) {
    // dump(Hir)即使带HIR错误也返回文本；不能将Ok误称编译支持。
    let mut ast = session(instance);
    assert!(ryupy_driver::dump(&mut ast, input, Stage::Ast).is_ok());
    assert!(!ast.diagnostics.has_errors(), "{:?}", ast.diagnostics);
    let mut hir = session(instance);
    assert!(ryupy_driver::dump(&mut hir, input, Stage::Hir).is_ok());
    assert!(hir.diagnostics.iter().any(|d| {
        d.is_error() && d.code == Some("E0100") && d.message.contains(message)
    }), "{:?}", hir.diagnostics);
    let mut mir = session(instance);
    assert!(ryupy_driver::dump(&mut mir, input, Stage::Mir).is_err());
    assert!(mir.diagnostics.iter().any(|d| {
        d.is_error() && d.code == Some("E0100") && d.message.contains(message)
    }), "{:?}", mir.diagnostics);
}

pub(super) fn check(root: &Path) {
    const GENERATOR: &str = include_str!("../../../../tests/fixtures/dynamic_except_structure/ni_generator.py");
    const CALL: &str = include_str!("../../../../tests/fixtures/dynamic_except_structure/ni_call.py");
    let input = root.join("ni_generator.py");
    std::fs::write(&input, GENERATOR).unwrap();
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("ni_generator.py", GENERATOR);
    let (parsed, diagnostics) = ryupy_syntax::parse(file, GENERATOR);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let info = ryupy_hir::ModuleInfo {
        instance_globals: false,
        name: "__main__".into(),
        file: "ni_generator.py".into(),
        ..Default::default()
    };
    let (hir, diagnostics) = ryupy_hir::lower_module(&parsed, &info, GENERATOR);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let functions: Vec<_> = hir.functions.iter().map(|(_, f)| f)
        .filter(|f| f.name.as_str() == "unsupported").collect();
    assert_eq!(functions.len(), 1);
    let generator = functions[0];
    assert!(generator.is_generator);
    assert_eq!(generator.params.len(), 1);
    assert_eq!(generator.body.handlers.len(), 1);
    let handler = generator.body.handlers.iter().next().unwrap().1;
    assert_eq!(handler.semantics, ryupy_hir::HandlerSemantics::SourceDynamic);
    assert!(handler.types.is_none() && handler.name.is_none());
    assert_eq!(handler.type_line, 5);
    assert!(matches!(generator.body.exprs[handler.type_expr.unwrap()].kind,
        ryupy_hir::ExprKind::Local(id) if id == generator.params[0].local));
    let (types, diagnostics) = ryupy_ty::check_module(&hir);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    let (program, _, diagnostics) = ryupy_mir::build_program(&hir, &types);
    assert!(diagnostics.is_empty(), "{diagnostics:?}");
    assert!(ryupy_mir::validate::validate(&program).is_ok());
    // 从工厂的真实MakeGenerator引用找恢复函数，不按名称猜测重复函数。
    let factories: Vec<_> = program.functions.iter().flat_map(|(_, f)| {
        f.body.blocks.iter().flat_map(move |(_, b)| b.stmts.iter().filter_map(move |s| {
            match value(s) {
                Some(Rvalue::MakeGenerator { resume, frame, .. }) => Some((f, *resume, frame)),
                _ => None,
            }
        }))
    }).collect();
    assert_eq!(factories.len(), 1);
    let (factory, resume_id, initial) = factories[0];
    assert_eq!(factory.name, "unsupported");
    assert_eq!(factory.params.len(), 1);
    let target = factory.params[0];
    assert_eq!(local(&initial[index_local(target) + 4]), target);
    let resume = &program.functions[resume_id];
    assert_eq!(resume.name, factory.name);
    assert!(resume.module_state.is_none() && resume.module_state_source.is_none());
    assert!(resume.body.source_handler_contracts.is_empty());
    assert_eq!(resume.params.len(), 1);
    assert_eq!(resume.body.regions.len(), 1);
    let outer = &resume.body.regions[0];
    assert!(matches!(outer.kind, RegionKind::CatchAll));
    assert!(outer.source_handler_chain.is_none());
    assert_eq!(outer.try_start, resume.body.entry);
    assert!(!inside(outer, outer.handler));
    let enters = calls(resume, RuntimeFn::HandlerEnter);
    let leaves = calls(resume, RuntimeFn::HandlerLeave);
    let matches = calls(resume, RuntimeFn::ExceptionMatch);
    assert_eq!(enters.len(), 1);
    assert_eq!(leaves.len(), 1);
    assert_eq!(matches.len(), 1);
    assert_eq!(local(&matches[0].3[0]), local(&enters[0].3[0]));
    let reads = calls(resume, RuntimeFn::LoadChecked);
    let target_reads: Vec<_> = reads.iter().filter(|c| {
        matches!(c.3, [Operand::Local(id), Operand::Const(Constant::Str(name))]
            if *id == target && name == "target")
    }).collect();
    assert_eq!(target_reads.len(), 1);
    let read = target_reads[0];
    assert_eq!(read.2, Some(local(&matches[0].3[1])));
    assert!(read.0 == matches[0].0 && read.1 < matches[0].1);
    assert_eq!(resume.body.blocks[read.0].stmts[..read.1].iter().rev().find_map(|s| {
        if let Statement::Line(line) = s { Some(*line) } else { None }
    }), Some(5));
    assert_eq!(local(&leaves[0].3[0]), enters[0].2.unwrap());
    assert_eq!(calls(resume, RuntimeFn::GeneratorRetry).len(), 1);
    for (id, block) in resume.body.blocks.iter() {
        assert!(!matches!(block.terminator, Terminator::Suspend { .. }));
        if inside(outer, id) {
            assert!(!matches!(block.terminator, Terminator::Return(_)));
        }
    }
    // 原对象、目标与ambient token均保存真实引用，并由同一frame/槽恢复。
    for id in [target, local(&enters[0].3[0]), enters[0].2.unwrap()] {
        let frame = resume.params[0];
        let slot = (index_local(id) + 4) as i64;
        assert_eq!(calls(resume, RuntimeFn::ArraySet).iter().filter(|c| {
            matches!(c.3, [Operand::Local(f), Operand::Const(Constant::I64(s)), Operand::Local(v)]
                if *f == frame && *s == slot && *v == id)
        }).count(), 3, "两个yield和Retry均保存同一引用槽");
        assert_eq!(calls(resume, RuntimeFn::ArrayGet).iter().filter(|c| {
            c.2 == Some(id) && matches!(c.3,
                [Operand::Local(f), Operand::Const(Constant::I64(s))] if *f == frame && *s == slot)
        }).count(), 1);
    }
    let mut legacy = session(false);
    let mir = ryupy_driver::dump(&mut legacy, &input, Stage::Mir).unwrap();
    assert!(!legacy.diagnostics.has_errors(), "{:?}", legacy.diagnostics);
    std::fs::write(root.join("ni_generator.mir"), mir).unwrap();
    reject_at_hir(&input, true, "生成器或类体源码except handler");
    let input = root.join("ni_call.py");
    std::fs::write(&input, CALL).unwrap();
    reject_at_hir(&input, false, "动态 except 目标仅支持名称、名称起点的属性链及无展开元组");
}

fn index_local(id: LocalId) -> usize {
    u32::from(id.into_raw()) as usize
}
