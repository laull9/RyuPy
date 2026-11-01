; Assembly listing for method RyuPy.Generated.Module:m0_call_alias(System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 224
       lea      rbp, [rsp+0xE0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      gword ptr [rbp-0x30], rdi
 
G_M000_IG02:
       mov      dword ptr [rbp-0xD8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x48], rax
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x50], rax
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x38], rax
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x58], rax
       mov      rdi, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x60], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x68], rax
       mov      rax, qword ptr [rbp-0x60]
       mov      qword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x78], rax
       mov      qword ptr [rbp-0x80], 1
       mov      rdi, gword ptr [rbp-0x38]
       call     [RyuPy.Runtime.PyNumericAccumulator:Create(System.Object):System.Object]
       mov      gword ptr [rbp-0xD0], rax
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0xD8]
       dec      eax
       mov      dword ptr [rbp-0xD8], eax
       cmp      dword ptr [rbp-0xD8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xD8]
       mov      esi, 94
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x80], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x84], eax
       mov      rax, qword ptr [rbp-0x70]
       cmp      rax, qword ptr [rbp-0x78]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x88], eax
       mov      rax, qword ptr [rbp-0x70]
       cmp      rax, qword ptr [rbp-0x78]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x8C], eax
       mov      eax, dword ptr [rbp-0x84]
       and      eax, dword ptr [rbp-0x88]
       movzx    rax, al
       mov      dword ptr [rbp-0x90], eax
       cmp      dword ptr [rbp-0x84], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       mov      eax, dword ptr [rbp-0x94]
       and      eax, dword ptr [rbp-0x8C]
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       mov      eax, dword ptr [rbp-0x90]
       or       eax, dword ptr [rbp-0x98]
       movzx    rax, al
       mov      dword ptr [rbp-0x9C], eax
       cmp      dword ptr [rbp-0x9C], 0
       jne      SHORT G_M000_IG06
       jmp      G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x70]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0xA8], rax
       mov      rax, gword ptr [rbp-0xA8]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rdi, gword ptr [rax]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyErrors:LoadName(System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xB0], rax
       mov      rdx, gword ptr [rbp-0x40]
       mov      rsi, gword ptr [rbp-0xB0]
       mov      edi, 1
       call     [RyuPy.Runtime.PyPic:CallValue1(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rdi, gword ptr [rbp-0xD0]
       mov      rsi, gword ptr [rbp-0xB8]
       call     [RyuPy.Runtime.PyNumericAccumulator:Add(System.Object,System.Object)]
       mov      rax, qword ptr [rbp-0x70]
       add      rax, qword ptr [rbp-0x80]
       jo       SHORT G_M000_IG09
       mov      qword ptr [rbp-0x70], rax
       jmp      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0xD0]
       call     [RyuPy.Runtime.PyNumericAccumulator:Value(System.Object):System.Object]
       mov      gword ptr [rbp-0x38], rax
       mov      edi, 18
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0xC8], rax
       mov      rdi, qword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xC8]
 
G_M000_IG08:
       add      rsp, 224
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 660

