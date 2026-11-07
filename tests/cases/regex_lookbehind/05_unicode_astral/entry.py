# ryupy: pending
# Unicode 宽度是码点，不是 UTF-16 码元；包含非 BMP、组合符与孤立代理。
import re
for pattern, subject in [(r'(?<=.)x', '😀x Ωx'), ('(?<=😀)x', '😀x'),
                         ('(?<=😀Ω)x', '😀Ωx'), ('(?<!😀)x', '😀x ax'),
                         (r'(?<=.{2})x', '😀x a😀x'), (r'(?<=\w)x', '𐐀x 😀x'),
                         ('(?<=é)x', 'éx'), ('(?<=\ud800)x', '\ud800x')]:
    compiled = re.compile(pattern)
    print(ascii(pattern), ascii(subject), [(m.span(), ascii(m.group())) for m in compiled.finditer(subject)])
    for pos in range(len(subject) + 1):
        match = compiled.match(subject, pos)
        print(pos, None if match is None else match.span())
