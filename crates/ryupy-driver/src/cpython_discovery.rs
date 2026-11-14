//! CPython 解释器环境自动探测与发行适配：
//! 在构建期优先探测当前项目 venv、激活的虚拟环境、PATH 系统 Python 或 Windows py 启动器，
//! 全面核验 implementation、ABI、架构、GIL 模式与版本上限，
//! 并准备可冻结、可搬迁的独立基础发行与来源许可证。

use super::native::version::PythonVersion;
use anyhow::{Context, Result, bail, ensure};
use serde::{Deserialize, Serialize};
use std::path::{Path, PathBuf};
use std::process::{Command, Stdio};

/// 在系统 PATH 中查找指定可执行程序。
fn find_in_path(cmd: &str) -> Option<PathBuf> {
    if let Some(path_var) = std::env::var_os("PATH") {
        for dir in std::env::split_paths(&path_var) {
            let candidate = dir.join(cmd);
            if candidate.is_file() {
                return Some(candidate);
            }
            if cfg!(windows) {
                let exe = dir.join(format!("{cmd}.exe"));
                if exe.is_file() {
                    return Some(exe);
                }
            }
        }
    }
    None
}

/// 自动探测到的 Python 解释器详细环境描述。
#[derive(Debug, Clone, Serialize, Deserialize)]
pub(crate) struct DiscoveredPython {
    pub version: PythonVersion,
    pub executable: PathBuf,
    pub prefix: PathBuf,
    pub base_prefix: PathBuf,
    pub is_venv: bool,
    pub distribution_root: PathBuf,
    pub source_project: String,
    pub source_build_id: String,
    pub source_url: String,
    pub licenses: Vec<String>,
    pub builtin_modules: Vec<String>,
    pub soabi: String,
    pub includepy: PathBuf,
    pub libdir: PathBuf,
}

/// 探针执行返回的原生 JSON 结果。
#[derive(Deserialize)]
struct ProbeOutput {
    major: u32,
    minor: u32,
    micro: u32,
    releaselevel: String,
    version_str: String,
    implementation: String,
    is_64bit: bool,
    machine: String,
    gil_disabled: bool,
    soabi: String,
    is_debug: bool,
    #[serde(rename = "prefix")]
    _prefix: String,
    base_prefix: String,
    #[serde(rename = "executable")]
    _executable: String,
    #[serde(default)]
    includepy: String,
    #[serde(default)]
    libdir: String,
    #[serde(default)]
    #[serde(rename = "ldlibrary")]
    _ldlibrary: String,
    #[serde(default)]
    builtin_modules: Vec<String>,
}

/// 沿原始选中可执行文件的父级目录（bin 或 Scripts）检测 pyvenv.cfg，以准确识别虚拟环境根。
fn detect_venv_root(exe: &Path) -> Result<Option<PathBuf>> {
    let Some(parent) = exe.parent() else {
        return Ok(None);
    };
    let Some(name) = parent.file_name().and_then(|n| n.to_str()) else {
        return Ok(None);
    };
    if name == "bin" || name == "Scripts" {
        let venv_dir = parent.parent().context("虚拟环境解释器缺少环境根")?;
        let cfg = venv_dir.join("pyvenv.cfg");
        if cfg.is_file() {
            let text = std::fs::read_to_string(&cfg).context("无法读取虚拟环境 pyvenv.cfg")?;
            let home = text
                .lines()
                .filter_map(|line| line.split_once('='))
                .find(|(key, _)| key.trim().eq_ignore_ascii_case("home"))
                .map(|(_, value)| value.trim())
                .context("虚拟环境 pyvenv.cfg 缺少 home")?;
            ensure!(
                !home.is_empty() && Path::new(home).is_dir(),
                "虚拟环境 pyvenv.cfg 的 home 不存在：{home}"
            );
            return Ok(Some(venv_dir.to_path_buf()));
        }
    }
    Ok(None)
}

