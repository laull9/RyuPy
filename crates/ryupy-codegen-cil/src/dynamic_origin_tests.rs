//! 小方案后端的单一结构门：真实PE检查thunk双路self、隐藏参数、来源引用和生成器两种ABI。

use dotnetdll::prelude::*;
use la_arena::Arena;
use ryupy_mir::{
    Callee, Constant, DynamicFunctionOrigin, GlobalDecl, GlobalId, LinkedProgram, MirType, Operand,
    ParamKind, ParamMeta, RuntimeFn, Rvalue, Statement,
};

use crate::exact_i64_tests::{function, local, program, single_block};

fn global(program: &mut LinkedProgram, name: &str) -> GlobalId {
    program.globals.alloc(GlobalDecl {
        storage: ryupy_mir::GlobalStorage::LegacyStatic,
        name: name.into(),
        ty: MirType::Dynamic,
    })
}

fn bytes(program: &LinkedProgram) -> Vec<u8> {
    crate::emit_assembly(
        program,
        &crate::EmitOptions {
            assembly_name: "dynamic_origin".into(),
            entry_source_path: None,
        },
    )
    .expect("结构fixture应能发射PE")
}

fn object_type(res: &Resolution<'_>) -> MethodType {
    let (id, _) = res
        .enumerate_type_references()
        .find(|(_, ty)| ty.name == "Object" && ty.namespace.as_deref() == Some("System"))
        .unwrap();
    BaseType::class(id).into()
}

fn check_thunk(dynamic: bool, closure: bool) {
    let mut program = program();
    let mut locals = Arena::default();
    let ty = if dynamic {
        MirType::Dynamic
    } else {
        MirType::I64
    };
    let argument = local(&mut locals, ty.clone());
    let env = closure.then(|| local(&mut locals, MirType::Dynamic));
    let owner = dynamic.then(|| local(&mut locals, MirType::Dynamic));
    let depth = local(&mut locals, MirType::I64);
    let mut params = vec![argument];
    params.extend(env);
    params.extend(owner);
    let mut trace_args = vec![
        Operand::Const(Constant::Str("origin.py".into())),
        Operand::Const(Constant::Str("owner_core".into())),
    ];
    if let Some(owner) = owner {
        trace_args.push(Operand::Local(owner));
    }
    let mut core = function(
        "owner_core",
        single_block(
            locals,
            vec![
                Statement::Assign {
                    dest: depth,
                    value: Rvalue::Call {
                        callee: Callee::Runtime(if dynamic {
                            RuntimeFn::TracePushWithOrigin
                        } else {
                            RuntimeFn::TracePush
                        }),
                        args: trace_args,
                    },
                },
                Statement::Eval(Rvalue::Call {
                    callee: Callee::Runtime(RuntimeFn::TracePopTo),
                    args: vec![Operand::Local(depth)],
                }),
            ],
            Some(Operand::Local(argument)),
        ),
        params,
        ty.clone(),
    );
    core.param_meta.push(ParamMeta {
        name: "value".into(),
        kind: ParamKind::PositionalOrKeyword,
        has_default: true,
        ty,
    });
    core.env = env;
    core.signature_global = Some(global(&mut program, "owner_signature"));
    core.dynamic_origin = owner.map(|owner| DynamicFunctionOrigin {
        owner,
        source_global: None,
    });
    let expected_params = core.params.len();
    program.functions.alloc(core);
    let bytes = bytes(&program);
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let methods: Vec<_> = res
        .type_definitions
        .iter()
        .flat_map(|ty| &ty.methods)
        .collect();
    let core = methods
        .iter()
        .find(|method| method.name == "owner_core")
        .unwrap();
    let object = object_type(&res);
    let expected_signature = match (dynamic, closure) {
        (true, true) => msig! { static @object (@object, @object, @object) },
        (true, false) => msig! { static @object (@object, @object) },
        _ => msig! { static long (long) },
    };
    assert_eq!(core.signature, expected_signature);
    let core_body = core.body.as_ref().unwrap();
    assert_eq!(
        core_body
            .instructions
            .iter()
            .any(|ins| ins.show(&res).contains("PushFrameWithOrigin")),
        dynamic,
    );
    let thunk = methods
        .iter()
        .find(|method| method.name == "__thunk_0")
        .unwrap();
    let body = thunk.body.as_ref().unwrap();
    // 快路径只比较一个Python实参，不得把env/owner计入绑定实参数量。
    assert!(matches!(
        body.instructions[3],
        Instruction::LoadConstantInt32(1)
    ));
    assert_eq!(body.header.maximum_stack_size, expected_params + 8);
    let calls: Vec<_> = body
        .instructions
        .iter()
        .enumerate()
        .filter_map(|(index, ins)| ins.show(&res).contains("owner_core(").then_some(index))
        .collect();
    assert_eq!(calls.len(), 2, "快慢两条thunk路径都须调用同一core");
    for index in calls {
        assert_eq!(
            matches!(body.instructions[index - 1], Instruction::LoadArgument(0)),
            dynamic
        );
        if closure {
            assert!(body.instructions[index - 2].show(&res).contains("get_Env"));
            assert!(matches!(
                body.instructions[index - 3],
                Instruction::LoadArgument(0)
            ));
        }
    }
    // 来源只传self，不能在thunk读取Module或创建额外参数数组。
    assert!(!body.instructions.iter().any(|ins| {
        let shown = ins.show(&res);
        shown.contains("get_Module") || shown.contains("FunctionModule")
    }));
    assert!(
        !body
            .instructions
            .iter()
            .any(|ins| matches!(ins, Instruction::NewArray(_)))
    );
}

