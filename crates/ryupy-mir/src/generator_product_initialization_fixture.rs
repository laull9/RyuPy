//! 初始化集中反例的真实前端/七pass基底；先直接核新合同，再做单个目标变异。
use crate::generator_product_initialization::Initialization;
use crate::generator_product_tests::{B, Observer};
use crate::trace_lowering::LoweringOrigins;
use crate::*;

pub(super) struct Fixture {
    pub program: Program,
    pub exports: Vec<ModuleExport>,
    pub plan: generator_product::DeferredGenerators,
    pub initialization: Initialization,
    pub block: BlockId,
}
impl Fixture {
    pub fn new(runtime_file: bool) -> Self {
        let mut db = ryupy_base::SourceDb::default();
        let file = db.add("product.py", B);
        let (parsed, errors) = ryupy_syntax::parse(file, B);
        assert!(errors.is_empty());
        let info = ryupy_hir::ModuleInfo {
            name: "__main__".into(),
            file: "product.py".into(),
            resolve_file_at_runtime: runtime_file,
            ..Default::default()
        };
        let (hir, errors) = ryupy_hir::lower_module(&parsed, &info, B);
        assert!(errors.is_empty());
        let (types, errors) = ryupy_ty::check_module(&hir);
        assert!(errors.is_empty());
        let mut origins = LoweringOrigins::default();
        let mut plan = generator_product::DeferredGenerators::default();
        let (mut program, exports, errors) = build::build_program_mode(
            &hir,
            &types,
            Some(&mut origins),
            Some(&mut plan),
        );
        assert!(errors.is_empty());
        let initialization = Initialization::expected(&hir, &origins, &program, &exports).unwrap();
        passes::run_pipeline_high(
            &mut program,
            &passes::default_pipeline(),
            &mut Observer,
            plan.functions(),
        )
        .unwrap();
        initialization
            .inspect(&program, &exports)
            .expect("真实前言必须先通过本合同");
        let block = program.functions[program.entry].body.entry;
        Self {
            program,
            exports,
            plan,
            initialization,
            block,
        }
    }
    pub fn stmts(&mut self) -> &mut Vec<Statement> {
        &mut self.program.functions[self.program.entry].body.blocks[self.block].stmts
    }
    pub fn slot(&self, key: &str) -> GlobalId {
        self.exports.iter().find(|e| e.name == key).unwrap().value
    }
    pub fn publish(&self, key: &str) -> usize {
        let slot = self.slot(key);
        self.program.functions[self.program.entry].body.blocks[self.block].stmts.iter()
            .rposition(|s| matches!(s, Statement::SetGlobal { dest, .. } if *dest == slot)).unwrap()
    }
    pub fn call(&self, runtime: RuntimeFn) -> usize {
        self.program.functions[self.program.entry].body.blocks[self.block].stmts.iter()
            .position(|s| matches!(s, Statement::Assign { value: Rvalue::Call { callee: Callee::Runtime(r), .. }, .. }
                | Statement::Eval(Rvalue::Call { callee: Callee::Runtime(r), .. }) if *r == runtime)).unwrap()
    }
    pub fn local(&mut self) -> LocalId {
        self.program.functions[self.program.entry].body.locals.alloc(LocalDecl {
            ty: MirType::Dynamic,
            name: None,
            is_cell: false,
        })
    }
    pub fn reject(&self, reason: &str) {
        let result = self.initialization.inspect(&self.program, &self.exports).unwrap_err();
        assert!(result.contains(reason), "期待{reason}，实际{result}");
    }
    pub fn meta(&self) -> UnitMeta {
        UnitMeta {
            name: "__main__".into(),
            package: String::new(),
            file: "product.py".into(),
            exports: self.exports.clone(),
            protected_exports: vec![],
            execution: ModuleExecutionKind::LegacySingleton,
            instance_capabilities: Default::default(),
            environment_reason: "test-init".into(),
            location: ModuleLocation::LegacyDiagnostic,
            file_backed_by_global: true,
            is_package: false,
        }
    }
}
