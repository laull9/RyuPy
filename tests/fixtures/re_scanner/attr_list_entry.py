# 独立真实属性工作负载：不改固定20入口，不启用extra，不包装上游Scanner。
import markdown

cases = [
    (
        "# Title {#hero .wide .bright title=\"hello world\" data-note='two words' lang=en disabled}",
        '<h1 class="wide bright" data-note="two words" disabled="disabled" id="hero" lang="en" title="hello world">Title</h1>',
    ),
    (
        '*word*{: .em title="a b"}',
        '<p><em class="em" title="a b">word</em></p>',
    ),
    (
        'Paragraph\n{: #block .note}',
        '<p class="note" id="block">Paragraph</p>',
    ),
]
for source, expected in cases:
    actual = markdown.markdown(source, extensions=['attr_list'])
    print(actual)
    assert actual == expected
