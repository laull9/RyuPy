; Assembly listing for method RyuPy.Generated.Module:m0_sum_range(System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 208
       lea      rbp, [rsp+0xD0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0xC8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x30]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x58], rax
       mov      rdi, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x60], rax
       mov      rdi, gword ptr [rbp-0x38]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x68], rax
       mov      rax, qword ptr [rbp-0x60]
       mov      qword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x78], rax
       mov      qword ptr [rbp-0x80], 1
       mov      rdi, gword ptr [rbp-0x40]
       call     [RyuPy.Runtime.PyNumericAccumulator:Create(System.Object):System.Object]
       mov      gword ptr [rbp-0xC0], rax
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0xC8]
       dec      eax
       mov      dword ptr [rbp-0xC8], eax
       cmp      dword ptr [rbp-0xC8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xC8]
       mov      esi, 85
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
       mov      gword ptr [rbp-0x48], rax
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0xC0]
       mov      rsi, gword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyNumericAccumulator:Add(System.Object,System.Object)]
       mov      rax, qword ptr [rbp-0x70]
       add      rax, qword ptr [rbp-0x80]
       jo       SHORT G_M000_IG09
       mov      qword ptr [rbp-0x70], rax
       jmp      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0xC0]
       call     [RyuPy.Runtime.PyNumericAccumulator:Value(System.Object):System.Object]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xB8], rax
       mov      rdi, qword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xB8]
 
G_M000_IG08:
       add      rsp, 208
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 571

; Assembly listing for method RyuPy.Generated.Module:m0_sum_range(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x55
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 1
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
       mov      r12, qword ptr [rbp+0x90]
       mov      rbx, qword ptr [rbp+0x70]
       mov      r14, qword ptr [rbp+0x68]
       mov      r13, qword ptr [rbp+0x60]
       mov      r15, bword ptr [rbp+0x20]
 
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
       mov      gword ptr [rbp-0x40], rax
       mov      rdi, qword ptr [rax+0x08]
       mov      rcx, qword ptr [r15+0x10]
       lea      rdx, [rcx+rdi]
       xor      rcx, rdx
       xor      rdi, rdx
       and      rdi, rcx
       mov      rax, gword ptr [rbp-0x40]
       jl       SHORT G_M000_IG12
 
G_M000_IG05:
       mov      qword ptr [r15+0x10], rdx
       xor      rdi, rdi
       mov      gword ptr [r15+0x08], rdi
       jmp      G_M000_IG21
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], rbx
 
G_M000_IG07:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG10
 
G_M000_IG08:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      esi, edi
       cmp      esi, dword ptr [rdx+0x08]
       jae      G_M000_IG43
       cmp      dword ptr [rdx+4*rsi+0x10], 10
       je       SHORT G_M000_IG10
 
G_M000_IG09:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG43
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 10
 
G_M000_IG10:
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
       mov      gword ptr [rbp-0x40], rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x30]
       mov      qword ptr [rax+0x08], rdi
       mov      rsi, rax
 
G_M000_IG15:
       lea      rdi, bword ptr [r15+0x08]
       mov      gword ptr [rbp-0x50], rsi
       call     CORINFO_HELP_CHECKED_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x50]
       mov      rax, gword ptr [rbp-0x40]
 
G_M000_IG16:
       mov      rdx, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x48], rax
       lea      rdi, bword ptr [r15+0x08]
       mov      rsi, rax
       call     CORINFO_HELP_CHECKED_ASSIGN_REF
       mov      rdi, gword ptr [rbp-0x48]
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
       mov      rdx, rdi
       mov      rax, rdx
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
       mov      gword ptr [rbp-0x40], rax
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
       cmp      dword ptr [rdi+4*r8+0x10], 11
       je       SHORT G_M000_IG37
 
G_M000_IG36:
       cmp      esi, dword ptr [rdx+0x08]
       jae      SHORT G_M000_IG43
       mov      edi, esi
       mov      dword ptr [rdx+4*rdi+0x10], 11
 
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
 
; Total bytes of code 883

