; Assembly listing for method RyuPy.Generated.Module:m0_twice(System.Object):System.Object (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 32
       lea      rbp, [rsp+0x20]
       vxorps   xmm8, xmm8, xmm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x10], rax
       mov      gword ptr [rbp-0x08], rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x10], rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdx, gword ptr [rbp-0x08]
       mov      rcx, gword ptr [rbp-0x08]
       xor      edi, edi
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x20], rax
       mov      rax, gword ptr [rbp-0x20]
       mov      gword ptr [rbp-0x18], rax
       mov      rdi, qword ptr [rbp-0x10]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x18]
 
G_M000_IG03:
       add      rsp, 32
       pop      rbp
       ret      
 
; Total bytes of code 124