fn check_generator(with_origin: bool) {
    let mut program = program();
    let mut resume_locals = Arena::default();
    let frame = local(&mut resume_locals, MirType::Dynamic);
    let resume = program.functions.alloc(function(
        "owner_resume",
        single_block(resume_locals, vec![], Some(Operand::Const(Constant::Null))),
        vec![frame],
        MirType::Dynamic,
    ));
    let mut locals = Arena::default();
    let owner = with_origin.then(|| local(&mut locals, MirType::Dynamic));
    let generator = local(&mut locals, MirType::Dynamic);
    let mut factory = function(
        "owner_generator",
        single_block(
            locals,
            vec![Statement::Assign {
                dest: generator,
                value: Rvalue::MakeGenerator {
                    resume,
                    // 宽frame使实际方法头上限超过256，不能被既有保底值掩盖漏槽。
                    frame: vec![Operand::Const(Constant::Null); 250],
                    origin: owner.map(Operand::Local),
                },
            }],
            Some(Operand::Local(generator)),
        ),
        owner.into_iter().collect(),
        MirType::Dynamic,
    );
    factory.dynamic_origin = owner.map(|owner| DynamicFunctionOrigin {
        owner,
        source_global: None,
    });
    factory.signature_global = Some(global(&mut program, "generator_signature"));
    program.functions.alloc(factory);
    let bytes = bytes(&program);
    let res = Resolution::parse(&bytes, ReadOptions::default()).unwrap();
    let methods: Vec<_> = res
        .type_definitions
        .iter()
        .flat_map(|ty| &ty.methods)
        .collect();
    let resume = methods
        .iter()
        .find(|method| method.name == "owner_resume")
        .unwrap();
    let object = object_type(&res);
    assert_eq!(resume.signature, msig! { static @object (@object) });
    let factory = methods
        .iter()
        .find(|method| method.name == "owner_generator")
        .unwrap();
    let body = factory.body.as_ref().unwrap();
    assert_eq!(
        body.header.maximum_stack_size,
        250 + 5 + usize::from(with_origin) + 16
    );
    let constructors: Vec<_> = body
        .instructions
        .iter()
        .enumerate()
        .filter_map(|(index, ins)| {
            if let Instruction::NewObject(UserMethod::Reference(reference)) = ins
                && ins.show(&res).contains("PyGenerator")
            {
                Some((index, &res[*reference]))
            } else {
                None
            }
        })
        .collect();
    assert_eq!(constructors.len(), 1);
    let (index, constructor) = constructors[0];
    assert_eq!(
        constructor.signature.parameters.len(),
        4 + usize::from(with_origin)
    );
    if with_origin {
        assert!(matches!(
            body.instructions[index - 1],
            Instruction::LoadArgument(0)
        ));
        assert_eq!(
            constructor.signature.parameters.last(),
            Some(&Parameter::value(BaseType::Object.into()))
        );
    }
}

#[test]
fn dynamic_owner_backend_contract() {
    check_thunk(false, false);
    check_thunk(true, false);
    check_thunk(true, true);
    check_generator(false);
    check_generator(true);

    // 精确CLR签名直接对照已实现的运行时公开API，不以方法名存在代替ABI检查。
    let mut res = Resolution::new(Module::new("origin_refs.dll"));
    let (refs, _) = crate::refs::build_runtime_refs(&mut res);
    assert_eq!(
        res[refs.origins.push_frame].signature,
        msig! { static long (string, string, object) }
    );
    assert_eq!(
        res[refs.origins.module_source].signature,
        msig! { static object (object) }
    );
}
