# 文本与数据处理：markdown 转换与扩展。
import markdown

print(markdown.markdown("# Title\n\n- a\n- b"))
print(markdown.markdown("**bold**", extensions=["extra"]))
