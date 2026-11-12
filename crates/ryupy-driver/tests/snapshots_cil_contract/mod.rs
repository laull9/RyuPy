//! 被内部函数符号改动影响的旧CIL黄金改为手写语义结构合同；HIR/MIR/types及运行门保留。
//! 按唯一core检查静态ABI、真实运算、调用与EH，不保存完整生成文本或猜测函数编号。
use crate::support::cil_contract::{Cil, Method};

fn calls(method: &Method<'_>, expected: &[(&str, usize)]) {
    for &(member, count) in expected {
        assert_eq!(
            method.runtime_count(member),
            count,
            "{}: {member}",
            method.name
        );
    }
}
fn ops(method: &Method<'_>, expected: &[(&str, usize)]) {
    for &(opcode, count) in expected {
        assert_eq!(
            method.opcode_count(opcode),
            count,
            "{}: {opcode}",
            method.name
        );
    }
}
fn signature(method: &Method<'_>, result: &str, args: &str) {
    assert_eq!(
        method.header,
        format!("public static {result} {}({args}) cil managed", method.name)
    );
}
fn numeric(method: &Method<'_>, args: &str) {
    signature(method, "long", args);
    ops(
        method,
        &[
            ("Box", 0),
            ("Unbox", 0),
            ("NewArray", 0),
            ("NewObject", 0),
            ("Return", 1),
        ],
    );
    calls(
        method,
        &[
            ("PyPic.Binary", 0),
            ("PyOps.Binary", 0),
            ("PyTraceback.PushFrameWithOrigin", 1),
            ("PyTraceback.PopTo", 1),
        ],
    );
}

// EH边界必须指向本方法实际IL，catch仅允许本竖切发出的Python异常类型。
fn handlers(method: &Method<'_>, expected: &[(&str, usize)]) {
    assert_eq!(
        method.eh.len(),
        expected.iter().map(|(_, count)| count).sum::<usize>()
    );
    let mut actual = std::collections::BTreeMap::new();
    for line in &method.eh {
        let rest = line.strip_prefix("// EH: try [").unwrap();
        let (range, rest) = rest.split_once(") catch ").unwrap();
        let (kind, handler) = rest.split_once(" handler [").unwrap();
        *actual.entry(kind).or_insert(0usize) += 1;
        for range in [range, handler.strip_suffix(')').unwrap()] {
            let (start, end) = range.split_once("..").unwrap();
            let start: usize = start.parse().unwrap();
            let end: usize = end.parse().unwrap();
            assert!(start < end && end <= method.instructions.len());
        }
    }
    for &(name, count) in expected {
        let full = format!("[RyuPy.Runtime]RyuPy.Runtime.{name}");
        assert_eq!(actual.remove(full.as_str()), Some(count));
    }
    assert!(actual.is_empty());
}

