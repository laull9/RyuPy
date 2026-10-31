# RyuPy 常用命令。`make help` 查看全部。
.DEFAULT_GOAL := help
FILE ?= tests/e2e/phase1/m1_0_print_int.py
EMIT ?= hir
PYTHON ?= python3

.PHONY: help
help: ## 显示帮助
	@grep -hE '^[a-zA-Z0-9_-]+:.*?## ' $(MAKEFILE_LIST) | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-12s\033[0m %s\n", $$1, $$2}'

.PHONY: runtime
runtime: ## 构建 C# 运行时 RyuPy.Runtime（Release）
	dotnet build runtime/RyuPy.Runtime -c Release -nologo -v q

.PHONY: fmt
fmt: ## 格式化 Rust 代码
	cargo fmt --all

.PHONY: lint
lint: ## fmt 检查 + clippy（警告视为错误）
	cargo fmt --all --check
	cargo clippy --workspace --all-targets -- -D warnings

.PHONY: test
test: python-baseline ## Python 基线 + 单元测试 + 快照测试（不含编译 e2e）
	dotnet test runtime/RyuPy.Runtime.Tests -nologo -v q
	cargo test --workspace --exclude ryupy-e2e

.PHONY: python-baseline
python-baseline: ## 自检 Phase 1–3 的 CPython 用例与预期错误退出
	"$(PYTHON)" tests/run_python_baseline.py

.PHONY: python-extensions
python-extensions: ## 自检 Phase 4 扩展基线（需已安装 NumPy、sqlite3、zlib）
	"$(PYTHON)" tests/run_python_baseline.py --phase phase4

.PHONY: e2e
e2e: runtime ## 端到端差分测试（跳过 pending 用例）
	cargo test -p ryupy-e2e

.PHONY: e2e-all
e2e-all: runtime ## 端到端测试，包括 pending 用例（查看进度用）
	-cargo test -p ryupy-e2e -- --include-ignored

.PHONY: cases-selftest
cases-selftest: ## 只自检用例与框架：多文件/参数/环境/输入/错误路径（不编译 RyuPy）
	E2E_PYTHON_ONLY=1 cargo test -p ryupy-e2e -- --include-ignored

.PHONY: check
check: lint test cases-selftest e2e ## 提交前必跑：lint + test + 用例自检 + e2e

.PHONY: snap
snap: ## 交互式审阅 insta 快照（需 cargo install cargo-insta）
	cargo insta review

.PHONY: dump
dump: ## 打印中间表示：make dump FILE=x.py EMIT=ast|hir|types|mir|cil
	cargo run -q -p ryupy-cli -- dump $(FILE) --emit $(EMIT)

.PHONY: run
run: runtime ## 编译并运行：make run FILE=x.py
	cargo run -q -p ryupy-cli -- run $(FILE)

.PHONY: clean
clean: ## 清理全部产物
	cargo clean
	rm -rf runtime/RyuPy.Runtime/bin runtime/RyuPy.Runtime/obj
