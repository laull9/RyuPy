//! 集中反例的共同基底：真实前端及七pass生成的同块def发布链，不使用手造helper白名单。
use crate::generator_product_defaults::Defaults;
use crate::generator_product_tests::{Observer, frontend};
use crate::*;

pub(super) const SOURCE: &str =
    "def generate(value, other):\n    yield value\nit = generate('ready', 'other')\nnext(it)\n";
pub(super) struct Fixture {
    pub program: Program,
    pub exports: Vec<ModuleExport>,
    pub block: BlockId,
    pub producer: usize,
    pub getter: usize,
    pub publish: usize,
    pub target: FnId,
    pub receiver: LocalId,
    pub result: LocalId,
}
impl Fixture {
    pub fn new() -> Self {
        let (hir, types) = frontend(SOURCE);
        let mut plan = generator_product::DeferredGenerators::default();
        let (mut program, exports, errors) =
            build::build_program_mode(&hir, &types, None, Some(&mut plan));
        assert!(errors.is_empty());
        passes::run_pipeline_high(
            &mut program,
            &passes::default_pipeline(),
            &mut Observer,
            plan.functions(),
        )
        .unwrap();
        Defaults::inspect(&program, &exports).unwrap();
        let module = &program.functions[program.entry];
        let (block, getter, result, receiver) = module
            .body
            .blocks
            .iter()
            .find_map(|(id, block)| {
                block.stmts.iter().enumerate().find_map(|(at, statement)| {
                    match statement {
                        Statement::Assign {
                            dest,
                            value:
                                Rvalue::Call {
                                    callee: Callee::Runtime(RuntimeFn::FunctionDefaults),
                                    args,
                                },
                        } => {
                            let [Operand::Local(receiver)] = args.as_slice() else {
                                panic!("实际receiver");
                            };
                            Some((id, at, *dest, *receiver))
                        }
                        _ => None,
                    }
                })
            })
            .unwrap();
        let (producer, target) = module.body.blocks[block]
            .stmts
            .iter()
            .enumerate()
            .find_map(|(at, s)| match s {
                Statement::Assign {
                    dest,
                    value: Rvalue::MakeFunction { func, .. },
                } if *dest == receiver => Some((at, *func)),
                _ => None,
            })
            .unwrap();
        let publish = getter + 1;
        assert!(matches!(
            module.body.blocks[block].stmts[publish],
            Statement::SetGlobal { .. }
        ));
        Self {
            program,
            exports,
            block,
            producer,
            getter,
            publish,
            target,
            receiver,
            result,
        }
    }
    pub fn stmts(&mut self) -> &mut Vec<Statement> {
        &mut self.program.functions[self.program.entry].body.blocks[self.block].stmts
    }
    pub fn local(&mut self) -> LocalId {
        self.program.functions[self.program.entry]
            .body
            .locals
            .alloc(LocalDecl {
                ty: MirType::Dynamic,
                name: None,
                is_cell: false,
            })
    }
    pub fn reject(&self, expected: &str) {
        let error = match Defaults::inspect(&self.program, &self.exports) {
            Ok(_) => panic!("新合同错误接受：{expected}"),
            Err(error) => error,
        };
        assert!(error.starts_with("FunctionDefaults合同："), "{error}");
        assert!(error.contains(expected), "期待{expected}，实际{error}");
    }
}
