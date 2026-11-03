//! AOT 失败生命周期：通过真实 CLI 子进程注入假 SDK，不修改测试进程环境。
//! 假 SDK 只模拟失败/缺产物，绝不把假文件的成功当作 NativeAOT 验收。
#![cfg(all(target_os = "linux", target_arch = "x86_64", target_env = "gnu"))]

use std::fs;
use std::os::unix::fs::PermissionsExt;
use std::path::{Path, PathBuf};
use std::process::Command;
use std::sync::atomic::{AtomicU64, Ordering};

static NEXT: AtomicU64 = AtomicU64::new(0);

fn cli() -> PathBuf {
    // 此文件归 ryupy-cli/tests；Cargo 集成测试保证同轮真实 CLI 已构建。
    PathBuf::from(env!("CARGO_BIN_EXE_ryupy"))
}

fn verify_failure(code: i32, default_output: bool) {
    // create_dir 独占创建；断言失败保留本轮证据，成功仅清理自己创建的根。
    let root = std::env::temp_dir().join(format!(
        "ryupy-aot-failure-{}-{}",
        std::process::id(),
        NEXT.fetch_add(1, Ordering::Relaxed)
    ));
    fs::create_dir(&root).unwrap();
    let source_dir = root.join("源码 & with spaces");
    fs::create_dir(&source_dir).unwrap();
    let out = if default_output {
        source_dir.clone()
    } else {
        root.join("输出 & with spaces")
    };
    fs::create_dir_all(&out).unwrap();
    let old = out.join("ryupy-aot-old");
    fs::create_dir(&old).unwrap();
    fs::create_dir(old.join("publish")).unwrap();
    let sentinel = old.join("publish/OldNative");
    fs::write(&sentinel, b"old-native-must-survive\0").unwrap();
    fs::write(out.join("unrelated.txt"), b"user-owned-sentinel").unwrap();
    let input = source_dir.join("sample_alias.py");
    fs::write(&input, "print('compile only')\n").unwrap();
    let fake = root.join("fake-sdk.sh");
    fs::write(
        &fake,
        format!(
            r#"#!/bin/sh
printf '%s\n' "$@" > "$AOT_ARGS"
# 保留即将被失败清理的项目，用于锁实际静态引用及 XML 转义。
cat "$2" > "$AOT_PROJECT"
printf 'sdk-stdout-marker\n'
printf 'sdk-stderr-marker\n' >&2
exit {code}
"#
        ),
    )
    .unwrap();
    fs::set_permissions(&fake, fs::Permissions::from_mode(0o700)).unwrap();
    let mut command = Command::new(cli());
    if default_output {
        // 复现用户无-o调用，并覆盖input.parent为空时应以当前源目录作为ROOT。
        command
            .current_dir(&source_dir)
            .args(["build", "sample_alias.py", "--aot"]);
    } else {
        command.args([
            "build",
            input.to_str().unwrap(),
            "--out-dir",
            out.to_str().unwrap(),
            "--aot",
        ]);
    }
    let output = command
        .env("DOTNET", &fake)
        .env("AOT_ARGS", root.join("args.txt"))
        .env("AOT_PROJECT", root.join("project.xml"))
        .output()
        .unwrap();
    fs::write(root.join("cli.stdout"), &output.stdout).unwrap();
    fs::write(root.join("cli.stderr"), &output.stderr).unwrap();
    assert!(!output.status.success(), "不能以旧产物或 SDK 退出0冒充成功");
    let stderr = String::from_utf8(output.stderr).unwrap();
    assert!(
        !stderr.lines().any(|line| line.starts_with("已生成 ")),
        "{stderr}"
    );
    let stdout = String::from_utf8(output.stdout).unwrap();
    assert!(
        !stdout.lines().any(|line| line.starts_with("已生成 ")),
        "{stdout}"
    );
    assert!(stderr.contains("sdk-stdout-marker"), "{stderr}");
    assert!(stderr.contains("sdk-stderr-marker"), "{stderr}");
    assert!(stderr.contains(&format!("exit status: {code}")), "{stderr}");
    assert_eq!(fs::read(&sentinel).unwrap(), b"old-native-must-survive\0");
    assert_eq!(
        fs::read(out.join("unrelated.txt")).unwrap(),
        b"user-owned-sentinel"
    );
    let roots: Vec<_> = fs::read_dir(&out)
        .unwrap()
        .map(|entry| entry.unwrap().file_name())
        .filter(|name| name.to_string_lossy().starts_with("ryupy-aot-"))
        .collect();
    assert_eq!(roots, vec![std::ffi::OsString::from("ryupy-aot-old")]);
    // 编译真实程序集保留输入stem；发布失败不等于把JIT输出宣称native。
    assert!(!out.join("sample_alias.dll").exists());
    let build = out.join("sample_alias.build");
    assert!(build.join("ryupy-build-owner.json").is_file());
    let attempts: Vec<_> = fs::read_dir(&build)
        .unwrap()
        .map(|e| e.unwrap().path())
        .filter(|p| {
            p.file_name()
                .unwrap()
                .to_string_lossy()
                .starts_with("attempt-")
        })
        .collect();
    assert_eq!(attempts.len(), 1);
    assert!(attempts[0].join("sample_alias.dll").is_file());
    assert!(attempts[0].join("build-error.log").is_file());
    let sdk = attempts[0].join("ryupy-aot-0");
    assert!(sdk.join("host/Host.csproj").is_file());
    assert!(
        fs::read_to_string(sdk.join("publish.stdout.log"))
            .unwrap()
            .contains("sdk-stdout-marker")
    );
    assert!(
        fs::read_to_string(sdk.join("publish.stderr.log"))
            .unwrap()
            .contains("sdk-stderr-marker")
    );
    assert!(!out.join("sample_alias.dist").exists());
    assert!(
        !source_dir.join("__pycache__").exists(),
        "AOT默认布局不得写JIT缓存"
    );
    assert!(!out.join("sample_alias.build.lock").exists());
    let args = fs::read_to_string(root.join("args.txt")).unwrap();
    let args: Vec<_> = args.lines().collect();
    assert_eq!(args.first(), Some(&"publish"));
    assert!(args.windows(2).any(|pair| pair == ["-r", "linux-x64"]));
    assert!(
        args.windows(2)
            .any(|pair| pair == ["--self-contained", "true"])
    );
    assert!(args.contains(&"-p:ImportDirectoryBuildProps=false"));
    assert!(args.contains(&"-p:ImportDirectoryBuildTargets=false"));
    let project = fs::read_to_string(root.join("project.xml")).unwrap();
    for required in [
        "<Reference Include=\"sample_alias\">",
        "&amp;",
        "<PublishAot>true",
        "<IlcTreatWarningsAsErrors>true",
        "<ILLinkTreatWarningsAsErrors>true",
    ] {
        assert!(project.contains(required), "缺少 {required}：{project}");
    }
    assert!(!project.contains("TrimmerRootAssembly"));
    cleanup_owned(&root);
}

fn cleanup_owned(root: &Path) {
    fs::remove_dir_all(root).unwrap();
}

#[test]
fn failed_sdk_keeps_old_native_and_reports_both_streams() {
    verify_failure(23, false);
}

#[test]
fn successful_sdk_without_new_native_cannot_reuse_old_artifact() {
    verify_failure(0, false);
}

#[test]
fn default_aot_without_out_dir_uses_source_build_not_jit_cache() {
    verify_failure(23, true);
}
