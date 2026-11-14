//! 命令行意图模型：入口文件＋发布形态，普通调用自动选择兼容环境；旧子命令保留行为。
use anyhow::{Context, Result};
use clap::{Args, CommandFactory, Parser, Subcommand, ValueEnum};
use ryupy_driver::Stage;
use std::path::PathBuf;

#[derive(Parser)]
#[command(
    name = "ryupy",
    version,
    about = "将 Python 项目编译为独立应用",
    override_usage = "ryupy [OPTIONS] <ENTRY.py>\n       ryupy <COMMAND> [ARGS]",
    subcommand_negates_reqs = true,
    arg_required_else_help = true,
    after_help = "常用：ryupy main.py --onefile\n目录发布：ryupy main.py --mode standalone\n原生执行域：ryupy main.py --native\n高级参数：ryupy --help-advanced"
)]
pub(super) struct Cli {
    /// CPython provider 声明路径，或 auto 自动匹配当前 Python 环境
    #[arg(long, global = true, hide = true)]
    pub(super) cpython_provider: Option<PathBuf>,
    /// 显式封闭分发快照请求（JIT/AOT）；不改变sys.path
    #[arg(long, global = true, hide = true)]
    pub(super) distribution_metadata: Option<PathBuf>,
    /// 强制有限trace V1：域外整产物编译拒绝；普通build/run无flag自动检查有限域
    #[arg(long, global = true, hide = true)]
    pub(super) trace_product_v1: bool,
    /// 显式启用已发现源码模块的真实实例全局；不会自动包含模块（可重复）
    #[arg(long = "instance-module", global = true, hide = true)]
    pub(super) instance_modules: Vec<String>,
    /// 显式普通Instance的直接globals冷路；不支持推导式或间接builtin调用
    #[arg(long, global = true, hide = true)]
    pub(super) instance_direct_globals: bool,
    /// 实验：将受守卫循环累加展开成直接 CIL（不保证更快；默认使用 holder）
    #[arg(long, global = true, hide = true)]
    pub(super) experimental_direct_accumulator: bool,
    /// 实验：受守卫纯叶调用内联（默认关闭，不保证更快）
    #[arg(long, global = true, hide = true)]
    pub(super) experimental_leaf_inline: bool,
    /// 诊断：保SLP守卫/物化，以标量运算作为packed SIMD的公平控制组
    #[arg(
        long,
        global = true,
        hide = true,
        conflicts_with = "experimental_packed_slp"
    )]
    pub(super) diagnostic_scalar_slp: bool,
    /// 实验：真正packed两lane SLP（相对同守卫标量未获净收益，默认关闭）
    #[arg(
        long,
        global = true,
        hide = true,
        conflicts_with = "diagnostic_scalar_slp"
    )]
    pub(super) experimental_packed_slp: bool,
    /// 强制使用纯 RyuPy 执行域
    #[arg(long, global = true, conflicts_with = "cpython_provider")]
    pub(super) native: bool,
    /// 显示兼容参数与实验诊断选项
    #[arg(long, global = true)]
    pub(super) help_advanced: bool,
    /// 显示内嵌运行时组件的第三方许可证
    #[arg(long, global = true)]
    pub(super) licenses: bool,
    #[command(flatten)]
    pub(super) application: Application,
    /// 保留旧构建、运行、探测与IR诊断入口
    #[command(subcommand)]
    pub(super) command: Option<Command>,
}

/// 应用发布的最小输入；依赖、Python版本和AOT接线由驱动推导。
#[derive(Args, Default)]
pub(super) struct Application {
    /// Python 项目入口文件；本地导入自动进入编译图
    #[arg(required_unless_present_any = ["help_advanced", "licenses"])]
    pub input: Option<PathBuf>,
    /// 发布形态，默认 onefile
    #[arg(long, value_enum, conflicts_with_all = ["onefile", "standalone"])]
    pub mode: Option<Mode>,
    /// 发布单一原生可执行文件（默认）
    #[arg(long, conflicts_with = "standalone")]
    pub onefile: bool,
    /// 发布独立运行目录
    #[arg(long)]
    pub standalone: bool,
    /// 输出根目录，默认 ./dist
    #[arg(short, long, visible_alias = "output-dir")]
    pub out_dir: Option<PathBuf>,
    /// 额外源码搜索根，可重复
    #[arg(short = 'I', long = "search-path")]
    pub search_paths: Vec<PathBuf>,
    /// 显式补充动态导入的本地模块，可重复
    #[arg(long = "include-module")]
    pub include_modules: Vec<String>,
}

#[derive(Clone, Copy, ValueEnum)]
pub(super) enum Mode {
    Onefile,
    Standalone,
    Jit,
}