/// 构建期优先探测实际 CPython 解释器。
pub(crate) fn discover(entry: &Path) -> Result<DiscoveredPython> {
    // 优先级 1：当前项目目录及各级祖先目录下的虚拟环境（若存在则必须探测成功，禁止静默掩盖）
    let entry = entry.canonicalize().context("Python 自动探测入口不存在")?;
    if let Some(exe) = find_project_venv(&entry)? {
        let info = probe_executable(&exe)
            .with_context(|| format!("当前项目虚拟环境 ({}) 探测失败", exe.display()))?;
        return resolve_distribution(&exe, info);
    }

    // 优先级 2：当前激活的虚拟环境（VIRTUAL_ENV 环境变量，若已激活则必须探测成功）
    if let Ok(venv_str) = std::env::var("VIRTUAL_ENV") {
        let venv_path = PathBuf::from(venv_str);
        let candidate = if cfg!(windows) {
            venv_path.join("Scripts/python.exe")
        } else {
            venv_path.join("bin/python")
        };
        if candidate.is_file() {
            let info = probe_executable(&candidate).with_context(|| {
                format!(
                    "激活的虚拟环境 VIRTUAL_ENV ({}) 探测失败",
                    candidate.display()
                )
            })?;
            return resolve_distribution(&candidate, info);
        }
        bail!("激活环境缺少解释器：{}", candidate.display());
    }

    // 优先级 3：PATH 系统 Python（找到解释器但不支持时直接报错诊断，不存在则继续候选）
    for candidate_name in &["python3", "python"] {
        if let Some(path) = find_in_path(candidate_name) {
            let info = probe_executable(&path).with_context(|| {
                format!(
                    "系统 PATH 中的解释器 ({}) 探测失败或不受支持",
                    path.display()
                )
            })?;
            return resolve_distribution(&path, info);
        }
    }

    if cfg!(windows) {
        if let Some(py_launcher) = find_in_path("py") {
            // Windows 下尝试通过 py -3 探测
            let output = Command::new(&py_launcher)
                .args([
                    "-3",
                    "-I",
                    "-X",
                    "utf8",
                    "-S",
                    "-B",
                    "-c",
                    "import sys; print(sys.executable)",
                ])
                .output()
                .with_context(|| {
                    format!("执行 Windows py 启动器失败：{}", py_launcher.display())
                })?;
            ensure!(
                output.status.success(),
                "Windows py 启动器获取 Python 路径失败：{}",
                String::from_utf8_lossy(&output.stderr)
            );
            let exe_str = String::from_utf8_lossy(&output.stdout).trim().to_string();
            let exe_path = PathBuf::from(exe_str);
            if exe_path.is_file() {
                let info = probe_executable(&exe_path).with_context(|| {
                    format!("Windows py 选中的解释器 ({}) 探测失败", exe_path.display())
                })?;
                return resolve_distribution(&exe_path, info);
            }
        }
    }

    bail!(
        "构建期未探测到可用的 CPython 3.9+ 解释器环境，请在项目创建 venv 或确保 PATH 中存在 python3"
    );
}

/// 在项目入口目录及其父目录中查找 .venv 或 venv 目录。
fn find_project_venv(entry: &Path) -> Result<Option<PathBuf>> {
    let mut current = if entry.is_file() {
        entry.parent().unwrap_or(entry).to_path_buf()
    } else {
        entry.to_path_buf()
    };

    loop {
        for name in &[".venv", "venv", "env"] {
            let venv_dir = current.join(name);
            let candidate = if cfg!(windows) {
                venv_dir.join("Scripts/python.exe")
            } else {
                venv_dir.join("bin/python3")
            };
            if candidate.is_file() {
                return Ok(Some(candidate));
            }
            let candidate_fallback = if cfg!(windows) {
                venv_dir.join("Scripts/python.exe")
            } else {
                venv_dir.join("bin/python")
            };
            if candidate_fallback.is_file() {
                return Ok(Some(candidate_fallback));
            }
            ensure!(
                !venv_dir.join("pyvenv.cfg").exists(),
                "项目虚拟环境缺少有效解释器：{}",
                venv_dir.display()
            );
        }
        if !current.pop() {
            break;
        }
    }
    Ok(None)
}

