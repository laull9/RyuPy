; Assembly listing for method RyuPy.Generated.Module:m0_repeat_index(System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 288
       lea      rbp, [rsp+0x120]
       xor      eax, eax
       mov      qword ptr [rbp-0x10C], rax
       mov      dword ptr [rbp-0x104], eax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x100], zmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       vmovdqa  xmmword ptr [rbp-0x40], xmm8
       mov      gword ptr [rbp-0x30], rdi
 
G_M000_IG02:
       mov      dword ptr [rbp-0x118], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x48], rax
       mov      edi, 14
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:None():System.Object]
       mov      gword ptr [rbp-0x50], rax
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x38], rax
       mov      edi, 15
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
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x118]
       dec      eax
       mov      dword ptr [rbp-0x118], eax
       cmp      dword ptr [rbp-0x118], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x118]
       mov      esi, 86
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
       jmp      G_M000_IG08
 
G_M000_IG06:
       mov      rdi, qword ptr [rbp-0x70]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0xA8], rax
       mov      rax, gword ptr [rbp-0xA8]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 16
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rdi, gword ptr [rax]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyErrors:LoadName(System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xB0], rax
       mov      rax, gword ptr [rbp-0xB0]
       mov      gword ptr [rbp-0xC8], rax
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xD0], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rax, gword ptr [rbp-0xC8]
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x10C], eax
       cmp      dword ptr [rbp-0x10C], 0
       jne      G_M000_IG20
       jmp      G_M000_IG13
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x70]
       add      rax, qword ptr [rbp-0x80]
       jo       G_M000_IG23
       mov      qword ptr [rbp-0x70], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 17
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0xC0], rax
       mov      rdi, qword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xC0]
 
G_M000_IG09:
       add      rsp, 288
       pop      rbp
       ret      
 
G_M000_IG10:
       mov      eax, dword ptr [rbp-0x118]
       dec      eax
       mov      dword ptr [rbp-0x118], eax
       cmp      dword ptr [rbp-0x118], 0
       jg       SHORT G_M000_IG12
 
G_M000_IG11:
       lea      rdi, [rbp-0x118]
       mov      esi, 259
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG12:
       mov      rax, gword ptr [rbp-0xB8]
       mov      gword ptr [rbp-0x38], rax
       jmp      G_M000_IG07
 
G_M000_IG13:
       mov      eax, dword ptr [rbp-0x118]
       dec      eax
       mov      dword ptr [rbp-0x118], eax
       cmp      dword ptr [rbp-0x118], 0
       jg       SHORT G_M000_IG15
 
G_M000_IG14:
       lea      rdi, [rbp-0x118]
       mov      esi, 267
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG15:
       mov      rdx, gword ptr [rbp-0xD0]
       mov      rsi, gword ptr [rbp-0xC8]
       mov      edi, 2
       call     [RyuPy.Runtime.PyPic:CallValue1(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       jmp      G_M000_IG10
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       call     [RyuPy.Runtime.PyTraceback:CheckDepth()]
       mov      rdi, qword ptr [rbp-0xE8]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       jmp      G_M000_IG10
 
G_M000_IG17:
       mov      eax, dword ptr [rbp-0x118]
       dec      eax
       mov      dword ptr [rbp-0x118], eax
       cmp      dword ptr [rbp-0x118], 0
       jg       SHORT G_M000_IG19
 
G_M000_IG18:
       lea      rdi, [rbp-0x118]
       mov      esi, 303
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG19:
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xD8], rax
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xE0], rax
       mov      rax, qword ptr [rbp-0xD8]
       add      rax, qword ptr [rbp-0xE0]
       mov      qword ptr [rbp-0xE8], rax
       mov      rax, qword ptr [rbp-0xD8]
       xor      rax, qword ptr [rbp-0xE8]
       mov      qword ptr [rbp-0xF0], rax
       mov      rax, qword ptr [rbp-0xE0]
       xor      rax, qword ptr [rbp-0xE8]
       mov      qword ptr [rbp-0xF8], rax
       mov      rax, qword ptr [rbp-0xF0]
       and      rax, qword ptr [rbp-0xF8]
       mov      qword ptr [rbp-0x100], rax
       cmp      qword ptr [rbp-0x100], 0
       setge    al
       movzx    rax, al
       mov      dword ptr [rbp-0x104], eax
       cmp      dword ptr [rbp-0x104], 0
       jne      G_M000_IG16
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG20:
       mov      eax, dword ptr [rbp-0x118]
       dec      eax
       mov      dword ptr [rbp-0x118], eax
       cmp      dword ptr [rbp-0x118], 0
       jg       SHORT G_M000_IG22
 
G_M000_IG21:
       lea      rdi, [rbp-0x118]
       mov      esi, 379
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG22:
       mov      rsi, gword ptr [rbp-0xD0]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x108], eax
       cmp      dword ptr [rbp-0x108], 0
       jne      G_M000_IG17
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG23:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1198

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_index(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x103
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 1 inlinees with PGO data; 13 single block inlinees; 11 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 112
       mov      qword ptr [rsp+0x198], r15
       mov      qword ptr [rsp+0x190], r14
       mov      qword ptr [rsp+0x188], r13
       mov      qword ptr [rsp+0x180], r12
       mov      qword ptr [rsp+0x178], rbx
       lea      rbp, [rsp+0x70]
       mov      r13, qword ptr [rbp+0xE8]
       mov      rbx, qword ptr [rbp+0xC0]
       mov      r15, qword ptr [rbp+0xB8]
       mov      r14, qword ptr [rbp+0xB0]
       mov      rax, gword ptr [rbp+0x78]
 
G_M000_IG02:
       test     r14, r14
       setg     r12b
       movzx    r12, r12b
       jmp      G_M000_IG47
 
G_M000_IG03:
       test     rcx, rcx
       jl       G_M000_IG65
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG64
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      G_M000_IG14
 
G_M000_IG04:
       test     rax, rax
       jl       G_M000_IG65
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG64
       mov      rax, gword ptr [rax+8*rdi+0x10]
       mov      gword ptr [rbp-0x38], rax
       mov      rax, gword ptr [rbp-0x38]
       jmp      G_M000_IG50
 
G_M000_IG05:
       mov      gword ptr [rbp-0x38], rax
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ThrowUnboundGlobal(System.String)]
 
G_M000_IG07:
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp+0x68], rax
       mov      rcx, 0xD1FFAB1E
       cmp      rax, gword ptr [rcx]
       jne      G_M000_IG32
 
G_M000_IG08:
       mov      rcx, gword ptr [rbp-0x38]
       test     rcx, rcx
       je       G_M000_IG31
 
G_M000_IG09:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       je       SHORT G_M000_IG11
 
G_M000_IG10:
       mov      gword ptr [rbp-0x38], rcx
       jmp      G_M000_IG32
 
G_M000_IG11:
       mov      rdx, qword ptr [rcx+0x08]
       mov      rdi, rdx
       add      rdx, rdi
       mov      qword ptr [rbp+0x48], rdx
       xor      rdi, rdx
       mov      gword ptr [rbp-0x38], rcx
       jl       G_M000_IG32
 
G_M000_IG12:
       call     [RyuPy.Runtime.PyTraceback:CheckDepth()]
       mov      rax, qword ptr [rbp+0x48]
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG03
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp+0x48]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG14:
       jmp      G_M000_IG47
 
G_M000_IG15:
       mov      r9d, 3
       jmp      G_M000_IG40
 
G_M000_IG16:
       mov      r9d, 2
       jmp      G_M000_IG40
 
G_M000_IG17:
       mov      r9d, 1
       jmp      G_M000_IG40
 
G_M000_IG18:
       cmp      gword ptr [r8], rax
       jne      G_M000_IG41
 
G_M000_IG19:
       cmp      r9d, 1
       je       SHORT G_M000_IG28
 
G_M000_IG20:
       cmp      r9d, 3
       jne      G_M000_IG44
 
G_M000_IG21:
       mov      rdi, rax
       test     rdi, rdi
       je       SHORT G_M000_IG23
 
G_M000_IG22:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rdi], r8
       jne      G_M000_IG63
 
G_M000_IG23:
       mov      rcx, 0xD1FFAB1E
       mov      r8, gword ptr [rcx]
       mov      rcx, r8
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, gword ptr [rbp-0x48]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyClass:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       jmp      G_M000_IG47
 
G_M000_IG24:
       cmp      gword ptr [r8], rax
       jne      G_M000_IG42
       jmp      SHORT G_M000_IG19
 
G_M000_IG25:
       cmp      gword ptr [r8], rax
       jne      G_M000_IG43
       jmp      SHORT G_M000_IG19
 
G_M000_IG26:
       cmp      gword ptr [r8], rax
       jne      G_M000_IG44
 
G_M000_IG27:
       mov      r10, gword ptr [rbp-0x68]
       jmp      SHORT G_M000_IG19
 
G_M000_IG28:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG30
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG62
 
G_M000_IG30:
       mov      rax, gword ptr [rsi+0x18]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rdx, gword ptr [rbp-0x48]
       mov      rdi, gword ptr [rax+0x08]
       call     [rax+0x18]RyuPy.Runtime.PyCall:Invoke(RyuPy.Runtime.PyFunction,System.Object[],System.String[],System.Object[]):System.Object:this
       jmp      G_M000_IG47
 
G_M000_IG31:
       mov      gword ptr [rbp-0x38], rcx
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp-0x70], rcx
       mov      rdx, gword ptr [rcx+0x20]
       test     rdx, rdx
       je       SHORT G_M000_IG34
 
G_M000_IG33:
       cmp      dword ptr [rdx+0x08], 1
       mov      gword ptr [rbp-0x50], rdx
       je       SHORT G_M000_IG35
 
G_M000_IG34:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x70]
       lea      rdi, bword ptr [rdi+0x20]
       mov      gword ptr [rbp-0x50], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG35:
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x48], rax
       mov      rdi, rax
       mov      rdx, gword ptr [rbp-0x38]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rax, gword ptr [rbp+0x68]
       test     rax, rax
       je       SHORT G_M000_IG39
 
G_M000_IG36:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rax], r8
       je       G_M000_IG17
 
G_M000_IG37:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rax], r8
       je       G_M000_IG16
 
G_M000_IG38:
       mov      r8, 0xD1FFAB1E
       cmp      qword ptr [rax], r8
       je       G_M000_IG15
 
G_M000_IG39:
       xor      r9d, r9d
 
G_M000_IG40:
       mov      dword ptr [rbp-0x2C], r9d
       mov      r8, 0xD1FFAB1E
       mov      r10, gword ptr [r8]
       mov      gword ptr [rbp-0x68], r10
       lea      r8, bword ptr [r10+0x90]
       cmp      byte  ptr [r8+0x08], 0
       jne      G_M000_IG18
 
G_M000_IG41:
       lea      r8, bword ptr [r10+0xA0]
       cmp      byte  ptr [r8+0x08], 0
       jne      G_M000_IG24
 
G_M000_IG42:
       lea      r8, bword ptr [r10+0xB0]
       cmp      byte  ptr [r8+0x08], 0
       jne      G_M000_IG25
 