; Assembly listing for method RyuPy.Generated.Module:m0_call_direct(System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 208
       lea      rbp, [rsp+0xD0]
       xor      eax, eax
       mov      qword ptr [rbp-0xC8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       vmovdqa  xmmword ptr [rbp-0x40], xmm8
       mov      gword ptr [rbp-0x30], rdi
 
G_M000_IG02:
       mov      dword ptr [rbp-0xD0], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x48], rax
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x50], rax
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x38], rax
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x58], rax
       mov      rdi, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x60], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x68], rax
       mov      rax, qword ptr [rbp-0x60]
       mov      qword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x78], rax
       mov      qword ptr [rbp-0x80], 1
       mov      rdi, gword ptr [rbp-0x38]
       call     [RyuPy.Runtime.PyNumericAccumulator:Create(System.Object):System.Object]
       mov      gword ptr [rbp-0xC8], rax
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0xD0]
       dec      eax
       mov      dword ptr [rbp-0xD0], eax
       cmp      dword ptr [rbp-0xD0], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xD0]
       mov      esi, 94
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x80], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x84], eax
       mov      rax, qword ptr [rbp-0x70]
       cmp      rax, qword ptr [rbp-0x78]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x88], eax
       mov      rax, qword ptr [rbp-0x70]
       cmp      rax, qword ptr [rbp-0x78]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x8C], eax
       mov      eax, dword ptr [rbp-0x84]
       and      eax, dword ptr [rbp-0x88]
       movzx    rax, al
       mov      dword ptr [rbp-0x90], eax
       cmp      dword ptr [rbp-0x84], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       mov      eax, dword ptr [rbp-0x94]
       and      eax, dword ptr [rbp-0x8C]
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       mov      eax, dword ptr [rbp-0x90]
       or       eax, dword ptr [rbp-0x98]
       movzx    rax, al
       mov      dword ptr [rbp-0x9C], eax
       cmp      dword ptr [rbp-0x9C], 0
       jne      SHORT G_M000_IG06
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x70]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0xA8], rax
       mov      rax, gword ptr [rbp-0xA8]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x40]
       call     [RyuPy.Generated.Module:m0_twice(System.Object):System.Object]
       mov      gword ptr [rbp-0xB0], rax
       mov      rdi, gword ptr [rbp-0xC8]
       mov      rsi, gword ptr [rbp-0xB0]
       call     [RyuPy.Runtime.PyNumericAccumulator:Add(System.Object,System.Object)]
       mov      rax, qword ptr [rbp-0x70]
       add      rax, qword ptr [rbp-0x80]
       jo       SHORT G_M000_IG09
       mov      qword ptr [rbp-0x70], rax
       jmp      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0xC8]
       call     [RyuPy.Runtime.PyNumericAccumulator:Value(System.Object):System.Object]
       mov      gword ptr [rbp-0x38], rax
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0xC0], rax
       mov      rdi, qword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xC0]
 
G_M000_IG08:
       add      rsp, 208
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 612

; Assembly listing for method RyuPy.Generated.Module:m0_call_alias(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x5e
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 2
; 1 inlinees with PGO data; 15 single block inlinees; 18 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 160
       mov      qword ptr [rsp+0x188], r15
       mov      qword ptr [rsp+0x180], r14
       mov      qword ptr [rsp+0x178], r13
       mov      qword ptr [rsp+0x170], r12
       mov      qword ptr [rsp+0x168], rbx
       lea      rbp, [rsp+0xA0]
       mov      r12, qword ptr [rbp+0xA8]
       mov      rbx, qword ptr [rbp+0x80]
       mov      r14, qword ptr [rbp+0x78]
       mov      r13, qword ptr [rbp+0x70]
       mov      r15, bword ptr [rbp+0x20]
 
G_M000_IG02:
       test     r13, r13
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x3C], eax
       jmp      G_M000_IG30
 
G_M000_IG03:
       mov      gword ptr [rbp-0x48], rax
 
G_M000_IG04:
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ThrowUnboundGlobal(System.String)]
 
G_M000_IG05:
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp+0x40], rax
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp-0x90], rcx
       mov      rdx, gword ptr [rcx+0x18]
       test     rdx, rdx
       je       SHORT G_M000_IG07
 
G_M000_IG06:
       cmp      dword ptr [rdx+0x08], 1
       mov      gword ptr [rbp-0x60], rdx
       je       SHORT G_M000_IG08
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x90]
       lea      rdi, bword ptr [rdi+0x18]
       mov      gword ptr [rbp-0x60], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG08:
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0x58], rax
       mov      rdi, rax
       mov      rdx, gword ptr [rbp-0x48]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rax, gword ptr [rbp+0x40]
       test     rax, rax
       je       SHORT G_M000_IG12
 
G_M000_IG09:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG40
 
G_M000_IG10:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG39
 
G_M000_IG11:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG38
 
G_M000_IG12:
       xor      r9d, r9d
 
G_M000_IG13:
       mov      dword ptr [rbp-0x2C], r9d
       mov      rcx, 0xD1FFAB1E
       mov      r10, gword ptr [rcx]
       mov      gword ptr [rbp-0x98], r10
       lea      rcx, bword ptr [r10+0x50]
       cmp      byte  ptr [rcx+0x08], 0
       je       G_M000_IG41
 