/// 执行探测探针，提取解释器的版本、实现、GIL 模式与路径信息。
fn probe_executable(exe: &Path) -> Result<ProbeOutput> {
    let script = r#"
import sys, json, sysconfig, platform
inc = sysconfig.get_config_var("INCLUDEPY")
libdir = sysconfig.get_config_var("LIBDIR")
ldlib = sysconfig.get_config_var("LDLIBRARY")
res = {
    "major": sys.version_info.major,
    "minor": sys.version_info.minor,
    "micro": sys.version_info.micro,
    "releaselevel": sys.version_info.releaselevel,
    "version_str": sys.version.split()[0],
    "implementation": getattr(sys, "implementation", None).name if hasattr(sys, "implementation") else "cpython",
    "is_64bit": sys.maxsize > 2**32,
    "machine": platform.machine().lower(),
    "gil_disabled": bool(sysconfig.get_config_var("Py_GIL_DISABLED")),
    "soabi": sysconfig.get_config_var("SOABI") or "",
    "is_debug": bool(sysconfig.get_config_var("Py_DEBUG") or hasattr(sys, "gettotalrefcount")),
    "prefix": sys.prefix,
    "base_prefix": getattr(sys, "base_prefix", sys.prefix),
    "executable": sys.executable,
    "includepy": inc if inc else "",
    "libdir": libdir if libdir else "",
    "ldlibrary": ldlib if ldlib else "",
    "builtin_modules": list(sys.builtin_module_names),
}
print(json.dumps(res))
"#;

    let output = Command::new(exe)
        .args(["-I", "-X", "utf8", "-S", "-B", "-c", script])
        .stdin(Stdio::null())
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .output()
        .with_context(|| format!("无法执行探测解释器：{}", exe.display()))?;

    ensure!(
        output.status.success(),
        "Python 探针执行失败：{}",
        String::from_utf8_lossy(&output.stderr)
    );

    let probe: ProbeOutput =
        serde_json::from_slice(&output.stdout).context("Python 探针输出解析失败")?;

    ensure!(
        probe.implementation == "cpython",
        "RyuPy 仅支持标准 CPython 解释器，探测到不支持的实现：{}",
        probe.implementation
    );
    ensure!(
        probe.is_64bit,
        "RyuPy CPython provider 仅支持 64 位架构目标，拒绝 32 位解释器"
    );
    let machine = match std::env::consts::ARCH {
        "aarch64" => ["aarch64", "arm64"],
        _ => ["x86_64", "amd64"],
    };
    ensure!(
        machine.contains(&probe.machine.as_str()),
        "解释器机器架构与当前目标不符：{}",
        probe.machine
    );
    ensure!(
        !probe.gil_disabled,
        "暂不支持 free-threaded (Py_GIL_DISABLED=1) CPython 构建，请使用常规 GIL 解释器"
    );
    ensure!(
        !probe.soabi.split('-').nth(1).unwrap_or("").ends_with('t'),
        "暂不支持 free-threaded (SOABI: {}) CPython 构建，请使用常规 GIL 解释器",
        probe.soabi
    );
    ensure!(
        !probe.is_debug,
        "暂不支持 debug (Py_DEBUG=1) CPython 构建，请使用常规发布版解释器"
    );
    ensure!(
        probe.releaselevel == "final",
        "拒绝预发布 CPython 解释器：{}，RyuPy 要求纯正正式发布版",
        probe.version_str
    );

    // 严格解析并校验版本字符串，杜绝带 '+' 或非法后缀
    let version = PythonVersion::parse(&probe.version_str)?;
    ensure!(
        version.major == probe.major
            && version.minor == probe.minor
            && version.micro == probe.micro,
        "探针版本一致性校验失败：{} vs {}.{}.{}",
        probe.version_str,
        probe.major,
        probe.minor,
        probe.micro
    );
    version.validate_supported()?;

    Ok(probe)
}