G_M000_IG43:
       lea      r8, bword ptr [r10+0xC0]
       cmp      byte  ptr [r8+0x08], 0
       jne      G_M000_IG26
 
G_M000_IG44:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rcx, r8
       mov      edi, r9d
       mov      rsi, rax
       mov      rdx, gword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyPic:InvokeCached(byte,System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x58], rax
       cmp      dword ptr [rbp-0x2C], 0
       je       SHORT G_M000_IG46
 
G_M000_IG45:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       add      rdi, 18
       movzx    rsi, byte  ptr [rdi]
       lea      edx, [rsi+0x01]
       mov      byte  ptr [rdi], dl
       and      esi, 3
       add      esi, 8
       mov      edi, esi
       shl      rdi, 4
       mov      rsi, gword ptr [rbp-0x68]
       lea      rdx, bword ptr [rsi+rdi+0x10]
       mov      bword ptr [rbp-0x60], rdx
       mov      rsi, gword ptr [rbp+0x68]
       mov      rdi, rdx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdi, bword ptr [rbp-0x60]
       mov      byte  ptr [rdi+0x08], 1
       mov      ecx, dword ptr [rbp-0x2C]
       mov      byte  ptr [rdi+0x09], cl
 
G_M000_IG46:
       mov      rax, gword ptr [rbp-0x58]
 
G_M000_IG47:
       add      rbx, r14
       jo       G_M000_IG65
       mov      edi, r12d
       cmp      rbx, r15
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       cmp      rbx, r15
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG55
 
G_M000_IG48:
       lea      rax, [rbx+0x05]
       cmp      rax, 261
       jbe      G_M000_IG04
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], rbx
 
G_M000_IG50:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG53
 
G_M000_IG51:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG64
       cmp      dword ptr [rdx+4*rsi+0x10], 16
       je       SHORT G_M000_IG53
 
G_M000_IG52:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG64
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 16
 
G_M000_IG53:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp-0x40], rcx
       test     rcx, rcx
       je       G_M000_IG05
 
G_M000_IG54:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       mov      gword ptr [rbp-0x38], rax
       jne      G_M000_IG07
       jmp      G_M000_IG06
 
G_M000_IG55:
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG58
 
G_M000_IG56:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG64
       cmp      dword ptr [rdi+4*r8+0x10], 17
       je       SHORT G_M000_IG58
 
G_M000_IG57:
       cmp      esi, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG64
       mov      edi, esi
       mov      dword ptr [rdx+4*rdi+0x10], 17
 
G_M000_IG58:
       test     r13, r13
       jle      SHORT G_M000_IG61
 
G_M000_IG59:
       movsxd   rcx, ecx
       cmp      r13, rcx
       jg       SHORT G_M000_IG61
 
G_M000_IG60:
       dec      r13d
       mov      dword ptr [(reloc)], r13d
 
G_M000_IG61:
       add      rsp, 376
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG62:
       mov      rsi, rax
       call     [CORINFO_HELP_CHKCASTCLASS]
       int3     
 
G_M000_IG63:
       mov      rsi, rax
       mov      rdi, r8
       call     [CORINFO_HELP_CHKCASTCLASS]
       int3     
 
G_M000_IG64:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG65:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1343

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_index(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x56
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 1 inlinees with PGO data; 13 single block inlinees; 11 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 112
       mov      qword ptr [rsp+0x198], r15
       mov      qword ptr [rsp+0x190], r14
       mov      qword ptr [rsp+0x188], r13
       mov      qword ptr [rsp+0x180], r12
       mov      qword ptr [rsp+0x178], rbx
       lea      rbp, [rsp+0x70]
       mov      rax, gword ptr [rbp+0xF8]
       mov      r13, qword ptr [rbp+0xE8]
       mov      rbx, qword ptr [rbp+0xC0]
       mov      r15, qword ptr [rbp+0xB8]
       mov      r14, qword ptr [rbp+0xB0]
 
G_M000_IG02:
       test     r14, r14
       setg     r12b
       movzx    r12, r12b
       jmp      G_M000_IG21
 
G_M000_IG03:
       test     rcx, rcx
       jl       G_M000_IG60
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG59
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      G_M000_IG35
 
G_M000_IG04:
       test     rax, rax
       jl       G_M000_IG60
       mov      rdi, rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG59
       mov      rax, gword ptr [rax+8*rdi+0x10]
       mov      gword ptr [rbp-0x38], rax
       mov      rax, gword ptr [rbp-0x38]
       jmp      G_M000_IG24
 
G_M000_IG05:
       mov      gword ptr [rbp-0x38], rax
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ThrowUnboundGlobal(System.String)]
 
G_M000_IG07:
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp+0x68], rax
       mov      rcx, 0xD1FFAB1E
       cmp      rax, gword ptr [rcx]
       je       G_M000_IG29
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp-0x68], rcx
       mov      rdx, gword ptr [rcx+0x20]
       test     rdx, rdx
       je       SHORT G_M000_IG10
 
G_M000_IG09:
       cmp      dword ptr [rdx+0x08], 1
       mov      gword ptr [rbp-0x50], rdx
       je       SHORT G_M000_IG11
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x68]
       lea      rdi, bword ptr [rdi+0x20]
       mov      gword ptr [rbp-0x50], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG11:
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x48], rax
       mov      rdi, rax
       mov      rdx, gword ptr [rbp-0x38]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rax, gword ptr [rbp+0x68]
       test     rax, rax
       je       SHORT G_M000_IG14
 
G_M000_IG12:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG37
 
G_M000_IG13:
       mov      r9d, 1
       jmp      SHORT G_M000_IG15
 
G_M000_IG14:
       xor      r9d, r9d
 
