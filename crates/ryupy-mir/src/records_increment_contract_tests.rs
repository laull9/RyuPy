//! 从真实源码构建MIR，记录records融合仍必须处理的方法捕获、具名key和动态加法边界。
//! 本模块不实现宽松matcher，不把方法名get或单个MakeTuple当成产品准入证书。
use crate::mir::*;

fn compile(source: &str) -> Program {
    let mut db = ryupy_base::SourceDb::default();
    let file = db.add("records-contract.py", source);
    let (parsed, errors) = ryupy_syntax::parse(file, source);
    assert!(errors.is_empty(), "{errors:?}");
    let (hir, errors) = ryupy_hir::lower_module(&parsed, &Default::default(), source);
    assert!(errors.is_empty(), "{errors:?}");
    let (types, errors) = ryupy_ty::check_module(&hir);
    assert!(errors.is_empty(), "{errors:?}");
    let (program, _, errors) = crate::build_program(&hir, &types);
    assert!(errors.is_empty(), "{errors:?}");
    crate::validate::validate(&program).expect("源码MIR必须合法");
    program
}

fn function<'a>(program: &'a Program, name: &str) -> &'a Function {
    program
        .functions
        .iter()
        .find(|(_, f)| f.name == name)
        .unwrap()
        .1
}

fn values(function: &Function) -> Vec<&Rvalue> {
    function
        .body
        .blocks
        .iter()
        .flat_map(|(_, block)| &block.stmts)
        .filter_map(|statement| match statement {
            Statement::Assign { value, .. } | Statement::Eval(value) => Some(value),
            _ => None,
        })
        .collect()
}

fn has_increment_spelling(f: &Function) {
    let values = values(f);
    assert!(values.iter().any(|value| matches!(value,
        Rvalue::CallWithArgs { callee: RuntimeFn::MakeTuple, args, .. } if args.len() == 2)));
    // 捕获标志存在不等于精确dict证明；模块方法和用户对象也走同一种显式MIR。
    assert!(values.iter().any(|value| matches!(value,
        Rvalue::DynCallMethod { name, captured: Some(_), args, .. } if name == "get" && args.len() == 2)));
    assert!(values.iter().any(|value| matches!(
        value,
        Rvalue::DynBinary {
            op: DynOp::Add,
            inplace: false,
            ..
        }
    )));
    assert!(values.iter().any(|value| matches!(value,
        Rvalue::Call { callee: Callee::Runtime(RuntimeFn::SetItem), args } if args.len() == 3)));
    assert!(
        f.body
            .locals
            .iter()
            .any(|(_, local)| local.name.as_deref() == Some("key"))
    );
}

#[test]
fn original_records_retains_named_tuple_dynamic_get_add_and_write() {
    let program = compile(include_str!("../../../tests/benchmarks/records.py"));
    has_increment_spelling(function(&program, "summarize"));
}

#[test]
fn observable_key_and_generic_receiver_still_have_the_same_candidate_spelling() {
    let program = compile(
        "def step(counts, user, action):\n    key = (user, action)\n    counts[key] = counts.get(key, 0) + 1\n    return key\n",
    );
    let f = function(&program, "step");
    has_increment_spelling(f);
    assert!(
        f.body
            .blocks
            .iter()
            .any(|(_, block)| matches!(block.terminator, Terminator::Return(Some(_))))
    );
    // 函数参数counts可能是用户dict派生；语法相同不能消掉get/__setitem__协议。
    // 物理params可能另含module_state/owner；Python签名不能按ABI槽数推断。
    let names: Vec<_> = f.param_meta.iter().map(|p| p.name.as_str()).collect();
    assert_eq!(names, ["counts", "user", "action"]);
}

#[test]
fn local_observation_does_not_authorize_discarding_the_key() {
    let program = compile(
        "def step(counts, user, action):\n    key = (user, action)\n    counts[key] = counts.get(key, 0) + 1\n    visible = locals()\n    return visible['key'] is key\n",
    );
    has_increment_spelling(function(&program, "step"));
}
