"""仅CP侧观察同源真实对象；期望在执行前冻结，首次差异立即失败。"""
import json
import pathlib
import runpy
import sys


def inspect(error):
    frames = []
    tb = error.__traceback__
    while tb is not None:
        frames.append({"function": tb.tb_frame.f_code.co_name, "line": tb.tb_lineno})
        tb = tb.tb_next
    return {"type": type(error).__name__, "message": str(error), "frames": frames,
            "context": None if error.__context__ is None else inspect(error.__context__)}


def main():
    assert sys.version_info[:3] == (3, 12, 13), sys.version
    source = pathlib.Path(sys.argv[1]).resolve()
    result = inspect(runpy.run_path(str(source))["result"])
    print(json.dumps(result, ensure_ascii=False, sort_keys=True))
    expected = json.loads(source.with_suffix(".expected.json").read_text())
    assert result == expected, (result, expected)


if __name__ == "__main__":
    main()