G_M000_IG14:
       cmp      gword ptr [rcx], rax
       jne      G_M000_IG41
 
G_M000_IG15:
       cmp      r9d, 1
       jne      G_M000_IG71
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG17
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG70
 
G_M000_IG17:
       mov      rax, gword ptr [rsi+0x18]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rdx, gword ptr [rbp-0x58]
       mov      rdi, gword ptr [rax+0x08]
       call     [rax+0x18]RyuPy.Runtime.PyCall:Invoke(RyuPy.Runtime.PyFunction,System.Object[],System.String[],System.Object[]):System.Object:this
 
G_M000_IG18:
       movzx    rdi, byte  ptr [r15+0x18]
       test     edi, edi
       je       G_M000_IG48
 
G_M000_IG19:
       test     rax, rax
       jne      G_M000_IG43
 
G_M000_IG20:
       mov      rsi, gword ptr [r15+0x08]
       test     rsi, rsi
       jne      SHORT G_M000_IG24
 
G_M000_IG21:
       mov      rcx, qword ptr [r15+0x10]
       mov      qword ptr [rbp-0x38], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      G_M000_IG47
 
G_M000_IG22:
       mov      gword ptr [rbp-0x68], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x38]
       mov      qword ptr [rax+0x08], rdi
       mov      rsi, rax
 
G_M000_IG23:
       lea      rdi, bword ptr [r15+0x08]
       mov      gword ptr [rbp-0x80], rsi
       call     CORINFO_HELP_CHECKED_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x80]
       mov      rax, gword ptr [rbp-0x68]
 
G_M000_IG24:
       mov      rdx, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x78], rax
       lea      rdi, bword ptr [r15+0x08]
       mov      rsi, rax
       call     CORINFO_HELP_CHECKED_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x78]
       test     rsi, rsi
       jne      G_M000_IG49
 
G_M000_IG25:
       xor      edi, edi
 
G_M000_IG26:
       mov      byte  ptr [r15+0x18], dil
       test     rsi, rsi
       je       SHORT G_M000_IG29
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      SHORT G_M000_IG29
 
G_M000_IG28:
       mov      rsi, qword ptr [rsi+0x08]
       mov      qword ptr [r15+0x10], rsi
 
G_M000_IG29:
       add      rbx, r13
       jo       G_M000_IG75
       mov      eax, dword ptr [rbp-0x3C]
 
G_M000_IG30:
       mov      edi, eax
       cmp      rbx, r14
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       cmp      rbx, r14
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG51
 
G_M000_IG31:
       lea      rcx, [rbx+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG46
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], rbx
 
G_M000_IG33:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG36
 
G_M000_IG34:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG74
       cmp      dword ptr [rdx+4*rsi+0x10], 17
       je       SHORT G_M000_IG36
 
G_M000_IG35:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG74
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 17
 
G_M000_IG36:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp-0x50], rcx
       test     rcx, rcx
       je       G_M000_IG03
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       mov      gword ptr [rbp-0x48], rax
       jne      G_M000_IG05
       jmp      G_M000_IG04
 
G_M000_IG38:
       mov      r9d, 3
       jmp      G_M000_IG13
 
G_M000_IG39:
       mov      r9d, 2
       jmp      G_M000_IG13
 
G_M000_IG40:
       mov      r9d, 1
       jmp      G_M000_IG13
 
G_M000_IG41:
       lea      rcx, bword ptr [r10+0x60]
       cmp      byte  ptr [rcx+0x08], 0
       je       G_M000_IG65
 
G_M000_IG42:
       cmp      gword ptr [rcx], rax
       jne      G_M000_IG65
       jmp      G_M000_IG15
 
G_M000_IG43:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG20
 
G_M000_IG44:
       mov      gword ptr [rbp-0x68], rax
       mov      rdi, qword ptr [rax+0x08]
       mov      rcx, qword ptr [r15+0x10]
       lea      rdx, [rcx+rdi]
       xor      rcx, rdx
       xor      rdi, rdx
       and      rdi, rcx
       mov      rax, gword ptr [rbp-0x68]
       jl       G_M000_IG20
 