/// 基于探测信息准备可冻结、可搬迁的基础发行与来源/许可。
fn resolve_distribution(exe: &Path, probe: ProbeOutput) -> Result<DiscoveredPython> {
    let version = PythonVersion::new(probe.major, probe.minor, probe.micro);
    let base_prefix = PathBuf::from(&probe.base_prefix).canonicalize()?;
    // 若原始选中的 exe 属于带有 pyvenv.cfg 的虚拟环境，精准记录 venv 根，不被 -S 影响
    let (prefix, is_venv) = if let Some(venv_root) = detect_venv_root(exe)? {
        (venv_root.canonicalize()?, true)
    } else {
        (base_prefix.clone(), false)
    };
    // 保留选中的原始 exe，绝不 canonicalize 导致跳出虚拟环境
    let executable = exe.to_path_buf();

    let soabi = probe.soabi.clone();
    let includepy = if !probe.includepy.is_empty() {
        PathBuf::from(&probe.includepy)
    } else {
        base_prefix.join("include")
    };
    let libdir = if !probe.libdir.is_empty() {
        PathBuf::from(&probe.libdir)
    } else {
        base_prefix.join("lib")
    };

    let request = serde_json::json!({
        "version": version.version_str(), "local": base_prefix,
        "triple": super::native::target_triple()?, "options": super::native::build_options()?,
    });
    let mut child = Command::new(exe)
        .args([
            "-I",
            "-X",
            "utf8",
            "-S",
            "-B",
            "-c",
            include_str!("cpython_portable.py"),
        ])
        .stdin(Stdio::piped())
        .stdout(Stdio::piped())
        .stderr(Stdio::piped())
        .spawn()?;
    use std::io::Write;
    child
        .stdin
        .take()
        .context("缺少发行准备输入管道")?
        .write_all(&serde_json::to_vec(&request)?)?;
    let output = child.wait_with_output()?;
    ensure!(
        output.status.success(),
        "{}",
        String::from_utf8_lossy(&output.stderr)
    );
    let prepared: serde_json::Value = serde_json::from_slice(&output.stdout)?;
    let root = PathBuf::from(prepared["root"].as_str().context("缺少发行根")?).canonicalize()?;
    let metadata: serde_json::Value =
        serde_json::from_slice(&std::fs::read(root.join("PYTHON.json"))?)?;
    // v7 的同一事实名为 optimizations；保留原始官方JSON，按已审查schema读取。
    let build_options = match metadata["version"].as_str() {
        Some("7") => &metadata["optimizations"],
        Some("8") => &metadata["build_options"],
        _ => bail!("不支持的官方 PBS 元数据schema：{}", metadata["version"]),
    };
    ensure!(
        metadata["python_version"] == version.version_str()
            && metadata["target_triple"] == super::native::target_triple()?
            && build_options == super::native::build_options()?
            && metadata["libpython_link_mode"] == "shared",
        "官方基础发行元数据与选中版本/ABI不一致"
    );
    let portable_exe = if cfg!(windows) {
        root.join("python.exe")
    } else {
        root.join(format!("bin/python{}", version.series()))
    };
    let portable = probe_executable(&portable_exe).context("冻结基础发行探测失败")?;
    ensure!(
        portable.version_str == probe.version_str && portable.soabi == soabi,
        "冻结基础发行与用户解释器完整版本/SOABI不一致"
    );
    let mut licenses = Vec::new();
    for entry in std::fs::read_dir(root.join("licenses"))? {
        let entry = entry?;
        ensure!(entry.file_type()?.is_file(), "组件许可目录含非普通文件");
        licenses.push(format!("licenses/{}", entry.file_name().to_string_lossy()));
    }
    ensure!(!licenses.is_empty(), "官方基础发行组件许可为空");
    // 上游元数据逐组件列出的许可必须实际存在，避免只复制 CPython 许可。
    fn check_licenses(value: &serde_json::Value, root: &Path) -> Result<()> {
        match value {
            serde_json::Value::Object(obj) => {
                for (key, val) in obj {
                    if key == "license_path" {
                        let rel = val.as_str().context("许可路径格式错误")?;
                        super::relative(rel)?;
                        ensure!(root.join(rel).is_file(), "组件许可缺失：{rel}");
                    } else if key == "license_paths" {
                        for path in val.as_array().context("许可路径列表格式错误")? {
                            let rel = path.as_str().context("许可路径格式错误")?;
                            super::relative(rel)?;
                            ensure!(root.join(rel).is_file(), "组件许可缺失：{rel}");
                        }
                    } else {
                        check_licenses(val, root)?;
                    }
                }
            }
            serde_json::Value::Array(items) => {
                for item in items {
                    check_licenses(item, root)?;
                }
            }
            _ => (),
        }
        Ok(())
    }
    check_licenses(&metadata, &root)?;
    let build = prepared["build"]
        .as_str()
        .context("缺少官方构建标识")?
        .to_owned();
    Ok(DiscoveredPython {
        version,
        executable,
        prefix,
        base_prefix,
        is_venv,
        distribution_root: root,
        source_project: prepared["project"]
            .as_str()
            .unwrap_or("python-build-standalone")
            .into(),
        source_url: prepared["url"]
            .as_str()
            .filter(|s| !s.is_empty())
            .map(str::to_owned)
            .unwrap_or_else(|| {
                format!("https://github.com/astral-sh/python-build-standalone/releases/tag/{build}")
            }),
        source_build_id: build,
        licenses,
        builtin_modules: portable.builtin_modules,
        soabi,
        includepy,
        libdir,
    })
}
