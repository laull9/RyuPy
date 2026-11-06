//! 实例函数的惰性ABI及共享代码委托缓存；缓存绝不保存context、self、defaults或cells。
use dotnetdll::prelude::*;
use rustc_hash::FxHashMap;
use ryupy_mir::{FnId, InstanceStateSource, LinkedProgram};

pub(crate) struct OwnedFunctionRefs {
    pub create: MethodRefIndex,
    pub delegate_ctor: MethodRefIndex,
    delegate_type: MemberType,
    pub delegates: FxHashMap<FnId, FieldIndex>,
}

impl OwnedFunctionRefs {
    pub(crate) fn for_program(
        res: &mut Resolution<'static>,
        program: &LinkedProgram,
        runtime: AssemblyRefIndex,
    ) -> Option<Self> {
        if !program
            .functions
            .iter()
            .any(|(_, f)| f.module_state_source == Some(InstanceStateSource::FunctionParameter))
        {
            return None;
        }
        let instances =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PyModuleInstances in #runtime });
        let instances: MethodType = BaseType::class(instances).into();
        let delegate = res.push_type_reference(type_ref! { RyuPy.Runtime.PyOwnedCall in #runtime });
        let delegate_type = BaseType::class(delegate).into();
        let delegate: MethodType = BaseType::class(delegate).into();
        let function = res.push_type_reference(type_ref! { RyuPy.Runtime.PyFunction in #runtime });
        let function: MethodType = BaseType::class(function).into();
        let signature =
            res.push_type_reference(type_ref! { RyuPy.Runtime.PySignature in #runtime });
        let signature: MethodType = BaseType::class(signature).into();
        Some(Self {
            create: res.push_method_reference(method_ref! { static @function @instances::CreateOwnedFunction(object, string, @signature, @delegate, object[], bool, object[]) }),
            delegate_ctor: res.push_method_reference(method_ref! { void @delegate::.ctor(object, nint) }),
            delegate_type,
            delegates: FxHashMap::default(),
        })
    }
}

/// 在共享初始化入口缓存无捕获委托。未选程序不增加字段、方法引用或初始化指令。
pub(crate) fn initialize(
    res: &mut Resolution<'static>,
    module: TypeIndex,
    program: &LinkedProgram,
    refs: &mut crate::stackify_refs::RuntimeRefs,
    thunks: &FxHashMap<FnId, MethodIndex>,
    init_literals: MethodIndex,
) -> MethodIndex {
    let init = crate::callables::emit_init_functions(res, module, program, refs);
    // 字面量先初始化，def默认值才可读取它；未选路径指令次序保持不变。
    if let Some(body) = res[init].body.as_mut() {
        body.instructions
            .insert(0, Instruction::call(init_literals));
        body.header.maximum_stack_size = body.header.maximum_stack_size.max(8);
    }
    let Some(owned) = refs.owned_functions.as_mut() else {
        return init;
    };
    let mut instructions = Vec::new();
    for (id, function) in program.functions.iter() {
        if function.module_state_source != Some(InstanceStateSource::FunctionParameter) {
            continue;
        }
        let field = res.push_field(
            module,
            Field::static_member(
                Accessibility::Private,
                format!("__owned_code_{}", id.into_raw().into_u32()),
                owned.delegate_type.clone(),
            ),
        );
        owned.delegates.insert(id, field);
        instructions.extend([
            Instruction::LoadNull,
            Instruction::LoadMethodPointer(thunks[&id].into()),
            Instruction::NewObject(UserMethod::Reference(owned.delegate_ctor)),
            Instruction::StoreStaticField {
                volatile: false,
                param0: field.into(),
            },
        ]);
    }
    let body = res[init].body.as_mut().expect("共享签名初始化已有方法体");
    let at = body.instructions.len() - 1;
    body.instructions.splice(at..at, instructions);
    init
}