G_M000_IG15:
       mov      dword ptr [rbp-0x2C], r9d
       mov      rcx, 0xD1FFAB1E
       mov      r10, gword ptr [rcx]
       mov      gword ptr [rbp-0x70], r10
       lea      rcx, bword ptr [r10+0x90]
       cmp      byte  ptr [rcx+0x08], 0
       je       G_M000_IG41
 
G_M000_IG16:
       cmp      gword ptr [rcx], rax
       jne      G_M000_IG41
 
G_M000_IG17:
       cmp      r9d, 1
       jne      G_M000_IG56
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG19
 
G_M000_IG18:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG55
 
G_M000_IG19:
       mov      rax, gword ptr [rsi+0x18]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rdx, gword ptr [rbp-0x48]
       mov      rdi, gword ptr [rax+0x08]
       call     [rax+0x18]RyuPy.Runtime.PyCall:Invoke(RyuPy.Runtime.PyFunction,System.Object[],System.String[],System.Object[]):System.Object:this
 
G_M000_IG20:
       add      rbx, r14
       jo       G_M000_IG60
 
G_M000_IG21:
       mov      edi, r12d
       cmp      rbx, r15
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       cmp      rbx, r15
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG43
 
G_M000_IG22:
       lea      rax, [rbx+0x05]
       cmp      rax, 261
       jbe      G_M000_IG04
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], rbx
 
G_M000_IG24:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG27
 
G_M000_IG25:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG59
       cmp      dword ptr [rdx+4*rsi+0x10], 16
       je       SHORT G_M000_IG27
 
G_M000_IG26:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG59
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 16
 
G_M000_IG27:
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       mov      gword ptr [rbp-0x40], rcx
       test     rcx, rcx
       je       G_M000_IG05
 
G_M000_IG28:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       mov      gword ptr [rbp-0x38], rax
       jne      G_M000_IG07
       jmp      G_M000_IG06
 
G_M000_IG29:
       mov      rcx, gword ptr [rbp-0x38]
       test     rcx, rcx
       je       SHORT G_M000_IG36
 
G_M000_IG30:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       je       SHORT G_M000_IG32
 
G_M000_IG31:
       mov      gword ptr [rbp-0x38], rcx
       jmp      G_M000_IG08
 
G_M000_IG32:
       mov      rdx, qword ptr [rcx+0x08]
       mov      rdi, rdx
       add      rdx, rdi
       mov      qword ptr [rbp+0x48], rdx
       xor      rdi, rdx
       mov      gword ptr [rbp-0x38], rcx
       jl       G_M000_IG08
 
G_M000_IG33:
       call     [RyuPy.Runtime.PyTraceback:CheckDepth()]
       mov      rax, qword ptr [rbp+0x48]
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG03
 
G_M000_IG34:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp+0x48]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG35:
       jmp      G_M000_IG20
 
G_M000_IG36:
       mov      gword ptr [rbp-0x38], rcx
       jmp      G_M000_IG08
 
G_M000_IG37:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       SHORT G_M000_IG40
 
G_M000_IG38:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       jne      G_M000_IG14
 
G_M000_IG39:
       mov      r9d, 3
       jmp      G_M000_IG15
 
G_M000_IG40:
       mov      r9d, 2
       jmp      G_M000_IG15
 
G_M000_IG41:
       lea      rcx, bword ptr [r10+0xA0]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG50
 
G_M000_IG42:
       cmp      gword ptr [rcx], rax
       jne      SHORT G_M000_IG50
       jmp      G_M000_IG17
 
G_M000_IG43:
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG46
 
G_M000_IG44:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdi+0x08]
       jae      G_M000_IG59
       cmp      dword ptr [rdi+4*r8+0x10], 17
       je       SHORT G_M000_IG46
 
G_M000_IG45:
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG59
       mov      edi, esi
       mov      dword ptr [rdx+4*rdi+0x10], 17
 
G_M000_IG46:
       test     r13, r13
       jle      SHORT G_M000_IG49
 
G_M000_IG47:
       movsxd   rcx, ecx
       cmp      r13, rcx
       jg       SHORT G_M000_IG49
 
G_M000_IG48:
       dec      r13d
       mov      dword ptr [(reloc)], r13d
 
G_M000_IG49:
       add      rsp, 376
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG50:
       lea      rcx, bword ptr [r10+0xB0]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG52
 
G_M000_IG51:
       cmp      gword ptr [rcx], rax
       je       G_M000_IG17
 
G_M000_IG52:
       lea      rcx, bword ptr [r10+0xC0]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG54
 
G_M000_IG53:
       cmp      gword ptr [rcx], rax
       jne      SHORT G_M000_IG54
       mov      r10, gword ptr [rbp-0x70]
       jmp      G_M000_IG17
 
G_M000_IG54:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      edi, r9d
       mov      rsi, rax
       mov      rdx, gword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyPic:InvokeCached(byte,System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x58], rax
       cmp      dword ptr [rbp-0x2C], 0
       je       G_M000_IG58
       jmp      SHORT G_M000_IG57
 
G_M000_IG55:
       mov      rsi, rax
       call     [CORINFO_HELP_CHKCASTCLASS]
       int3     
 
G_M000_IG56:
       cmp      r9d, 3
       jne      SHORT G_M000_IG54
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, gword ptr [rbp-0x48]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyClass:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       jmp      G_M000_IG20
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       add      rdi, 18
       movzx    rsi, byte  ptr [rdi]
       lea      edx, [rsi+0x01]
       mov      byte  ptr [rdi], dl
       and      esi, 3
       add      esi, 8
       mov      edi, esi
       shl      rdi, 4
       mov      rsi, gword ptr [rbp-0x70]
       lea      rdx, bword ptr [rsi+rdi+0x10]
       mov      bword ptr [rbp-0x60], rdx
       mov      rsi, gword ptr [rbp+0x68]
       mov      rdi, rdx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, bword ptr [rbp-0x60]
       mov      byte  ptr [rsi+0x08], 1
       mov      edi, dword ptr [rbp-0x2C]
       mov      byte  ptr [rsi+0x09], dil
 
