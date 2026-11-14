# URL 逐 API 差分：保留分隔符、相对引用、重复查询键与 UTF-8 编解码。
from urllib.parse import urlsplit, urlparse, urljoin, urlencode, quote, unquote, parse_qs, quote_plus, unquote_plus, SplitResult, ParseResult

for url in ["HTTP://user:pw@Example.COM:8080/a;b?q=1#f", "../a?x=#", "//[::1]:443/a", "https://x/a%2Fb", "\x00 \thttps://x/a\nb?x=1"]:
    result = urlsplit(url)
    print(tuple(result), result.scheme, result.netloc, result.path, result.query, result.fragment)
    print(result.username, result.password, result.hostname, result.port, result.geturl())
    parsed = urlparse(url)
    print(tuple(parsed), parsed.params, parsed.geturl())
for url in ["g:h", "g", "./g", "g/", "/g", "//g", "?y", "g?y", "#s", "g#s", "g?y#s", ";x", "g;x", "g;x?y#s", "", ".", "./", "..", "../", "../g", "../..", "../../", "../../g", "../../../g", "../../../../g", "/./g", "/../g", "g.", ".g", "g..", "..g", "./../g", "./g/.", "g/./h", "g/../h", "g;x=1/./y", "g;x=1/../y", "g?y/./x", "g?y/../x", "g#s/./x", "g#s/../x", "g//h", "/g//h", "http:g", "https://other/x"]:
    print(urljoin("http://a/b/c/d;p?q", url))
result = urlsplit("https://user:pw@EXAMPLE.com/a?x=1#f")
print(repr(result), str(result), type(result).__name__)
print(isinstance(result, tuple), isinstance(result, SplitResult), isinstance(urlparse(""), ParseResult))
print(result[0], result[-1], len(result), result.count("https"), result.index("x=1"))
saved = result.geturl
print(saved())
for scheme in ["gopher", "svn+ssh", "mms", "mailto"]:
    print(urljoin(scheme + "://a/b/c", "../g"))
print(urlsplit("a#b", allow_fragments=False).path)
print(urlparse("a;p?q#f", allow_fragments=False).params)
print(quote("中文 a/b+~", safe="/"))
print(quote(b"\xff /", safe=""))
print(quote("?", safe=b"\xff"), quote_plus("a b+/"), unquote_plus("a+b%2B%2f"))
print(quote("é", encoding="latin-1"), unquote("%E9", encoding="latin-1"))
for text in ["%FF", "%%FF", "%C0%AF", "%E2%28%A1", "中%E4%B8%AD", "%"]:
    print(unquote(text))
print(unquote("%E4%B8%AD%FF%2f%zz"))
print(urlencode([("a", "x y"), ("a", "+/"), ("n", 3)]))
print(urlencode({"a": ["x", "y"], "b": [], "c": "z"}, doseq=True))
print(parse_qs("a=1&a=2&blank=&flag&utf=%E4%B8%AD+ok"))
print(parse_qs("a=&a=2&flag", keep_blank_values=True))
print(parse_qs("a=1;b=2", separator=";"))
for url in ["http://x:bad", "http://x:99999", "http://[abc", "http://[v.foo]", "http://[v1.]", "http://prefix[::1]suffix", "http://[::1]suffix", "http://／x", "http://x:０８０"]:
    try:
        print(urlsplit(url).port)
    except ValueError:
        print("invalid authority")
try:
    parse_qs("a=1&bad", strict_parsing=True)
except ValueError as error:
    print(str(error))
try:
    parse_qs("a=1&b=2", max_num_fields=1)
except ValueError as error:
    print(str(error))
