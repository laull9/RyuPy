"""补充真实业务观测，不替换固定 markdown_sample.py，也不计新项目。"""
import markdown

print(markdown.markdown('<div markdown="1">\n\n**bold**\n\n</div>', extensions=['extra']))
print(markdown.markdown('<div markdown="1">\nalpha\n<p markdown="span">*inner*</p>\ntail\n</div>', extensions=['extra']))
