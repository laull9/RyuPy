G_M000_IG56:
       mov      rsi, rax
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG57:
       cmp      byte  ptr [r13+0x18], 0
       jne      SHORT G_M000_IG60
 
G_M000_IG58:
       mov      gword ptr [rbp-0x110], rax
 
G_M000_IG59:
       mov      rdi, r13
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Value():System.Object:this]
       mov      rsi, rax
       mov      rdx, gword ptr [rbp-0x110]
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      rsi, rax
       mov      rdi, r13
       call     [RyuPy.Runtime.PyNumericAccumulator+NumericAccumulator:Store(System.Object):this]
       jmp      SHORT G_M000_IG63
 
G_M000_IG60:
       test     rax, rax
       je       G_M000_IG74
 
G_M000_IG61:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG73
 
G_M000_IG62:
       mov      gword ptr [rbp-0x110], rax
       mov      rdi, qword ptr [rax+0x08]
       mov      rcx, qword ptr [r13+0x10]
       lea      rdx, [rcx+rdi]
       xor      rcx, rdx
       xor      rdi, rdx
       and      rdi, rcx
       jl       SHORT G_M000_IG59
       mov      qword ptr [r13+0x10], rdx
       xor      rdi, rdi
       mov      gword ptr [r13+0x08], rdi
 
G_M000_IG63:
       add      r12, 1
       jo       G_M000_IG78
 
G_M000_IG64:
