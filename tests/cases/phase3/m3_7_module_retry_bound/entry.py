try:
    import flaky
except ValueError:
    print('模块首次失败')
import flaky
print(flaky.number)

try:
    import static_fail
except ValueError:
    print('静态首次失败')
try:
    import static_fail
except ValueError:
    print('静态再次失败')