G_M000_IG45:
       mov      qword ptr [r15+0x10], rdx
       xor      rdi, rdi
       mov      gword ptr [r15+0x08], rdi
       jmp      G_M000_IG29
 
G_M000_IG46:
       test     rcx, rcx
       jl       G_M000_IG75
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG74
       mov      rdi, gword ptr [rcx+8*rdi+0x10]
       mov      gword ptr [rbp-0x48], rdi
       mov      rax, gword ptr [rbp-0x48]
       jmp      G_M000_IG33
 
G_M000_IG47:
       test     rdx, rdx
       jl       G_M000_IG75
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG74
       mov      rsi, gword ptr [rcx+8*rsi+0x10]
       mov      gword ptr [rbp-0x68], rax
       jmp      G_M000_IG23
 
G_M000_IG48:
       mov      rsi, gword ptr [r15+0x08]
       jmp      G_M000_IG24
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG25
 
G_M000_IG50:
       mov      edi, 1
       jmp      G_M000_IG26
 
G_M000_IG51:
       cmp      byte  ptr [r15+0x18], 0
       je       G_M000_IG64
 
G_M000_IG52:
       mov      rax, gword ptr [r15+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG56
 
G_M000_IG53:
       mov      rbx, qword ptr [r15+0x10]
       lea      r14, [rbx+0x05]
       cmp      r14, 261
       jbe      G_M000_IG63
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      qword ptr [r13+0x08], rbx
 
G_M000_IG55:
       lea      rdi, bword ptr [r15+0x08]
       mov      rsi, r13
       call     CORINFO_HELP_CHECKED_ASSIGN_REF
       mov      rax, r13
 
G_M000_IG56:
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG59
 
G_M000_IG57:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG74
       cmp      dword ptr [rdi+4*r8+0x10], 18
       je       SHORT G_M000_IG59
 
G_M000_IG58:
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG74
       mov      edi, esi
       mov      dword ptr [rdx+4*rdi+0x10], 18
 
G_M000_IG59:
       test     r12, r12
       jle      SHORT G_M000_IG62
 
G_M000_IG60:
       movsxd   rcx, ecx
       cmp      r12, rcx
       jg       SHORT G_M000_IG62
 
G_M000_IG61:
       dec      r12d
       mov      dword ptr [(reloc)], r12d
 
G_M000_IG62:
       add      rsp, 360
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG63:
       test     r14, r14
       jl       G_M000_IG75
       mov      rdi, r14
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rsi]
       cmp      rdi, 262
       jae      G_M000_IG74
       mov      r13, gword ptr [rcx+8*rdi+0x10]
       jmp      G_M000_IG55
 
G_M000_IG64:
       mov      rax, gword ptr [r15+0x08]
       jmp      G_M000_IG56
 
G_M000_IG65:
       lea      rcx, bword ptr [r10+0x70]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG67
 
G_M000_IG66:
       cmp      gword ptr [rcx], rax
       je       G_M000_IG15
 
G_M000_IG67:
       lea      rcx, bword ptr [r10+0x80]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG69
 
G_M000_IG68:
       cmp      gword ptr [rcx], rax
       jne      SHORT G_M000_IG69
       mov      r10, gword ptr [rbp-0x98]
       jmp      G_M000_IG15
 
G_M000_IG69:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      edi, r9d
       mov      rsi, rax
       mov      rdx, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyPic:InvokeCached(byte,System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x70], rax
       cmp      dword ptr [rbp-0x2C], 0
       je       G_M000_IG73
       jmp      SHORT G_M000_IG72
 
G_M000_IG70:
       mov      rsi, rax
       call     [CORINFO_HELP_CHKCASTCLASS]
       int3     
 
G_M000_IG71:
       cmp      r9d, 3
       jne      SHORT G_M000_IG69
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, gword ptr [rbp-0x58]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyClass:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       jmp      G_M000_IG18
 
