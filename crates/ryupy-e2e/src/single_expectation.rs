//! 单文件旁车的严格、无依赖解析；不能把任意失败或信号当预期异常。

pub(crate) fn parse(text: &str) -> Result<(String, String), String> {
    let mut exit = None;
    let mut exception = None;
    for raw in text.lines() {
        let line = raw.trim();
        if line.is_empty() || line.starts_with('#') {
            continue;
        }
        let (key, value) = line.split_once('=').ok_or("旁车缺少 =")?;
        let slot = match key.trim() {
            "expect_exit" => &mut exit,
            "expect_exception" => &mut exception,
            _ => return Err("未知旁车字段".into()),
        };
        if slot.replace(value.trim()).is_some() {
            return Err("重复旁车字段".into());
        }
    }
    if exit != Some("1") {
        return Err("旁车必须精确 expect_exit=1".into());
    }
    let (kind, message) = exception
        .and_then(|value| value.split_once(':'))
        .ok_or("旁车必须声明异常类型: 非空消息")?;
    let (kind, message) = (kind.trim(), message.trim());
    if kind.is_empty()
        || !kind
            .bytes()
            .enumerate()
            .all(|(i, b)| b == b'_' || b.is_ascii_alphabetic() || (i > 0 && b.is_ascii_digit()))
        || message.is_empty()
    {
        return Err("旁车异常类型或消息无效".into());
    }
    Ok((kind.into(), message.into()))
}

#[cfg(test)]
mod tests {
    use super::parse;

    #[test]
    fn strict_sidecar_contract() {
        let valid = "expect_exit = 1\nexpect_exception = ValueError: concrete message\n";
        assert_eq!(
            parse(valid).unwrap(),
            ("ValueError".into(), "concrete message".into())
        );
        for exit in ["0", "2", "-9", "nonzero", "01"] {
            assert!(parse(&valid.replace("= 1", &format!("= {exit}"))).is_err());
        }
        for invalid in [
            "",
            "expect_exit=1",
            "expect_exception=ValueError: message",
            "expect_exit=1\nexpect_exception=ValueError:",
            "expect_exit=1\nexpect_exception=9Error: message",
            "expect_exit=1\nexpect_exception=ValueError message",
        ] {
            assert!(parse(invalid).is_err(), "{invalid}");
        }
        for extra in [
            "expect_exit=1",
            "expect_exception=TypeError: other",
            "pending=true",
            "timeout=30",
            "broken",
        ] {
            assert!(parse(&format!("{valid}{extra}\n")).is_err());
        }
    }
}

// 从库入口验证发现错误不被 pending 吞掉，及精确退出码拒绝信号/其它失败。
#[cfg(test)]
mod integration_tests {
    use crate::{CaseSpec, check, discover_single_files};
    use std::process::{Command, Stdio};

    #[test]
    fn single_discovery_and_real_synthetic_exits() {
        let dir = std::env::temp_dir().join(format!("ryupy-single-sidecar-{}", std::process::id()));
        std::fs::create_dir_all(&dir).unwrap();
        let source = dir.join("sample.py");
        let sidecar = source.with_extension("case.txt");
        std::fs::write(&source, "# ryupy: pending\n# inert source\n").unwrap();
        let normal = CaseSpec::from_single_file(&dir, &source).unwrap();
        assert!(normal.pending);
        std::fs::write(
            &sidecar,
            "expect_exit=1\nexpect_exception=ValueError: concrete failure\n",
        )
        .unwrap();
        let spec = CaseSpec::from_single_file(&dir, &source).unwrap();
        assert!(spec.pending);
        for (command, accepted) in [
            ("printf 'ValueError: concrete failure\\n' >&2; exit 1", true),
            (
                "printf '  ValueError: concrete failure\\n' >&2; exit 1",
                false,
            ),
            ("exit 0", false),
            ("exit 2", false),
            ("kill -TERM $$", false),
        ] {
            let output = Command::new("/bin/sh")
                .args(["-c", command])
                .stdin(Stdio::null())
                .output()
                .unwrap();
            assert_eq!(check(&spec.expected, &output).is_ok(), accepted);
            if accepted {
                assert!(check(&normal.expected, &output).is_err());
            }
        }
        std::fs::write(&sidecar, "expect_exit=1\nunknown=true\n").unwrap();
        assert!(discover_single_files(&dir, &mut Vec::new()).is_err());
        std::fs::remove_dir_all(&dir).unwrap();
    }
}