pub(super) fn check(kind: &str, text: &str) {
    let cil = Cil::parse(text);
    let main = cil.exact("m0___module_main");
    match kind {
        "m1_1" => {
            let calc = cil.method("main_calc");
            let helper = cil.method("helper");
            numeric(calc, "long");
            numeric(helper, "long");
            ops(calc, &[("MultiplyOverflow", 1), ("AddOverflow", 1)]);
            ops(helper, &[("SubtractOverflow", 1)]);
            assert_eq!(calc.generated_call_count(helper), 1);
            assert_eq!(main.generated_call_count(calc), 1);
            assert_eq!(main.generated_call_count(helper), 1);
            calls(
                main,
                &[("Builtins.WriteI64", 2), ("Builtins.WriteNewline", 2)],
            );
        }
        "m1_2_control_flow" => {
            let classify = cil.method("classify");
            let collatz = cil.method("collatz_steps");
            let multiple = cil.method("first_multiple");
            numeric(classify, "long");
            numeric(collatz, "long");
            numeric(multiple, "long, long");
            ops(
                classify,
                &[
                    ("CompareLess", 1),
                    ("CompareEqual", 1),
                    ("BranchTruthy", 2),
                    ("Branch", 5),
                ],
            );
            ops(
                collatz,
                &[
                    ("BranchTruthy", 2),
                    ("Branch", 7),
                    ("MultiplyOverflow", 1),
                    ("AddOverflow", 2),
                    ("StoreArgument", 2),
                ],
            );
            calls(collatz, &[("PyMath.ModI64", 1), ("PyMath.FloorDivI64", 1)]);
            ops(
                multiple,
                &[("CompareGreater", 1), ("BranchTruthy", 2), ("Branch", 7)],
            );
            calls(multiple, &[("PyMath.ModI64", 1)]);
            ops(main, &[("BranchTruthy", 2)]);
            calls(main, &[("Builtins.WriteBool", 2), ("Builtins.WriteI64", 6)]);
        }
        "m1_3_fib" => {
            let recursive = cil.method("fib_rec");
            let iterative = cil.method("fib_iter");
            numeric(recursive, "long");
            numeric(iterative, "long");
            assert_eq!(recursive.generated_call_count(recursive), 2);
            ops(
                recursive,
                &[
                    ("SubtractOverflow", 2),
                    ("AddOverflow", 1),
                    ("CompareLess", 1),
                    ("BranchTruthy", 1),
                ],
            );
            ops(
                iterative,
                &[
                    ("CompareLess", 1),
                    ("BranchTruthy", 1),
                    ("Branch", 5),
                    ("AddOverflow", 2),
                ],
            );
            assert_eq!(iterative.generated_call_count(recursive), 0);
            assert_eq!(main.generated_call_count(recursive), 1);
            assert_eq!(main.generated_call_count(iterative), 1);
        }
        "m1_4_mandelbrot" => {
            let mandel = cil.method("mandel");
            numeric(mandel, "double, double, long");
            ops(
                mandel,
                &[
                    ("Multiply", 4),
                    ("Add", 3),
                    ("Subtract", 1),
                    ("CompareGreater", 1),
                    ("CompareLess", 1),
                    ("BranchTruthy", 2),
                    ("AddOverflow", 1),
                ],
            );
            calls(
                main,
                &[
                    ("PyMath.TrueDivF64", 2),
                    ("Builtins.StrConcat", 2),
                    ("Builtins.WriteStr", 1),
                ],
            );
            assert_eq!(main.generated_call_count(mandel), 1);
        }
        "m1_5_dataclass" => {
            calls(
                main,
                &[
                    ("PyOps.MakeClass", 2),
                    ("PyOps.MakeDict", 2),
                    ("PyOps.SetItem", 5),
                    ("PyOps.FinalizeOrdinaryClass", 2),
                    ("PyOps.DeclareDataclass", 2),
                    ("PyOps.ApplyDataclass", 2),
                ],
            );
            main.ordered_calls(&[
                "PyOps.MakeClass",
                "PyOps.FinalizeOrdinaryClass",
                "PyOps.DeclareDataclass",
                "PyOps.ApplyDataclass",
                "PyOps.MakeClass",
                "PyOps.FinalizeOrdinaryClass",
                "PyOps.DeclareDataclass",
                "PyOps.ApplyDataclass",
            ]);
            calls(
                cil.method("step"),
                &[
                    ("PyPic.GetAttr", 2),
                    ("PyPic.Binary", 2),
                    ("PyPic.SetAttr", 1),
                ],
            );
            calls(
                cil.method("dot"),
                &[("PyPic.GetAttr", 4), ("PyPic.Binary", 3)],
            );
        }
        "m2_1_dynamic" => {
            let twice = cil.method("twice");
            let choose = cil.method("choose");
            signature(
                twice,
                "[System.Runtime]System.Object",
                "[System.Runtime]System.Object, [System.Runtime]System.Object",
            );
            calls(twice, &[("PyPic.Binary", 1)]);
            ops(twice, &[("AddOverflow", 0)]);
            calls(choose, &[("PyOps.Truthy", 1)]);
            ops(choose, &[("BranchTruthy", 1), ("Branch", 3)]);
            calls(
                main,
                &[
                    ("Builtins.WriteObject", 9),
                    ("PyPic.Binary", 2),
                    ("PyOps.Truthy", 2),
                ],
            );
        }
        "m2_3_containers" => {
            let mix = cil.method("mix");
            calls(
                mix,
                &[
                    ("PyOps.MakeList", 1),
                    ("PyOps.MakeDict", 1),
                    ("PyOps.SetItem", 3),
                    ("PyOps.IterStart", 1),
                    ("PyOps.IterNextOk", 1),
                    ("PyOps.IterValue", 1),
                    ("PyOps.Contains", 1),
                ],
            );
            mix.ordered_calls(&[
                "PyOps.MakeList",
                "PyOps.SetItem",
                "PyOps.MakeDict",
                "PyOps.IterStart",
                "PyOps.IterNextOk",
                "PyOps.IterValue",
            ]);
            ops(
                mix,
                &[("NewArray", 2), ("StoreElement", 3), ("BranchTruthy", 1)],
            );
        }
        "m2_5_classes" => {
            calls(
                main,
                &[
                    ("PyOps.MakeClass", 1),
                    ("PyOps.Decorate", 3),
                    ("PyOps.FinalizeOrdinaryClass", 1),
                    ("PyPic.SetAttr", 7),
                    ("PyOps.DelAttr", 1),
                    ("PyPic.CallCapturedMethod0", 2),
                ],
            );
            main.ordered_calls(&[
                "PyOps.MakeClass",
                "PyOps.Decorate",
                "PyOps.Decorate",
                "PyOps.Decorate",
                "PyOps.FinalizeOrdinaryClass",
            ]);
            calls(cil.method("__init__"), &[("PyPic.SetAttr", 1)]);
            calls(
                cil.method("add"),
                &[
                    ("PyPic.GetAttr", 2),
                    ("PyPic.Binary", 1),
                    ("PyPic.SetAttr", 1),
                ],
            );
            calls(
                cil.method("doubled"),
                &[("PyPic.GetAttr", 1), ("PyPic.Binary", 1)],
            );
            calls(cil.method("named"), &[("PyPic.GetAttr", 1)]);
            ops(cil.method("kind"), &[("LoadString", 3), ("Return", 1)]);
        }
        "m2_9_pic" => {
            calls(cil.method("add"), &[("PyPic.Binary", 1)]);
            calls(cil.method("call"), &[("PyPic.CallValue0", 1)]);
            for name in ["bump", "replacement"] {
                calls(
                    cil.method(name),
                    &[
                        ("PyPic.GetAttr", 2),
                        ("PyPic.Binary", 1),
                        ("PyPic.SetAttr", 1),
                    ],
                );
            }
            calls(
                main,
                &[
                    ("PyModules.Import", 1),
                    ("PyOps.CaptureModuleTarget", 3),
                    ("PyOps.IsModule", 3),
                    ("PyPic.CallCapturedMethod0", 3),
                    ("PyPic.SetAttr", 5),
                    ("PyOps.DelAttr", 1),
                ],
            );
            main.ordered_calls(&[
                "PyOps.IsModule",
                "PyOps.CaptureModuleTarget",
                "PyPic.CallCapturedMethod0",
            ]);
            assert_eq!(cil.runtime_count("PyOps.CaptureModuleMethod"), 0);
        }
        "m2_2_try" => {
            let returning = cil.method("returning");
            calls(
                returning,
                &[("PyErrors.EnterHandler", 4), ("PyErrors.LeaveHandler", 5)],
            );
            ops(returning, &[("Leave", 4), ("Throw", 2), ("Return", 1)]);
            handlers(returning, &[("PyBaseException", 2)]);
            calls(
                main,
                &[
                    ("PyErrors.EnterHandler", 18),
                    ("PyErrors.LeaveHandler", 21),
                    ("PyErrors.PrepareRaise", 2),
                ],
            );
            handlers(
                main,
                &[
                    ("PyBaseException", 8),
                    ("PyRuntimeError", 1),
                    ("PyValueError", 1),
                    ("PyUnsupportedOperation", 1),
                ],
            );
        }
        "m2_8_with" => {
            let early = cil.method("early_return");
            calls(
                early,
                &[
                    ("PyOps.CaptureModuleTarget", 1),
                    ("PyPic.CallCapturedMethod0", 1),
                    ("PyPic.CallValue3", 2),
                    ("PyErrors.Reraise", 1),
                    ("PyErrors.EnterHandler", 7),
                    ("PyErrors.LeaveHandler", 8),
                ],
            );
            early.ordered_calls(&[
                "PyOps.CaptureModuleTarget",
                "PyPic.CallCapturedMethod0",
                "PyPic.CallValue3",
            ]);
            ops(early, &[("Leave", 9), ("Throw", 4), ("Return", 1)]);
            handlers(early, &[("PyBaseException", 4)]);
            assert_eq!(cil.methods_named("__enter__").len(), 2);
            assert_eq!(cil.methods_named("__exit__").len(), 2);
            calls(
                main,
                &[
                    ("PyOps.MakeClass", 2),
                    ("PyOps.FinalizeOrdinaryClass", 2),
                    ("PyPic.CallCapturedMethod0", 6),
                    ("PyPic.CallValue3", 12),
                    ("PyErrors.Reraise", 6),
                ],
            );
            ops(main, &[("Leave", 80), ("Throw", 27)]);
            handlers(
                main,
                &[
                    ("PyBaseException", 26),
                    ("PyRuntimeError", 1),
                    ("PyValueError", 1),
                    ("PyUnsupportedOperation", 1),
                ],
            );
        }
        _ => panic!("未定义迁移合同: {kind}"),
    }
}