G_M000_IG58:
       mov      rax, gword ptr [rbp-0x58]
       jmp      G_M000_IG20
 
G_M000_IG59:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG60:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1324

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_constant(System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 288
       lea      rbp, [rsp+0x120]
       xor      eax, eax
       mov      dword ptr [rbp-0x114], eax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x110], zmm8
       vmovdqu32 zmmword ptr [rbp-0xD0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       vmovdqu  ymmword ptr [rbp-0x50], ymm8
       mov      gword ptr [rbp-0x30], rdi
 
G_M000_IG02:
       mov      dword ptr [rbp-0x120], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x48], rax
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       call     [RyuPy.Runtime.PyOps:None():System.Object]
       mov      gword ptr [rbp-0x50], rax
       mov      rax, gword ptr [rbp-0x50]
       mov      gword ptr [rbp-0x38], rax
       mov      edi, 21
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
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0x120]
       mov      esi, 86
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
       jmp      G_M000_IG08
 
G_M000_IG06:
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rdi, gword ptr [rax]
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyErrors:LoadName(System.Object,System.String):System.Object]
       mov      gword ptr [rbp-0xB0], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xB8], rax
       mov      rax, gword ptr [rbp-0xB0]
       mov      gword ptr [rbp-0xD0], rax
       mov      rax, gword ptr [rbp-0xB8]
       mov      gword ptr [rbp-0xD8], rax
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rax, gword ptr [rbp-0xD0]
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x114], eax
       cmp      dword ptr [rbp-0x114], 0
       jne      G_M000_IG20
       jmp      G_M000_IG13
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x70]
       add      rax, qword ptr [rbp-0x80]
       jo       G_M000_IG23
       mov      qword ptr [rbp-0x70], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 23
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x38]
       mov      gword ptr [rbp-0xC8], rax
       mov      rdi, qword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xC8]
 
G_M000_IG09:
       add      rsp, 288
       pop      rbp
       ret      
 
G_M000_IG10:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG12
 
G_M000_IG11:
       lea      rdi, [rbp-0x120]
       mov      esi, 255
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG12:
       mov      rax, gword ptr [rbp-0xC0]
       mov      gword ptr [rbp-0x38], rax
       jmp      G_M000_IG07
 
G_M000_IG13:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG15
 
G_M000_IG14:
       lea      rdi, [rbp-0x120]
       mov      esi, 263
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG15:
       mov      rdx, gword ptr [rbp-0xD8]
       mov      rsi, gword ptr [rbp-0xD0]
       mov      edi, 3
       call     [RyuPy.Runtime.PyPic:CallValue1(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xC0], rax
       jmp      G_M000_IG10
 
G_M000_IG16:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       call     [RyuPy.Runtime.PyTraceback:CheckDepth()]
       mov      rdi, qword ptr [rbp-0xF0]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0xC0], rax
       jmp      G_M000_IG10
 
G_M000_IG17:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG19
 
G_M000_IG18:
       lea      rdi, [rbp-0x120]
       mov      esi, 299
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG19:
       mov      rsi, gword ptr [rbp-0xD8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xE0], rax
       mov      rsi, gword ptr [rbp-0xD8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0xE8], rax
       mov      rax, qword ptr [rbp-0xE0]
       add      rax, qword ptr [rbp-0xE8]
       mov      qword ptr [rbp-0xF0], rax
       mov      rax, qword ptr [rbp-0xE0]
       xor      rax, qword ptr [rbp-0xF0]
       mov      qword ptr [rbp-0xF8], rax
       mov      rax, qword ptr [rbp-0xE8]
       xor      rax, qword ptr [rbp-0xF0]
       mov      qword ptr [rbp-0x100], rax
       mov      rax, qword ptr [rbp-0xF8]
       and      rax, qword ptr [rbp-0x100]
       mov      qword ptr [rbp-0x108], rax
       cmp      qword ptr [rbp-0x108], 0
       setge    al
       movzx    rax, al
       mov      dword ptr [rbp-0x10C], eax
       cmp      dword ptr [rbp-0x10C], 0
       jne      G_M000_IG16
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG20:
       mov      eax, dword ptr [rbp-0x120]
       dec      eax
       mov      dword ptr [rbp-0x120], eax
       cmp      dword ptr [rbp-0x120], 0
       jg       SHORT G_M000_IG22
 
G_M000_IG21:
       lea      rdi, [rbp-0x120]
       mov      esi, 375
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG22:
       mov      rsi, gword ptr [rbp-0xD8]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_ISINSTANCEOFCLASS]
       test     rax, rax
       setne    al
       movzx    rax, al
       mov      dword ptr [rbp-0x110], eax
       cmp      dword ptr [rbp-0x110], 0
       jne      G_M000_IG17
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       jmp      G_M000_IG13
 