G_M000_IG72:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       add      rdi, 17
       movzx    rsi, byte  ptr [rdi]
       lea      edx, [rsi+0x01]
       mov      byte  ptr [rdi], dl
       and      esi, 3
       add      esi, 4
       mov      edi, esi
       shl      rdi, 4
       mov      rsi, gword ptr [rbp-0x98]
       lea      rdx, bword ptr [rsi+rdi+0x10]
       mov      bword ptr [rbp-0x88], rdx
       mov      rsi, gword ptr [rbp+0x40]
       mov      rdi, rdx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, bword ptr [rbp-0x88]
       mov      byte  ptr [rdi+0x08], 1
       mov      ecx, dword ptr [rbp-0x2C]
       mov      byte  ptr [rdi+0x09], cl
 
G_M000_IG73:
       mov      rax, gword ptr [rbp-0x70]
       jmp      G_M000_IG18
 
G_M000_IG74:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG75:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1639

; Assembly listing for method RyuPy.Generated.Module:m0_call_direct(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x5e
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 2
; 0 inlinees with PGO data; 5 single block inlinees; 14 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 80
       mov      qword ptr [rsp+0x128], r15
       mov      qword ptr [rsp+0x120], r14
       mov      qword ptr [rsp+0x118], r13
       mov      qword ptr [rsp+0x110], r12
       mov      qword ptr [rsp+0x108], rbx
       lea      rbp, [rsp+0x50]
       mov      r12, qword ptr [rbp+0x98]
       mov      rbx, qword ptr [rbp+0x70]
       mov      r14, qword ptr [rbp+0x68]
       mov      r13, qword ptr [rbp+0x60]
       mov      r15, bword ptr [rbp+0x18]
 
G_M000_IG02:
       test     r13, r13
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x34], eax
       jmp      G_M000_IG22
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG12
 
G_M000_IG04:
       mov      rdi, qword ptr [rax+0x08]
       mov      rcx, qword ptr [r15+0x10]
       lea      rdx, [rcx+rdi]
       xor      rcx, rdx
       xor      rdi, rdx
       and      rdi, rcx
       mov      rax, gword ptr [rbp+0x30]
       jl       G_M000_IG12
 
G_M000_IG05:
       mov      qword ptr [r15+0x10], rdx
       xor      rdi, rdi
       mov      gword ptr [r15+0x08], rdi
       jmp      G_M000_IG21
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, rax
       mov      qword ptr [rdi+0x08], rbx
 
G_M000_IG07:
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG10
 
G_M000_IG08:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, rdx
       dec      ecx
       mov      r8d, ecx
       cmp      r8d, dword ptr [rsi+0x08]
       jae      G_M000_IG43
       cmp      dword ptr [rsi+4*r8+0x10], 24
       je       SHORT G_M000_IG10
 
G_M000_IG09:
       cmp      ecx, dword ptr [rdx+0x08]
       jae      G_M000_IG43
       mov      ecx, ecx
       mov      dword ptr [rdx+4*rcx+0x10], 24
 
G_M000_IG10:
       call     [RyuPy.Generated.Module:m0_twice(System.Object):System.Object]
       mov      gword ptr [rbp+0x30], rax
       movzx    rdi, byte  ptr [r15+0x18]
       test     edi, edi
       je       G_M000_IG26
 
G_M000_IG11:
       test     rax, rax
       jne      G_M000_IG03
 
G_M000_IG12:
       mov      rsi, gword ptr [r15+0x08]
       test     rsi, rsi
       jne      SHORT G_M000_IG16
 
G_M000_IG13:
       mov      rcx, qword ptr [r15+0x10]
       mov      qword ptr [rbp-0x30], rcx
       lea      rdx, [rcx+0x05]
       cmp      rdx, 261
       jbe      G_M000_IG25
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x30]
       mov      qword ptr [rax+0x08], rdi
       mov      rsi, rax
 
G_M000_IG15:
       lea      rdi, bword ptr [r15+0x08]
       mov      gword ptr [rbp-0x48], rsi
       call     CORINFO_HELP_CHECKED_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x48]
       mov      rax, gword ptr [rbp+0x30]
 
G_M000_IG16:
       mov      rdx, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x40], rax
       lea      rdi, bword ptr [r15+0x08]
       mov      rsi, rax
       call     CORINFO_HELP_CHECKED_ASSIGN_REF
       mov      rdi, gword ptr [rbp-0x40]
       test     rdi, rdi
       jne      G_M000_IG27
 
