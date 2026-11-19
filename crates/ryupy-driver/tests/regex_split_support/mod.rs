//! 保留本套件标签与错误路径合同；预期由固定 CPython 当次生成，临时目录自动清理。
#[path = "../support/cpython_oracle.rs"]
mod oracle;

pub(super) fn check(filename: &str, source: &str, labels: &[&str]) {
    oracle::check(filename, source, labels, false);
}