G_M000_IG23:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1195

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_constant(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0xff
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 1 inlinees with PGO data; 12 single block inlinees; 10 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 112
       mov      qword ptr [rsp+0x198], r15
       mov      qword ptr [rsp+0x190], r14
       mov      qword ptr [rsp+0x188], r13
       mov      qword ptr [rsp+0x180], r12
       mov      qword ptr [rsp+0x178], rbx
       lea      rbp, [rsp+0x70]
       mov      r13, qword ptr [rbp+0xE8]
       mov      rbx, qword ptr [rbp+0xC0]
       mov      r15, qword ptr [rbp+0xB8]
       mov      r14, qword ptr [rbp+0xB0]
       mov      rax, gword ptr [rbp+0x70]
 
G_M000_IG02:
       test     r14, r14
       setg     r12b
       movzx    r12, r12b
       jmp      G_M000_IG25
 
G_M000_IG03:
       test     rcx, rcx
       jl       G_M000_IG54
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG53
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      G_M000_IG11
 
G_M000_IG04:
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ThrowUnboundGlobal(System.String)]
 
G_M000_IG05:
       mov      rax, gword ptr [rbp-0x38]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      gword ptr [rbp+0x78], rcx
       mov      gword ptr [rbp+0x60], rax
       mov      rdx, 0xD1FFAB1E
       cmp      rax, gword ptr [rdx]
       jne      SHORT G_M000_IG12
 
G_M000_IG06:
       test     rcx, rcx
       je       SHORT G_M000_IG12
 
G_M000_IG07:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      SHORT G_M000_IG12
 
G_M000_IG08:
       mov      rdx, qword ptr [rcx+0x08]
       mov      rdi, rdx
       add      rdx, rdi
       mov      qword ptr [rbp+0x40], rdx
       xor      rdi, rdx
       mov      rcx, gword ptr [rbp+0x78]
       jl       SHORT G_M000_IG12
 
G_M000_IG09:
       call     [RyuPy.Runtime.PyTraceback:CheckDepth()]
       mov      rax, qword ptr [rbp+0x40]
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      G_M000_IG03
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp+0x40]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG11:
       jmp      G_M000_IG25
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      gword ptr [rbp-0x60], rdx
       mov      r8, gword ptr [rdx+0x28]
       test     r8, r8
       je       SHORT G_M000_IG14
 
G_M000_IG13:
       cmp      dword ptr [r8+0x08], 1
       mov      gword ptr [rbp-0x48], r8
       je       SHORT G_M000_IG15
       jmp      SHORT G_M000_IG14
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x60]
       lea      rdi, bword ptr [rdi+0x28]
       mov      gword ptr [rbp-0x48], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG15:
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x40], rax
       mov      rdi, rax
       mov      rdx, gword ptr [rbp+0x78]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rax, gword ptr [rbp+0x60]
       test     rax, rax
       je       SHORT G_M000_IG19
 
G_M000_IG16:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG33
 
G_M000_IG17:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG32
 
G_M000_IG18:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG31
 
G_M000_IG19:
       xor      r9d, r9d
 
G_M000_IG20:
       mov      dword ptr [rbp-0x2C], r9d
       mov      rcx, 0xD1FFAB1E
       mov      r10, gword ptr [rcx]
       mov      gword ptr [rbp-0x68], r10
       lea      rcx, bword ptr [r10+0xD0]
       cmp      byte  ptr [rcx+0x08], 0
       je       G_M000_IG34
 
G_M000_IG21:
       cmp      gword ptr [rcx], rax
       jne      G_M000_IG34
 
G_M000_IG22:
       cmp      r9d, 1
       jne      G_M000_IG50
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG24
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG49
 
G_M000_IG24:
       mov      rax, gword ptr [rsi+0x18]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rdx, gword ptr [rbp-0x40]
       mov      rdi, gword ptr [rax+0x08]
       call     [rax+0x18]RyuPy.Runtime.PyCall:Invoke(RyuPy.Runtime.PyFunction,System.Object[],System.String[],System.Object[]):System.Object:this
 
G_M000_IG25:
       add      rbx, r14
       jo       G_M000_IG54
       mov      edi, r12d
       cmp      rbx, r15
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       cmp      rbx, r15
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG36
 
G_M000_IG26:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG29
 
G_M000_IG27:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG53
       cmp      dword ptr [rcx+4*rdx+0x10], 22
       je       SHORT G_M000_IG29
 
G_M000_IG28:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG53
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 22
 
G_M000_IG29:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x38], rax
       test     rax, rax
       je       G_M000_IG04
 
G_M000_IG30:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG05
       jmp      G_M000_IG04
 
G_M000_IG31:
       mov      r9d, 3
       jmp      G_M000_IG20
 
G_M000_IG32:
       mov      r9d, 2
       jmp      G_M000_IG20
 
G_M000_IG33:
       mov      r9d, 1
       jmp      G_M000_IG20
 
G_M000_IG34:
       lea      rcx, bword ptr [r10+0xE0]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG43
 
G_M000_IG35:
       cmp      gword ptr [rcx], rax
       jne      SHORT G_M000_IG43
       jmp      G_M000_IG22
 
G_M000_IG36:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG39
 