impl Cli {
    /// 新入口以交付应用为默认；旧子命令继续使用原执行域和输出约定。
    pub(super) fn take_command(&mut self) -> Result<Command> {
        if let Some(command) = self.command.take() {
            return Ok(command);
        }
        if !self.native && self.cpython_provider.is_none() {
            self.cpython_provider = Some(PathBuf::from("auto"));
        }
        let app = std::mem::take(&mut self.application);
        let mode = app.mode.unwrap_or(if app.standalone {
            Mode::Standalone
        } else {
            Mode::Onefile
        });
        Ok(Command::Build {
            input: app.input.context("请提供Python入口文件")?,
            diagnostic_stages: false,
            out_dir: Some(app.out_dir.unwrap_or(std::env::current_dir()?.join("dist"))),
            aot: !matches!(mode, Mode::Jit),
            onefile: matches!(mode, Mode::Onefile),
            resources: None,
            native_binding: None,
            search_paths: app.search_paths,
            include_modules: app.include_modules,
        })
    }
}

pub(super) fn print_advanced_help() -> Result<()> {
    Cli::command()
        .mut_args(|argument| argument.hide(false))
        .print_long_help()?;
    println!();
    Ok(())
}

#[cfg(test)]
#[path = "args_tests.rs"]
mod tests;

#[derive(Subcommand)]
pub(super) enum Command {
    /// 静态探测来源闭包并输出 JSON；解析源码与环境，不执行应用代码
    Probe {
        input: PathBuf,
        /// 报告及已授权 catalog 部署目录；默认入口旁 __pycache__/<入口>/probe
        #[arg(short, long)]
        out_dir: Option<PathBuf>,
        #[arg(short = 'I', long = "search-path")]
        search_paths: Vec<PathBuf>,
        /// 按现有默认 AOT 策略探测，不启动 ILC
        #[arg(long)]
        aot: bool,
    },
    /// 编译为 .dll，或通过 --aot / --onefile 发布原生应用
    #[command(group(clap::ArgGroup::new("native_publication").args(["aot", "onefile"]).multiple(true)))]
    Build {
        input: PathBuf,
        /// 诊断：逐阶段写 stderr（编译至产物写出，不含 AOT publish；默认关闭）
        #[arg(long, conflicts_with = "native_binding")]
        diagnostic_stages: bool,
        /// 输出根；AOT使用<入口>.build/.dist，单文件发布到<入口>.onefile，JIT默认__pycache__/
        #[arg(short, long)]
        out_dir: Option<PathBuf>,
        #[arg(long)]
        aot: bool,
        /// 将完整 NativeAOT 依赖封装为唯一可执行文件（隐含 --aot）
        #[arg(long)]
        onefile: bool,
        /// 显式资源清单（AOT / onefile；目标相对路径首切限 ASCII）
        #[arg(long, requires = "native_publication")]
        resources: Option<PathBuf>,
        /// 显式固定签名 C ABI 声明；AOT 可同时发布显式资源
        #[arg(long)]
        native_binding: Option<PathBuf>,
        /// 额外的模块搜索根（可重复；入口文件所在目录始终最优先）
        #[arg(short = 'I', long = "search-path")]
        search_paths: Vec<PathBuf>,
        /// 显式包含ASCII点分源码模块（可重复；仍按import惰性初始化，不复制资源）
        #[arg(long = "include-module")]
        include_modules: Vec<String>,
    },
    /// 编译并立即用 dotnet 运行
    Run {
        input: PathBuf,
        /// 诊断：逐阶段写 stderr（仅编译，不含 dotnet 运行；默认关闭）
        #[arg(long)]
        diagnostic_stages: bool,
        /// 额外的模块搜索根（可重复；入口文件所在目录始终最优先）
        #[arg(short = 'I', long = "search-path")]
        search_paths: Vec<PathBuf>,
        /// 显式包含ASCII点分源码模块（可重复；仍按import惰性初始化，不复制资源）
        #[arg(long = "include-module")]
        include_modules: Vec<String>,
    },
    /// 打印某一阶段的中间表示
    Dump {
        input: PathBuf,
        #[arg(long, value_enum, default_value_t = EmitArg::Hir)]
        emit: EmitArg,
    },
}

#[derive(Clone, Copy, ValueEnum)]
pub(super) enum EmitArg {
    Ast,
    Hir,
    Types,
    Mir,
    Cil,
}

impl From<EmitArg> for Stage {
    fn from(e: EmitArg) -> Self {
        match e {
            EmitArg::Ast => Stage::Ast,
            EmitArg::Hir => Stage::Hir,
            EmitArg::Types => Stage::Types,
            EmitArg::Mir => Stage::Mir,
            EmitArg::Cil => Stage::Cil,
        }
    }
}