G_M000_IG17:
       xor      eax, eax
 
G_M000_IG18:
       mov      byte  ptr [r15+0x18], al
       test     rdi, rdi
       je       SHORT G_M000_IG21
 
G_M000_IG19:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      SHORT G_M000_IG21
 
G_M000_IG20:
       mov      rdi, qword ptr [rdi+0x08]
       mov      qword ptr [r15+0x10], rdi
 
G_M000_IG21:
       add      rbx, r13
       jo       G_M000_IG44
       mov      eax, dword ptr [rbp-0x34]
 
G_M000_IG22:
       mov      edi, eax
       cmp      rbx, r14
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       cmp      rbx, r14
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG29
 
G_M000_IG23:
       lea      rcx, [rbx+0x05]
       cmp      rcx, 261
       ja       G_M000_IG06
 
G_M000_IG24:
       test     rcx, rcx
       jl       G_M000_IG44
       mov      rdi, rcx
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       cmp      rdi, 262
       jae      G_M000_IG43
       mov      rdi, gword ptr [rcx+8*rdi+0x10]
       jmp      G_M000_IG07
 
G_M000_IG25:
       test     rdx, rdx
       jl       G_M000_IG44
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG43
       mov      rsi, gword ptr [rcx+8*rsi+0x10]
       jmp      G_M000_IG15
 
G_M000_IG26:
       mov      rsi, gword ptr [r15+0x08]
       jmp      G_M000_IG16
 
G_M000_IG27:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      G_M000_IG17
 
G_M000_IG28:
       mov      eax, 1
       jmp      G_M000_IG18
 
G_M000_IG29:
       cmp      byte  ptr [r15+0x18], 0
       je       G_M000_IG42
 
G_M000_IG30:
       mov      rax, gword ptr [r15+0x08]
       test     rax, rax
       jne      SHORT G_M000_IG34
 
G_M000_IG31:
       mov      rbx, qword ptr [r15+0x10]
       lea      r14, [rbx+0x05]
       cmp      r14, 261
       jbe      G_M000_IG41
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      qword ptr [r13+0x08], rbx
 
G_M000_IG33:
       lea      rdi, bword ptr [r15+0x08]
       mov      rsi, r13
       call     CORINFO_HELP_CHECKED_ASSIGN_REF
       mov      rax, r13
 
G_M000_IG34:
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG37
 
G_M000_IG35:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG43
       cmp      dword ptr [rdi+4*r8+0x10], 25
       je       SHORT G_M000_IG37
 
G_M000_IG36:
       cmp      esi, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG43
       mov      edi, esi
       mov      dword ptr [rdx+4*rdi+0x10], 25
 
G_M000_IG37:
       test     r12, r12
       jle      SHORT G_M000_IG40
 
G_M000_IG38:
       movsxd   rcx, ecx
       cmp      r12, rcx
       jg       SHORT G_M000_IG40
 
G_M000_IG39:
       dec      r12d
       mov      dword ptr [(reloc)], r12d
 
G_M000_IG40:
       add      rsp, 264
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG41:
       test     r14, r14
       jl       SHORT G_M000_IG44
       mov      rdi, r14
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rsi]
       cmp      rdi, 262
       jae      SHORT G_M000_IG43
       mov      r13, gword ptr [rcx+8*rdi+0x10]
       jmp      G_M000_IG33
 
G_M000_IG42:
       mov      rax, gword ptr [r15+0x08]
       jmp      G_M000_IG34
 
G_M000_IG43:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG44:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 885

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_call_alias(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; optimized using Synthesized PGO
; rsp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 100
; No PGO data

G_M000_IG01:
       push     rax
 
G_M000_IG02:
       test     edx, edx
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rsi]
       call     [RyuPy.Generated.Module:m0_call_alias(System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 26

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_call_direct(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; optimized using Synthesized PGO
; rsp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 100
; No PGO data

G_M000_IG01:
       push     rax
 
G_M000_IG02:
       test     edx, edx
       je       SHORT G_M000_IG04
       mov      rdi, gword ptr [rsi]
       call     [RyuPy.Generated.Module:m0_call_direct(System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 26

