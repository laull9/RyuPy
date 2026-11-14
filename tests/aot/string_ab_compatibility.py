"""固定已知差异观察门；不将预期拒绝称为CPython兼容通过。"""
from direct_protocol import validate_output
from string_ab_protocol import ARMS

CP_OUTPUT = b'radd supported True\niadd supported True\n'
RYUPY_OUTPUT = (
    b'radd known-unsupported str subclass slot __radd__ is not supported\n'
    b'iadd known-unsupported str subclass slot __iadd__ is not supported\n'
)


def observe(runner, commands, root, env, report, save):
    observations = []
    report['known_incompatibility'] = dict(
        case='custom_slots', classification='known_incompatibility',
        cpython_equivalent=False, observations=observations,
    )
    for arm in ARMS:
        row = dict(arm=arm, status='failed')
        observations.append(row)
        try:
            result = runner.run(f'compatibility-custom_slots-{arm}', commands[arm],
                                cwd=root, env=env, limit=120)
            row['process'] = result['case']
            validate_output(result, (CP_OUTPUT if arm == 'cpython' else RYUPY_OUTPUT, b''))
            row['status'] = 'expected-supported' if arm == 'cpython' else 'expected-unsupported'
        except Exception as error:
            row['error'] = f'{type(error).__name__}: {error}'
        save()
    if any(row['status'] == 'failed' for row in observations):
        raise ValueError('已知兼容性差异发生漂移，已尝试全部五臂；禁止性能结论')