/// 在同一真实CIL副本上破坏feature关键指令，证明迁移没有退成只检查可解析文本。
pub(super) fn check_and_reject_mutation(kind: &str, text: &str) {
    check(kind, text);
    let (from, to) = match kind {
        "m1_1" | "m1_2_control_flow" => ("MultiplyOverflow(Signed)", "Multiply"),
        "m1_3_fib" => ("SubtractOverflow(Signed)", "Subtract"),
        "m1_4_mandelbrot" => ("Multiply", "Divide"),
        "m1_5_dataclass" => ("PyOps.ApplyDataclass(", "PyOps.MissingDataclass("),
        "m2_1_dynamic" | "m2_9_pic" => ("PyPic.Binary(", "PyOps.Binary("),
        "m2_3_containers" => ("PyOps.MakeList(", "PyOps.MakeTuple("),
        "m2_5_classes" => ("PyOps.Decorate(", "PyOps.MissingDecorator("),
        "m2_2_try" => ("PyErrors.EnterHandler(", "PyErrors.MissingHandler("),
        "m2_8_with" => ("PyPic.CallValue3(", "PyPic.CallValue2("),
        _ => panic!("缺失feature负例"),
    };
    let changed = text.replacen(from, to, 1);
    assert_ne!(changed, text, "负例必须真正修改指令");
    assert!(
        std::panic::catch_unwind(|| check(kind, &changed)).is_err(),
        "关键指令变异未被结构合同拒绝"
    );
}
