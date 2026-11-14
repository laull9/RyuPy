//! 只拒真正拟发射的新MI能力，不把未取得编译产物的soft/deferred来源变硬。
use ryupy_base::{Diagnostic, Ranged, Span};
use ryupy_mir::{Callee, RuntimeFn, Rvalue, Statement};

pub(crate) fn reject_emitted_aot(
    session: &mut crate::Session,
    source: &crate::modules::SourceModule,
    program: &ryupy_mir::Program,
) {
    let has_mi = program.functions.iter().any(|(_, function)| {
        function.body.blocks.iter().any(|(_, block)| {
            block.stmts.iter().any(|statement| {
                let value = match statement {
                    Statement::Assign { value, .. } | Statement::Eval(value) => value,
                    _ => return false,
                };
                match value {
                    Rvalue::Call { callee: Callee::Runtime(RuntimeFn::PrepareOrdinaryMiClass), .. } => true,
                    // 词法frame沿旧ABI保全部bases；确定多基同样属于新C3域，不能绕过Native保护。
                    Rvalue::Call { callee: Callee::Runtime(RuntimeFn::LexicalClassPrepare), args } =>
                        matches!(args.get(3), Some(ryupy_mir::Operand::Array(bases)) if bases.len() > 1),
                    _ => false,
                }
            })
        })
    });
    if has_mi {
        session.diagnostics.push(
            Diagnostic::error(
                Span::new(source.parsed.file, source.parsed.module.range()),
                "普通多继承C3当前仅支持Legacy JIT；NativeAOT尚未验证",
            )
            .with_code("E0100"),
        );
    }
}
