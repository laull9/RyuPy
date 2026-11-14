"""当前生产者的compiler.options严格合同；共享于schema1/2/3，不迁移旧四字段清单。"""
from manifest_files import ManifestError

BOOL_OPTIONS = frozenset(('experimental_direct_accumulator', 'experimental_foreach_accumulator',
                         'experimental_leaf_inline', 'diagnostic_scalar_slp', 'experimental_packed_slp'))
OPTION_KEYS = BOOL_OPTIONS | {'instance_modules', 'module_environment_policy'}
# 与ModuleEnvironmentPolicy.identity的三个生产者值一致；不放行任意未知策略。
POLICIES = frozenset(('explicit-only', 'catalog-only', 'ordinary-dynamic-module-pilot'))


def validate_options(options):
    if not isinstance(options, dict) or set(options) != OPTION_KEYS:
        raise ManifestError('compiler.options必须精确包含当前七字段；不迁移历史四/五/六字段清单')
    if any(type(options[key]) is not bool for key in BOOL_OPTIONS):
        raise ManifestError('优化开关必须是bool')
    modules = options['instance_modules']
    # Session允许重复的显式选择；这里只验真实序列/字符串，不凭空去重或扩大选择能力。
    if type(modules) is not list or any(type(name) is not str or not name for name in modules):
        raise ManifestError('instance_modules必须是非空字符串组成的列表（空列表允许）')
    if type(options['module_environment_policy']) is not str or options['module_environment_policy'] not in POLICIES:
        raise ManifestError('未知module_environment_policy')
