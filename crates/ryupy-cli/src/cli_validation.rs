//! clap先解析，再对项目入口做跨层校验；前置全局参数不能绕过部署互斥。
use super::*;
use clap::{CommandFactory, error::ErrorKind};
use std::ffi::OsString;

impl Cli {
    pub(super) fn parse() -> Self {
        Self::try_parse_from(std::env::args_os()).unwrap_or_else(|error| error.exit())
    }

    pub(super) fn try_parse_from<I, T>(args: I) -> Result<Self, clap::Error>
    where
        I: IntoIterator<Item = T>,
        T: Into<OsString> + Clone,
    {
        let cli = <Self as Parser>::try_parse_from(args)?;
        if let Command::Build {
            project, target, ..
        } = &cli.command
        {
            // clap的冲突项可免除requires；目标选择必须独立检查成对性。
            if project.is_some() != target.is_some() {
                return Err(Self::command().error(
                    ErrorKind::MissingRequiredArgument,
                    "--project与--target必须同时指定",
                ));
            }
            // 全局参数可能先在父命令解析，再传播至子命令；不能只靠子命令conflicts。
            if project.is_some()
                && (cli.distribution_metadata.is_some()
                    || !cli.instance_modules.is_empty()
                    || cli.instance_direct_globals)
            {
                return Err(Self::command().error(
                    ErrorKind::ArgumentConflict,
                    "--project不能与--distribution-metadata、--instance-module或--instance-direct-globals合并",
                ));
            }
        }
        Ok(cli)
    }
}
