//! 推导式各隐藏方法精确区分：循环、容器写入、共享cell与生成器惰性不得被符号重命名掩盖。
use crate::support::cil_contract::Cil;
pub fn check(output: &str, case: &str) {
    let cil = Cil::parse(output);
    if case == "m3_1_genexpr_cil" {
        let (_, resume) = crate::generator_cil_contract::pair(&cil, "<genexpr>");
        for member in [
            "PyOps.IterStart",
            "PyOps.IterNextOk",
            "PyOps.IterValue",
            "PyOps.Compare",
            "PyPic.Binary",
        ] {
            assert_eq!(resume.runtime_count(member), 1);
        }
        // 帧局部数可随正确None物化增长；typed MIR合同逐槽验证存取全覆盖。
        let restored = resume.runtime_count("PyOps.ArrayGet");
        assert!(restored >= 4);
        assert_eq!(resume.runtime_count("PyOps.ArraySet"), restored + 1);
        assert_eq!(resume.opcode_count("Return"), 2);
        resume.ordered_calls(&[
            "PyOps.IterStart",
            "PyOps.IterNextOk",
            "PyOps.IterValue",
            "PyOps.Compare",
            "PyPic.Binary",
            "PyOps.ArraySet",
        ]);
        return;
    }
    let lists = cil.methods_named("<listcomp>");
    assert_eq!(lists.len(), if case == "m3_1_list_cil" { 14 } else { 4 });
    for body in &lists {
        let loops = body.runtime_count("PyOps.IterStart");
        assert!((1..=2).contains(&loops));
        assert_eq!(body.runtime_count("PyOps.IterNextOk"), loops);
        assert_eq!(body.runtime_count("PyOps.IterValue"), loops);
        assert_eq!(body.runtime_count("PyOps.CaptureModuleTarget"), 1);
        assert_eq!(body.runtime_count("PyPic.CallCapturedMethod1"), 1);
        assert_eq!(body.opcode_count("Return"), 1);
        body.ordered_calls(&[
            "PyOps.MakeList",
            "PyOps.IterStart",
            "PyOps.IterValue",
            "PyOps.CaptureModuleTarget",
            "PyPic.CallCapturedMethod1",
        ]);
    }
    if case == "m3_1_list_cil" {
        assert_eq!(cil.method("build").runtime_count("PyOps.CellNew"), 1);
        assert_eq!(
            lists
                .iter()
                .map(|m| m.runtime_count("PyOps.CellNew"))
                .sum::<usize>(),
            3
        );
        assert_eq!(
            lists
                .iter()
                .map(|m| m.runtime_count("PyOps.CellSet"))
                .sum::<usize>(),
            3
        );
        assert_eq!(cil.methods_named("<lambda>").len(), 3);
        assert_eq!(
            cil.methods_named("<lambda>")
                .iter()
                .map(|m| m.runtime_count("PyOps.CellGetFree"))
                .sum::<usize>(),
            2
        );
    } else {
        assert_eq!(case, "m3_1_dict_set_cil");
        let dict = cil.method("<dictcomp>");
        dict.ordered_calls(&[
            "PyOps.MakeDict",
            "PyOps.IterStart",
            "PyOps.IterValue",
            "PyPic.Binary",
            "PyOps.SetItem",
        ]);
        assert_eq!(dict.runtime_count("PyOps.SetItem"), 1);
        let set = cil.method("<setcomp>");
        set.ordered_calls(&[
            "PyOps.MakeSet",
            "PyOps.IterStart",
            "PyOps.IterValue",
            "PyOps.CaptureModuleTarget",
            "PyPic.CallCapturedMethod1",
        ]);
        assert_eq!(set.runtime_count("PyPic.CallCapturedMethod1"), 1);
        assert_eq!(cil.method("<lambda>").runtime_count("PyOps.CellGetFree"), 1);
    }
}
