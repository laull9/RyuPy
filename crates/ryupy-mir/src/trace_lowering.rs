//! 资格事实到实际 MIR 的显式来源旁证，不进入 CIL，也不签发可运行协议。
//! 只在独立审计入口收集。普通 build/dump 保持现有结果与选路。
use crate::{BlockId, FnId, GlobalId, LocalId, Operand, Program, Rvalue, Statement};
use rustc_hash::FxHashMap;

#[derive(Clone, Debug)]
pub struct ExpressionOrigin {
    pub expression: ryupy_hir::ExprId,
    pub block: BlockId,
    /// generator lowering 之前的完成点；不是最终语句身份。
    /// 后续 lowering/pass 必须维护多重产生点映射，不能把LocalId终身标为旧事实。
    pub source_statement_cursor: usize,
    /// 该表达式完成时的真实结果，不能按临时变量名称重新推断。
    pub result: Operand,
}

#[derive(Debug, Default)]
pub struct FunctionOrigin {
    pub locals: FxHashMap<ryupy_hir::LocalId, LocalId>,
    pub expressions: Vec<ExpressionOrigin>,
}

#[derive(Clone, Debug)]
pub struct GeneratorOrigin {
    pub factory: FnId,
    pub resume: FnId,
    pub frame_parameter: LocalId,
    /// 工厂真实 MakeGenerator 输入；恢复槽必须逐项检验，不能只检验数组长度。
    pub initial_slots: Vec<Operand>,
}

/// 尚未验证的旁证，不是证书。passes 可能使 block/result 失效，最终审计必须重验。
#[derive(Debug, Default)]
pub struct LoweringOrigins {
    pub functions: FxHashMap<ryupy_hir::FuncId, FnId>,
    pub classes: FxHashMap<ryupy_hir::ClassId, GlobalId>,
    pub globals: FxHashMap<ryupy_hir::ModuleVarId, GlobalId>,
    pub bodies: FxHashMap<ryupy_hir::FuncId, FunctionOrigin>,
    pub generators: Vec<GeneratorOrigin>,
}

impl LoweringOrigins {
    pub(crate) fn collect_generators(&mut self, program: &Program) {
        for (factory, function) in program.functions.iter() {
            for (_, block) in function.body.blocks.iter() {
                for statement in &block.stmts {
                    if let Statement::Assign {
                        value: Rvalue::MakeGenerator { resume, frame, .. },
                        ..
                    } = statement
                        && let Some(frame_parameter) = program.functions[*resume].params.first()
                    {
                        self.generators.push(GeneratorOrigin {
                            factory,
                            resume: *resume,
                            frame_parameter: *frame_parameter,
                            initial_slots: frame.clone(),
                        });
                    }
                }
            }
        }
    }
}

/// 只有主动调用这个入口才收集 HIR→MIR 旁证；不会启用新运行时协议。
pub fn build_for_audit(
    hir: &ryupy_hir::Module,
    types: &ryupy_ty::ModuleTypes,
) -> (
    Program,
    Vec<crate::ModuleExport>,
    Vec<ryupy_base::Diagnostic>,
    LoweringOrigins,
) {
    let mut origins = LoweringOrigins::default();
    let (program, exports, diagnostics) =
        crate::build::build_program_with_origins(hir, types, Some(&mut origins));
    (program, exports, diagnostics, origins)
}

/// 链接器实际分配的身份，不假定单模块的 arena 下标恰巧不变。
#[derive(Debug)]
pub struct LinkRemap {
    pub functions: FxHashMap<FnId, FnId>,
    pub globals: FxHashMap<GlobalId, GlobalId>,
}

impl LinkRemap {
    pub(crate) fn new(
        functions: &FxHashMap<FnId, FnId>,
        globals: &FxHashMap<GlobalId, GlobalId>,
    ) -> Self {
        Self {
            functions: functions.clone(),
            globals: globals.clone(),
        }
    }
}

pub fn link_for_audit(
    units: Vec<(Program, crate::UnitMeta)>,
    entry: usize,
) -> (crate::LinkedProgram, Vec<LinkRemap>) {
    let mut maps = Vec::new();
    let linked = crate::link::link_with_origins(units, entry, Some(&mut maps));
    (linked, maps)
}
