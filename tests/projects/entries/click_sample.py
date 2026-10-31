# 命令行工具：click 的装饰器、选项与参数。
import click


@click.command()
@click.option("--count", default=1, type=int)
@click.argument("name")
def greet(count, name):
    for _ in range(count):
        click.echo(f"hello {name}")


greet.main(["--count", "2", "world"], standalone_mode=False)
print(greet.name)
