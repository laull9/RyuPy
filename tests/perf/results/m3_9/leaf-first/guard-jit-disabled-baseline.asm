; Assembly listing for method RyuPy.Generated.Module:m0_repeat(System.Object,System.Object):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data
; 0 inlinees with PGO data; 2 single block inlinees; 1 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x40]
       mov      r15, rdi
       mov      rbx, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r13, gword ptr [rdi]
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r12, rax
       mov      rdi, r15
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      r15, rax
       cmp      r12, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       je       G_M000_IG07
 
G_M000_IG03:
       mov      edi, 13
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyErrors:LoadName(System.Object,System.String):System.Object]
       mov      r13, rax
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x38], rax
       mov      rcx, gword ptr [rax+0x18]
       test     rcx, rcx
       je       SHORT G_M000_IG05
 
G_M000_IG04:
       cmp      dword ptr [rcx+0x08], 1
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x38]
       lea      rdi, bword ptr [rdi+0x18]
       mov      gword ptr [rbp-0x30], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
       mov      rcx, gword ptr [rbp-0x30]
 
G_M000_IG06:
       mov      gword ptr [rbp-0x30], rcx
       mov      rdi, rcx
       mov      rdx, rbx
       xor      esi, esi
       call     CORINFO_HELP_ARRADDR_ST
       mov      rdx, gword ptr [rbp-0x30]
       mov      rsi, r13
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:CallValueFast(int,System.Object,System.Object[]):System.Object]
       mov      r13, rax
       add      r12, 1
       jo       SHORT G_M000_IG09
       cmp      r12, r15
       setl     dil
       movzx    rdi, dil
       test     edi, edi
       jne      G_M000_IG03
 
G_M000_IG07:
       mov      edi, 14
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, r14
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r13
 
G_M000_IG08:
       add      rsp, 24
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 368

