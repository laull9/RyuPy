//! 生成器发射合同：工厂只建立帧，恢复函数持有迭代/暂停副作用，委托精确指向该恢复函数。
#![allow(dead_code)]
use crate::support::cil_contract::{Cil, Method};
pub fn pair<'a>(cil: &'a Cil<'a>, name: &str) -> (&'a Method<'a>, &'a Method<'a>) {
    let factory = cil.method(name);
    let prefix = format!("{}$resume", factory.name);
    let resumes: Vec<_> = cil
        .methods
        .iter()
        .filter(|m| {
            m.name
                .strip_prefix(&prefix)
                .is_some_and(|tail| !tail.is_empty() && tail.bytes().all(|b| b.is_ascii_digit()))
        })
        .collect();
    assert_eq!(resumes.len(), 1);
    let resume = resumes[0];
    assert_eq!(factory.opcode_count("LoadMethodPointer"), 1);
    let target = format!(
        "{}.{}([System.Runtime]System.Object))",
        resume.owner, resume.name
    );
    assert_eq!(
        factory
            .instructions
            .iter()
            .filter(|i| i.starts_with("LoadMethodPointer(") && i.ends_with(&target))
            .count(),
        1
    );
    assert_eq!(factory.opcode_count("NewArray"), 1);
    let constructors: Vec<_> = factory
        .instructions
        .iter()
        .copied()
        .filter(|i| i.starts_with("NewObject("))
        .collect();
    assert_eq!(
        constructors,
        vec![
            "NewObject(void [RyuPy.Runtime]RyuPy.Runtime.PyResume..ctor(object, nint))",
            "NewObject(void [RyuPy.Runtime]RyuPy.Runtime.PyGenerator..ctor([RyuPy.Runtime]RyuPy.Runtime.PyResume, object[], string, string, object))",
        ]
    );
    assert_eq!(factory.opcode_count("Return"), 1);
    for member in ["PyOps.IterStart", "PyOps.GeneratorInput", "PyOps.ArraySet"] {
        assert_eq!(
            factory.runtime_count(member),
            0,
            "工厂不得提前执行恢复体：{member}"
        );
    }
    assert!(
        resume
            .header
            .ends_with("([System.Runtime]System.Object) cil managed")
    );
    assert_eq!(
        resume.runtime_count("PyOps.GeneratorInput"),
        resume.opcode_count("Return") - 1
    );
    assert!(
        resume.runtime_count("PyOps.ArrayGet") > 0 && resume.runtime_count("PyOps.ArraySet") > 0
    );
    (factory, resume)
}
pub fn check(output: &str, case: &str) {
    let cil = Cil::parse(output);
    let name = match case {
        "m3_2_send_cil" => "echo",
        "m3_2_prepared_cil" => "call",
        "m3_2_starred_call_cil" => "g",
        _ => "values",
    };
    let (_, resume) = pair(&cil, name);
    let (gets, sets, yields) = match case {
        "m3_2_basic_cil" => (20, 21, 1),
        "m3_2_send_cil" => (7, 8, 1),
        "m3_2_prepared_cil" => (10, 21, 2),
        "m3_2_starred_cil" => (14, 29, 2),
        "m3_2_starred_call_cil" => (12, 13, 1),
        _ => panic!("未登记生成器合同{case}"),
    };
    assert_eq!(resume.runtime_count("PyOps.ArrayGet"), gets);
    assert_eq!(resume.runtime_count("PyOps.ArraySet"), sets);
    assert_eq!(resume.runtime_count("PyOps.GeneratorInput"), yields);
    assert_eq!(resume.opcode_count("Return"), yields + 1);
    match case {
        "m3_2_basic_cil" => {
            assert_eq!(resume.opcode_count("BranchTruthy"), 2);
            assert_eq!(resume.runtime_count("PyCast.ToInt64"), 9);
        }
        "m3_2_send_cil" => assert_eq!(resume.opcode_count("BranchTruthy"), 1),
        "m3_2_prepared_cil" => {
            assert_eq!(resume.runtime_count("PyOps.PrepareMethod"), 1);
            assert_eq!(resume.runtime_count("PyPic.CallValue1"), 1);
            resume.ordered_calls(&[
                "PyOps.PrepareMethod",
                "PyOps.ArraySet",
                "PyOps.GeneratorInput",
                "PyPic.CallValue1",
            ]);
        }
        "m3_2_starred_cil" => {
            for member in [
                "PyOps.ListAppend",
                "PyOps.ListExtend",
                "PyOps.MakeSet",
                "PyOps.SetExtend",
                "PyOps.SetAdd",
            ] {
                assert_eq!(resume.runtime_count(member), 1);
            }
            resume.ordered_calls(&[
                "PyOps.MakeList",
                "PyOps.ListAppend",
                "PyOps.GeneratorInput",
                "PyOps.ListExtend",
                "PyOps.MakeSet",
                "PyOps.SetExtend",
                "PyOps.GeneratorInput",
                "PyOps.SetAdd",
            ]);
        }
        "m3_2_starred_call_cil" => {
            for member in ["PyOps.ListExtend", "PyOps.StarArg", "PyPic.CallValue"] {
                assert_eq!(resume.runtime_count(member), 1);
            }
            resume.ordered_calls(&[
                "PyOps.ListExtend",
                "PyOps.StarArg",
                "PyOps.GeneratorInput",
                "PyPic.CallValue",
            ]);
        }
        _ => unreachable!(),
    }
}
