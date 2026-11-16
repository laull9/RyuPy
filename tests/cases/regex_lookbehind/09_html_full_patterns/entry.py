# 原 html.parser 的三份完整后顾模式；逐字提取赋值表达式，未做化简。
import re

attrfind_tolerant = re.compile(r"""
  (
    (?<=['"\t\n\r\f /])[^\t\n\r\f />][^\t\n\r\f /=>]*  # attribute name
   )
  ([\t\n\r\f ]*=[\t\n\r\f ]*        # value indicator
    ('[^']*'                        # LITA-enclosed value
    |"[^"]*"                        # LIT-enclosed value
    |(?!['"])[^>\t\n\r\f ]*         # bare value
    )
   )?
  (?:[\t\n\r\f ]|/(?!>))*           # possibly followed by a space
""", re.VERBOSE)

locatetagend = re.compile(r"""
  [a-zA-Z][^\t\n\r\f />]*           # tag name
  [\t\n\r\f /]*                     # optional whitespace before attribute name
  (?:(?<=['"\t\n\r\f /])[^\t\n\r\f />][^\t\n\r\f /=>]*  # attribute name
    (?:[\t\n\r\f ]*=[\t\n\r\f ]*    # value indicator
      (?:'[^']*'                    # LITA-enclosed value
        |"[^"]*"                    # LIT-enclosed value
        |(?!['"])[^>\t\n\r\f ]*     # bare value
       )
     )?
    [\t\n\r\f /]*                   # possibly followed by a space
   )*
   >?
""", re.VERBOSE)

locatestarttagend_tolerant = re.compile(r"""
  <[a-zA-Z][^\t\n\r\f />\x00]*       # tag name
  (?:[\s/]*                          # optional whitespace before attribute name
    (?:(?<=['"\s/])[^\s/>][^\s/=>]*  # attribute name
      (?:\s*=+\s*                    # value indicator
        (?:'[^']*'                   # LITA-enclosed value
          |"[^"]*"                   # LIT-enclosed value
          |(?!['"])[^>\s]*           # bare value
         )
        \s*                          # possibly followed by a space
       )?(?:\s|/(?!>))*
     )*
   )?
  \s*                                # trailing whitespace
""", re.VERBOSE)

for name, compiled in [('attr', attrfind_tolerant), ('end', locatetagend),
                       ('legacy', locatestarttagend_tolerant)]:
    for subject in (' a="b" c=bare disabled>', " a='b c' / d=2 />", '<p a="b">',
                    'p a="b">', ' x=\'unterminated', ' a==b c="">',
                    ' title="😀" /data-x=1>', '\tclass=x\nchecked>', 'a=b>',
                    'p / x=1/ y=2 >', 'p\x00 a=1>'):
        print(name, repr(subject), [(m.span(), m.groups()) for m in compiled.finditer(subject)])