G_M000_IG37:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      esi, [rdi-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG53
       cmp      dword ptr [rdx+4*r8+0x10], 23
       je       SHORT G_M000_IG39
 
G_M000_IG38:
       mov      edx, esi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG53
       mov      dword ptr [rcx+4*rdx+0x10], 23
 
G_M000_IG39:
       test     r13, r13
       jle      SHORT G_M000_IG42
 
G_M000_IG40:
       movsxd   rcx, edi
       cmp      r13, rcx
       jg       SHORT G_M000_IG42
 
G_M000_IG41:
       dec      r13d
       mov      dword ptr [(reloc)], r13d
 
G_M000_IG42:
       add      rsp, 376
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG43:
       lea      rcx, bword ptr [r10+0xF0]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG45
 
G_M000_IG44:
       cmp      gword ptr [rcx], rax
       je       G_M000_IG22
 
G_M000_IG45:
       lea      rcx, bword ptr [r10+0x100]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG48
 
G_M000_IG46:
       cmp      gword ptr [rcx], rax
       jne      SHORT G_M000_IG48
 
G_M000_IG47:
       mov      r10, gword ptr [rbp-0x68]
       jmp      G_M000_IG22
 
G_M000_IG48:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      edi, r9d
       mov      rsi, rax
       mov      rdx, gword ptr [rbp-0x40]
       call     [RyuPy.Runtime.PyPic:InvokeCached(byte,System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x50], rax
       cmp      dword ptr [rbp-0x2C], 0
       je       G_M000_IG52
       jmp      SHORT G_M000_IG51
 
G_M000_IG49:
       mov      rsi, rax
       call     [CORINFO_HELP_CHKCASTCLASS]
       int3     
 
G_M000_IG50:
       cmp      r9d, 3
       jne      SHORT G_M000_IG48
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, gword ptr [rbp-0x40]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyClass:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       jmp      G_M000_IG25
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       add      rdi, 19
       movzx    rsi, byte  ptr [rdi]
       lea      edx, [rsi+0x01]
       mov      byte  ptr [rdi], dl
       and      esi, 3
       add      esi, 12
       mov      edi, esi
       shl      rdi, 4
       mov      rsi, gword ptr [rbp-0x68]
       lea      rdx, bword ptr [rsi+rdi+0x10]
       mov      bword ptr [rbp-0x58], rdx
       mov      rsi, gword ptr [rbp+0x60]
       mov      rdi, rdx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, bword ptr [rbp-0x58]
       mov      byte  ptr [rsi+0x08], 1
       mov      edi, dword ptr [rbp-0x2C]
       mov      byte  ptr [rsi+0x09], dil
 
G_M000_IG52:
       mov      rax, gword ptr [rbp-0x50]
       jmp      G_M000_IG25
 
G_M000_IG53:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG54:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1219

; Assembly listing for method RyuPy.Generated.Module:m0_repeat_constant(System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x56
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 1 inlinees with PGO data; 12 single block inlinees; 10 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 112
       mov      qword ptr [rsp+0x198], r15
       mov      qword ptr [rsp+0x190], r14
       mov      qword ptr [rsp+0x188], r13
       mov      qword ptr [rsp+0x180], r12
       mov      qword ptr [rsp+0x178], rbx
       lea      rbp, [rsp+0x70]
       mov      rax, gword ptr [rbp+0xF8]
       mov      r13, qword ptr [rbp+0xE8]
       mov      rbx, qword ptr [rbp+0xC0]
       mov      r15, qword ptr [rbp+0xB8]
       mov      r14, qword ptr [rbp+0xB0]
 
G_M000_IG02:
       test     r14, r14
       setg     r12b
       movzx    r12, r12b
       jmp      G_M000_IG26
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:ThrowUnboundGlobal(System.String)]
 
G_M000_IG04:
       mov      rax, gword ptr [rbp-0x38]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      gword ptr [rbp+0x78], rcx
       mov      gword ptr [rbp+0x60], rax
       mov      rdx, 0xD1FFAB1E
       cmp      rax, gword ptr [rdx]
       je       SHORT G_M000_IG07
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      gword ptr [rbp-0x60], rdx
       mov      r8, gword ptr [rdx+0x28]
       test     r8, r8
       je       G_M000_IG14
 
G_M000_IG06:
       cmp      dword ptr [r8+0x08], 1
       mov      gword ptr [rbp-0x48], r8
       je       G_M000_IG15
       jmp      G_M000_IG14
 
G_M000_IG07:
       test     rcx, rcx
       je       SHORT G_M000_IG05
 
G_M000_IG08:
       mov      rdx, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdx
       jne      SHORT G_M000_IG05
 
G_M000_IG09:
       mov      rdx, qword ptr [rcx+0x08]
       mov      rdi, rdx
       add      rdx, rdi
       mov      qword ptr [rbp+0x40], rdx
       xor      rdi, rdx
       mov      rcx, gword ptr [rbp+0x78]
       jl       SHORT G_M000_IG05
 
G_M000_IG10:
       call     [RyuPy.Runtime.PyTraceback:CheckDepth()]
       mov      rax, qword ptr [rbp+0x40]
       lea      rcx, [rax+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG13
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp+0x40]
       mov      qword ptr [rax+0x08], rdi
 
G_M000_IG12:
       jmp      G_M000_IG25
 
G_M000_IG13:
       test     rcx, rcx
       jl       G_M000_IG55
       mov      rax, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       cmp      rax, 262
       jae      G_M000_IG54
       mov      rax, gword ptr [rdi+8*rax+0x10]
       jmp      SHORT G_M000_IG12
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      rdi, gword ptr [rbp-0x60]
       lea      rdi, bword ptr [rdi+0x28]
       mov      gword ptr [rbp-0x48], rax
       mov      rsi, rax
       call     CORINFO_HELP_ASSIGN_REF
 
G_M000_IG15:
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x40], rax
       mov      rdi, rax
       mov      rdx, gword ptr [rbp+0x78]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       mov      rax, gword ptr [rbp+0x60]
       test     rax, rax
       je       SHORT G_M000_IG19
 
G_M000_IG16:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG34
 
G_M000_IG17:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG33
 
G_M000_IG18:
       mov      rcx, 0xD1FFAB1E
       cmp      qword ptr [rax], rcx
       je       G_M000_IG32
 
G_M000_IG19:
       xor      r9d, r9d
 
G_M000_IG20:
       mov      dword ptr [rbp-0x2C], r9d
       mov      rcx, 0xD1FFAB1E
       mov      r10, gword ptr [rcx]
       mov      gword ptr [rbp-0x68], r10
       lea      rcx, bword ptr [r10+0xD0]
       cmp      byte  ptr [rcx+0x08], 0
       je       G_M000_IG35
 
G_M000_IG21:
       cmp      gword ptr [rcx], rax
       jne      G_M000_IG35
 
G_M000_IG22:
       cmp      r9d, 1
       jne      G_M000_IG51
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG24
 
G_M000_IG23:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       jne      G_M000_IG50
 
G_M000_IG24:
       mov      rax, gword ptr [rsi+0x18]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rdx, gword ptr [rbp-0x40]
       mov      rdi, gword ptr [rax+0x08]
       call     [rax+0x18]RyuPy.Runtime.PyCall:Invoke(RyuPy.Runtime.PyFunction,System.Object[],System.String[],System.Object[]):System.Object:this
 
G_M000_IG25:
       add      rbx, r14
       jo       G_M000_IG55
 
G_M000_IG26:
       mov      edi, r12d
       cmp      rbx, r15
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       cmp      rbx, r15
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG37
 
G_M000_IG27:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG30
 
G_M000_IG28:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG54
       cmp      dword ptr [rcx+4*rdx+0x10], 22
       je       SHORT G_M000_IG30
 
G_M000_IG29:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG54
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 22
 
G_M000_IG30:
       mov      rdi, 0xD1FFAB1E
       mov      rax, gword ptr [rdi]
       mov      gword ptr [rbp-0x38], rax
       test     rax, rax
       je       G_M000_IG03
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG04
       jmp      G_M000_IG03
 
G_M000_IG32:
       mov      r9d, 3
       jmp      G_M000_IG20
 
G_M000_IG33:
       mov      r9d, 2
       jmp      G_M000_IG20
 
G_M000_IG34:
       mov      r9d, 1
       jmp      G_M000_IG20
 
G_M000_IG35:
       lea      rcx, bword ptr [r10+0xE0]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG44
 
G_M000_IG36:
       cmp      gword ptr [rcx], rax
       jne      SHORT G_M000_IG44
       jmp      G_M000_IG22
 
G_M000_IG37:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG40
 
G_M000_IG38:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       lea      esi, [rdi-0x01]
       mov      r8d, esi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG54
       cmp      dword ptr [rdx+4*r8+0x10], 23
       je       SHORT G_M000_IG40
 
G_M000_IG39:
       mov      edx, esi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG54
       mov      dword ptr [rcx+4*rdx+0x10], 23
 
G_M000_IG40:
       test     r13, r13
       jle      SHORT G_M000_IG43
 
G_M000_IG41:
       movsxd   rcx, edi
       cmp      r13, rcx
       jg       SHORT G_M000_IG43
 
G_M000_IG42:
       dec      r13d
       mov      dword ptr [(reloc)], r13d
 
G_M000_IG43:
       add      rsp, 376
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG44:
       lea      rcx, bword ptr [r10+0xF0]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG46
 
G_M000_IG45:
       cmp      gword ptr [rcx], rax
       je       G_M000_IG22
 
G_M000_IG46:
       lea      rcx, bword ptr [r10+0x100]
       cmp      byte  ptr [rcx+0x08], 0
       je       SHORT G_M000_IG49
 
G_M000_IG47:
       cmp      gword ptr [rcx], rax
       jne      SHORT G_M000_IG49
 
G_M000_IG48:
       mov      r10, gword ptr [rbp-0x68]
       jmp      G_M000_IG22
 
G_M000_IG49:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      edi, r9d
       mov      rsi, rax
       mov      rdx, gword ptr [rbp-0x40]
       call     [RyuPy.Runtime.PyPic:InvokeCached(byte,System.Object,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x50], rax
       cmp      dword ptr [rbp-0x2C], 0
       je       G_M000_IG53
       jmp      SHORT G_M000_IG52
 
G_M000_IG50:
       mov      rsi, rax
       call     [CORINFO_HELP_CHKCASTCLASS]
       int3     
 
G_M000_IG51:
       cmp      r9d, 3
       jne      SHORT G_M000_IG49
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_CHKCASTCLASS]
       mov      rdi, rax
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, gword ptr [rbp-0x40]
       cmp      dword ptr [rdi], edi
       call     [RyuPy.Runtime.PyClass:Invoke(System.Object[],System.String[],System.Object[]):System.Object:this]
       jmp      G_M000_IG25
 
G_M000_IG52:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       add      rdi, 19
       movzx    rsi, byte  ptr [rdi]
       lea      edx, [rsi+0x01]
       mov      byte  ptr [rdi], dl
       and      esi, 3
       add      esi, 12
       mov      edi, esi
       shl      rdi, 4
       mov      rsi, gword ptr [rbp-0x68]
       lea      rdx, bword ptr [rsi+rdi+0x10]
       mov      bword ptr [rbp-0x58], rdx
       mov      rsi, gword ptr [rbp+0x60]
       mov      rdi, rdx
       call     CORINFO_HELP_ASSIGN_REF
       mov      rsi, bword ptr [rbp-0x58]
       mov      byte  ptr [rsi+0x08], 1
       mov      edi, dword ptr [rbp-0x2C]
       mov      byte  ptr [rsi+0x09], dil
 
G_M000_IG53:
       mov      rax, gword ptr [rbp-0x50]
       jmp      G_M000_IG25
 
G_M000_IG54:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG55:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1226

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_repeat_index(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
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
       call     [RyuPy.Generated.Module:m0_repeat_index(System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 26

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_repeat_constant(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
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
       call     [RyuPy.Generated.Module:m0_repeat_constant(System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 26

