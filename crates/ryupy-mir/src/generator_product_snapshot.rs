//! 最后高层生成器的只读实际输入。保存完整源操作/操作数与源行，不把 lowering 的标签当证明。
use crate::*;

pub(super) struct Source {
    pub factory: FnId,
    pub name: String,
    pub file: String,
    pub params: Vec<LocalId>,
    pub parameter_metadata: Vec<ParamMeta>,
    pub metadata: super::generator_product_identity::FunctionIdentity,
    pub locals: Vec<MirType>,
    pub local_declarations: Vec<LocalDecl>,
    pub blocks: Vec<SourceBlock>,
    pub entry: BlockId,
    pub regions: Vec<SourceRegion>,
    pub depths: Vec<LocalId>,
    pub origin: Option<Operand>,
    pub environment: Option<LocalId>,
    pub module_state: Option<LocalId>,
}

pub(super) struct SourceRegion {
    pub start: BlockId,
    pub length: usize,
    pub handler: BlockId,
    pub kind: RegionKind,
}

pub(super) struct SourceBlock {
    pub statements: Vec<SourceStatement>,
    pub terminator: Terminator,
}

pub(super) enum SourceStatement {
    Operation(Statement),
    Catch(LocalId),
}

impl Source {
    pub fn capture(id: FnId, function: &Function) -> Self {
        let mut depths = Vec::new();
        let blocks = function
            .body
            .blocks
            .iter()
            .map(|(_, block)| {
                let statements = block
                    .stmts
                    .iter()
                    .filter_map(|stmt| match stmt {
                        Statement::Assign {
                            dest,
                            value:
                                Rvalue::Call {
                                    callee:
                                        Callee::Runtime(
                                            RuntimeFn::TracePush | RuntimeFn::TracePushWithOrigin,
                                        ),
                                    ..
                                },
                        } => {
                            depths.push(*dest);
                            None
                        }
                        Statement::Eval(Rvalue::Call {
                            callee: Callee::Runtime(RuntimeFn::TracePopTo),
                            ..
                        }) => None,
                        Statement::Assign {
                            dest,
                            value: Rvalue::CatchException,
                        } => Some(SourceStatement::Catch(*dest)),
                        _ => Some(SourceStatement::Operation(stmt.clone())),
                    })
                    .collect();
                SourceBlock {
                    statements,
                    terminator: block.terminator.clone(),
                }
            })
            .collect();
        let mut regions: Vec<_> = function
            .body
            .regions
            .iter()
            .map(|region| SourceRegion {
                start: region.try_start,
                length: region.try_len,
                handler: region.handler,
                kind: region.kind.clone(),
            })
            .collect();
        regions.sort_by_key(|region| {
            (
                region.length,
                matches!(region.kind, RegionKind::FinallyGuard),
            )
        });
        Self {
            factory: id,
            name: function.name.clone(),
            file: function.source_file.clone(),
            params: function.params.clone(),
            parameter_metadata: function.param_meta.clone(),
            metadata: super::generator_product_identity::FunctionIdentity::capture(function),
            locals: function
                .body
                .locals
                .iter()
                .map(|(_, local)| local.ty.clone())
                .collect(),
            local_declarations: function
                .body
                .locals
                .iter()
                .map(|(_, local)| local.clone())
                .collect(),
            blocks,
            entry: function.body.entry,
            regions,
            depths,
            origin: function
                .dynamic_origin
                .map(|source| Operand::Local(source.owner)),
            environment: function.env,
            module_state: function.module_state,
        }
    }
}
