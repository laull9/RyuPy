//! 薄函数冷ABI和无捕获静态adapter；只消费MIR冻结信息，禁止依赖HIR或类型推导。
use crate::stackify::LowerCtx;
use crate::stackify_operands::lower_operand;
use dotnetdll::prelude::*;
use ryupy_mir::{FnId, InstanceStateSource, LinkedProgram};

pub(crate) struct Refs {
    module: String,
    environment: MethodType,
    pub load_global: MethodRefIndex,
    definition: MethodRefIndex,
    delegate: MethodRefIndex,
    create: MethodRefIndex,
}
impl Refs {
    pub fn for_program(
        res: &mut Resolution<'static>,
        program: &LinkedProgram,
        runtime: AssemblyRefIndex,
    ) -> Option<Self> {
        if !program
            .functions
            .iter()
            .any(|(_, f)| f.module_state_source == Some(InstanceStateSource::StockThinLease))
        {
            return None;
        }
        let environment = res
            .push_type_reference(type_ref! { RyuPy.Runtime.IPyStockThinEnvironment in #runtime });
        let environment: MethodType = BaseType::class(environment).into();
        let definition =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyStockThinDefinition in #runtime });
        let definition: MethodType = BaseType::class(definition).into();
        let delegate =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyStockThinBody in #runtime });
        let delegate: MethodType = BaseType::class(delegate).into();
        let instances =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleInstances in #runtime });
        let instances: MethodType = BaseType::class(instances).into();
        let functions =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyStockThinFunctions in #runtime });
        let functions: MethodType = BaseType::class(functions).into();
        Some(Self {
            module: program.modules[program.entry].name.clone(),
            environment,
            load_global: res.push_method_reference(method_ref! { static object @functions::LoadGlobal(object, string) }),
            definition: res.push_method_reference(method_ref! { void @definition::.ctor(string, string, string, string, int) }),
            delegate: res.push_method_reference(method_ref! { void @delegate::.ctor(object, nint) }),
            create: res.push_method_reference(method_ref! { static object @instances::CreateStockThinFunction(object, @definition, @delegate) }),
        })
    }
}

pub(crate) fn adapter(
    res: &mut Resolution<'static>,
    module: TypeIndex,
    method: MethodIndex,
    refs: &Refs,
    index: usize,
) -> MethodIndex {
    // Python实参在前，隐藏object lease在后；接口值仅借给这次实际body调用。
    let signature = MethodSignature::new(
        false,
        ReturnType::value(BaseType::Object.into()),
        vec![
            Parameter::value(refs.environment.clone()),
            Parameter::value(BaseType::Object.into()),
        ],
    );
    res.push_method(
        module,
        Method::new(
            Accessibility::Private,
            signature,
            format!("__stock_thin_adapter_{index}"),
            Some(body::Method::new(vec![
                Instruction::LoadArgument(1),
                Instruction::LoadArgument(0),
                Instruction::call(method),
                Instruction::Return,
            ])),
        ),
    )
}

pub(crate) fn emit(
    func: FnId,
    context: &ryupy_mir::Operand,
    ins: &mut Vec<Instruction>,
    ctx: &LowerCtx<'_>,
) {
    let refs = ctx.runtime_refs.stock_thin.as_ref().expect("冻结薄函数ABI");
    let function = &ctx.functions[func];
    lower_operand(
        context,
        ins,
        ctx.param_slots,
        ctx.local_slots,
        ctx.runtime_refs,
    );
    // 定义来源是MIR冻结的冷数据；没有绑定旧PyFunction、Context或adapter对象。
    ins.extend([
        Instruction::load_string(function.codegen_name.clone()),
        Instruction::load_string(refs.module.clone()),
        Instruction::load_string(function.name.clone()),
        Instruction::load_string(function.source_file.clone()),
        Instruction::LoadConstantInt32(
            function.definition.as_ref().expect("源码定义").firstlineno as i32,
        ),
        Instruction::NewObject(UserMethod::Reference(refs.definition)),
        Instruction::LoadNull,
        Instruction::LoadMethodPointer(ctx.thunks[&func].into()),
        Instruction::NewObject(UserMethod::Reference(refs.delegate)),
        Instruction::call(refs.create),
    ]);
}
