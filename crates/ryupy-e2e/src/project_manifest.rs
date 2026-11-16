//! 项目运行器的参数、清单和源码根解析；严格拒绝未知、重复、空选择和缺失源码。

use std::collections::BTreeSet;
use std::path::{Path, PathBuf};

pub struct Options {
    pub only: Option<BTreeSet<String>>,
    pub store: PathBuf,
    pub output: PathBuf,
    pub run_id: String,
}

impl Options {
    pub fn parse(root: &Path) -> Result<Self, String> {
        let mut result = Self {
            only: None,
            store: root.join("third_party/projects"),
            output: root.join("target/e2e/projects"),
            run_id: String::new(),
        };
        let mut seen = BTreeSet::new();
        let mut args = std::env::args().skip(1);
        while let Some(arg) = args.next() {
            if !seen.insert(arg.clone()) {
                return Err(format!("重复参数：{arg}"));
            }
            let value = args
                .next()
                .filter(|v| !v.is_empty())
                .ok_or_else(|| format!("参数缺值：{arg}"))?;
            match arg.as_str() {
                "--only" => {
                    let ids: Vec<_> = value.split(',').map(str::trim).collect();
                    let unique: BTreeSet<_> = ids.iter().map(|id| (*id).to_owned()).collect();
                    if ids.contains(&"") || unique.len() != ids.len() {
                        return Err("--only 含空或重复 ID".to_owned());
                    }
                    result.only = Some(unique);
                }
                "--source-store" => result.store = PathBuf::from(value),
                "--out-dir" => result.output = PathBuf::from(value),
                "--run-id" => result.run_id = value,
                _ => return Err(format!("未知参数：{arg}")),
            }
        }
        if !result.store.is_absolute() || !result.output.is_absolute() {
            return Err("源码和输出目录必须为绝对路径".to_owned());
        }
        Ok(result)
    }
}

pub struct Project {
    pub id: String,
    pub pypi: String,
    pub version: String,
    pub entry: PathBuf,
    pub scope: String,
    pub deps: String,
}

pub fn read_manifest(path: &Path) -> Result<Vec<Project>, String> {
    let text = std::fs::read_to_string(path).map_err(|e| e.to_string())?;
    let mut out = Vec::new();
    let mut ids = BTreeSet::new();
    for (lineno, line) in text.trim_start_matches('\u{feff}').lines().enumerate() {
        if line.trim().is_empty() || line.starts_with('#') || line.starts_with("id\t") {
            continue;
        }
        let cols: Vec<_> = line.split('\t').collect();
        if cols.len() != 8 || cols.iter().any(|c| c.is_empty()) {
            return Err(format!("第 {} 行需要非空的 8 列", lineno + 1));
        }
        if !cols[0]
            .chars()
            .all(|c| c.is_ascii_alphanumeric() || c == '-' || c == '_')
            || !ids.insert(cols[0].to_owned())
        {
            return Err(format!("非法或重复项目 ID：{}", cols[0]));
        }
        let entry = Path::new(cols[5]);
        if entry.is_absolute()
            || entry
                .components()
                .any(|c| c == std::path::Component::ParentDir)
        {
            return Err(format!("入口不得越出项目目录：{}", cols[5]));
        }
        out.push(Project {
            id: cols[0].to_owned(),
            pypi: cols[2].to_owned(),
            version: cols[3].to_owned(),
            entry: path.parent().unwrap_or(Path::new(".")).join(entry),
            scope: cols[6].to_owned(),
            deps: cols[7].to_owned(),
        });
    }
    if out.is_empty() {
        return Err("清单为空".to_owned());
    }
    Ok(out)
}

/// 只加入本项目与 manifest 明示依赖；缺源不能悄悄退回其它搜索位置。
pub fn search_paths(store: &Path, project: &Project) -> Result<Vec<PathBuf>, String> {
    let mut packages = vec![(&*project.pypi, &*project.version)];
    if project.deps != "-" {
        for dep in project.deps.split(',') {
            let pair = dep
                .trim()
                .split_once("==")
                .filter(|(name, version)| !name.is_empty() && !version.is_empty())
                .ok_or_else(|| format!("依赖缺精确版本：{dep}"))?;
            packages.push(pair);
        }
    }
    packages
        .into_iter()
        .map(|(name, version)| {
            let mut current = store.join(format!("{name}-{version}"));
            loop {
                if current.join("src").is_dir() {
                    return Ok(current.join("src"));
                }
                let entries = std::fs::read_dir(&current)
                    .map_err(|e| format!("缺少或无法读取源码 {}：{e}", current.display()))?
                    .collect::<Result<Vec<_>, _>>()
                    .map_err(|e| e.to_string())?;
                if entries.is_empty() {
                    return Err(format!("源码目录为空：{}", current.display()));
                }
                if entries.len() == 1 && entries[0].path().is_dir() {
                    current = entries[0].path();
                } else {
                    return Ok(current);
                }
            }
        })
        .collect()
}
