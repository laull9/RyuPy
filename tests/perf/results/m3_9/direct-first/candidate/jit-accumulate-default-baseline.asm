; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 240
       lea      rbp, [rsp+0xF0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xF0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xB0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0xE8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x30]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x58], rax
       mov      rdi, 0xD1FFAB1E
       mov      esi, 1
       call     CORINFO_HELP_NEWARR_1_PTR
       mov      gword ptr [rbp-0xD8], rax
       mov      rdi, gword ptr [rbp-0xD8]
       mov      rdx, gword ptr [rbp-0x38]
       xor      esi, esi
       call     [CORINFO_HELP_ARRADDR_ST]
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0xE0], rax
       call     [RyuPy.Runtime.PyOps:EmptyArray():System.Object[]]
       mov      gword ptr [rbp-0xF0], rax
       mov      rcx, gword ptr [rbp-0xF0]
       mov      rsi, gword ptr [rbp-0xD8]
       mov      rdx, gword ptr [rbp-0xE0]
       mov      rdi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyOps:CallBuiltin(System.String,System.Object[],System.Object[],System.Object[]):System.Object]
       mov      gword ptr [rbp-0x60], rax
       mov      rdi, gword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rax, qword ptr [rbp-0x68]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      qword ptr [rbp-0x88], 1
       mov      rdi, gword ptr [rbp-0x40]
       call     [RyuPy.Runtime.PyNumericAccumulator:Create(System.Object):System.Object]
       mov      gword ptr [rbp-0xD0], rax
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0xE8]
       dec      eax
       mov      dword ptr [rbp-0xE8], eax
       cmp      dword ptr [rbp-0xE8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xE8]
       mov      esi, 122
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x88], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x8C], eax
       mov      rax, qword ptr [rbp-0x78]
       cmp      rax, qword ptr [rbp-0x80]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x90], eax
       mov      rax, qword ptr [rbp-0x78]
       cmp      rax, qword ptr [rbp-0x80]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       mov      eax, dword ptr [rbp-0x8C]
       and      eax, dword ptr [rbp-0x90]
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       cmp      dword ptr [rbp-0x8C], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0x9C], eax
       mov      eax, dword ptr [rbp-0x9C]
       and      eax, dword ptr [rbp-0x94]
       movzx    rax, al
       mov      dword ptr [rbp-0xA0], eax
       mov      eax, dword ptr [rbp-0x98]
       or       eax, dword ptr [rbp-0xA0]
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       cmp      dword ptr [rbp-0xA4], 0
       jne      SHORT G_M000_IG06
       jmp      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x78]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0xB0], rax
       mov      rax, gword ptr [rbp-0xB0]
       mov      gword ptr [rbp-0x48], rax
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x38]
       mov      rsi, gword ptr [rbp-0x48]
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xB8], rax
       mov      rdi, gword ptr [rbp-0xD0]
       mov      rsi, gword ptr [rbp-0xB8]
       call     [RyuPy.Runtime.PyNumericAccumulator:Add(System.Object,System.Object)]
       mov      rax, qword ptr [rbp-0x78]
       add      rax, qword ptr [rbp-0x88]
       jo       SHORT G_M000_IG09
       mov      qword ptr [rbp-0x78], rax
       jmp      G_M000_IG03
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, gword ptr [rbp-0xD0]
       call     [RyuPy.Runtime.PyNumericAccumulator:Value(System.Object):System.Object]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xC8], rax
       mov      rdi, qword ptr [rbp-0x50]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xC8]
 
G_M000_IG08:
       add      rsp, 240
       pop      rbp
       ret      
 
G_M000_IG09:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 730

; Assembly listing for method RyuPy.Generated.Module:m0_accumulate(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x7a
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 1
; 0 inlinees with PGO data; 8 single block inlinees; 14 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 80
       mov      qword ptr [rsp+0x148], r15
       mov      qword ptr [rsp+0x140], r14
       mov      qword ptr [rsp+0x138], r13
       mov      qword ptr [rsp+0x130], r12
       mov      qword ptr [rsp+0x128], rbx
       lea      rbp, [rsp+0x50]
       mov      rbx, gword ptr [rbp+0xC8]
       mov      r15, qword ptr [rbp+0x88]
       mov      r13, qword ptr [rbp+0x80]
       mov      r12, qword ptr [rbp+0x78]
       mov      r14, bword ptr [rbp+0x30]
 
G_M000_IG02:
       test     r12, r12
       setg     cl
       movzx    rcx, cl
       mov      dword ptr [rbp-0x34], ecx
       jmp      G_M000_IG22
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG12
 
G_M000_IG04:
       mov      rdi, qword ptr [rax+0x08]
       mov      rcx, qword ptr [r14+0x10]
       lea      rdx, [rcx+rdi]
       xor      rcx, rdx
       xor      rdi, rdx
       and      rdi, rcx
       mov      rax, gword ptr [rbp+0x48]
       jl       G_M000_IG12
 
G_M000_IG05:
       mov      qword ptr [r14+0x10], rdx
       xor      rdi, rdi
       mov      gword ptr [r14+0x08], rdi
       jmp      G_M000_IG21
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rsi, rax
       mov      qword ptr [rsi+0x08], r15
 
G_M000_IG07:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG10
 
G_M000_IG08:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      r8, rdx
       dec      edi
       mov      r9d, edi
       cmp      r9d, dword ptr [r8+0x08]
       jae      G_M000_IG44
       cmp      dword ptr [r8+4*r9+0x10], 11
       je       SHORT G_M000_IG10
 
G_M000_IG09:
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG44
       mov      edi, edi
       mov      dword ptr [rdx+4*rdi+0x10], 11
 
G_M000_IG10:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:GetItem(System.Object,System.Object):System.Object]
       mov      gword ptr [rbp+0x48], rax
       movzx    rdi, byte  ptr [r14+0x18]
       test     edi, edi
       je       G_M000_IG26
 
G_M000_IG11:
       test     rax, rax
       jne      G_M000_IG03
 
G_M000_IG12:
       mov      rsi, gword ptr [r14+0x08]
       test     rsi, rsi
       jne      SHORT G_M000_IG16
 
G_M000_IG13:
       mov      rcx, qword ptr [r14+0x10]
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
       lea      rdi, bword ptr [r14+0x08]
       mov      gword ptr [rbp-0x48], rsi
       call     CORINFO_HELP_CHECKED_ASSIGN_REF
       mov      rsi, gword ptr [rbp-0x48]
       mov      rax, gword ptr [rbp+0x48]
 
G_M000_IG16:
       mov      rdx, rax
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x40], rax
       lea      rdi, bword ptr [r14+0x08]
       mov      rsi, rax
       call     CORINFO_HELP_CHECKED_ASSIGN_REF
       mov      rdi, gword ptr [rbp-0x40]
       test     rdi, rdi
       jne      G_M000_IG27
 
G_M000_IG17:
       xor      eax, eax
 
G_M000_IG18:
       mov      byte  ptr [r14+0x18], al
       test     rdi, rdi
       je       SHORT G_M000_IG21
 
G_M000_IG19:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      SHORT G_M000_IG21
 
G_M000_IG20:
       mov      rdi, qword ptr [rdi+0x08]
       mov      qword ptr [r14+0x10], rdi
 
G_M000_IG21:
       add      r15, r12
       jo       G_M000_IG45
       mov      ecx, dword ptr [rbp-0x34]
 
G_M000_IG22:
       mov      edi, ecx
       cmp      r15, r13
       setl     dl
       movzx    rdx, dl
       and      edx, edi
       cmp      r15, r13
       setg     sil
       movzx    rsi, sil
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, esi
       or       edi, edx
       je       G_M000_IG29
 
G_M000_IG23:
       lea      rdx, [r15+0x05]
       cmp      rdx, 261
       ja       G_M000_IG06
 
G_M000_IG24:
       test     rdx, rdx
       jl       G_M000_IG45
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG44
       mov      rsi, gword ptr [rdx+8*rsi+0x10]
       jmp      G_M000_IG07
 
G_M000_IG25:
       test     rdx, rdx
       jl       G_M000_IG45
       mov      rsi, rdx
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       cmp      rsi, 262
       jae      G_M000_IG44
       mov      rsi, gword ptr [rdx+8*rsi+0x10]
       jmp      G_M000_IG15
 
G_M000_IG26:
       mov      rsi, gword ptr [r14+0x08]
       jmp      G_M000_IG16
 
G_M000_IG27:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rdi], rax
       jne      G_M000_IG17
 
G_M000_IG28:
       mov      eax, 1
       jmp      G_M000_IG18
 
G_M000_IG29:
       cmp      byte  ptr [r14+0x18], 0
       je       G_M000_IG43
 
G_M000_IG30:
       mov      rcx, gword ptr [r14+0x08]
       test     rcx, rcx
       jne      SHORT G_M000_IG34
 
G_M000_IG31:
       mov      rbx, qword ptr [r14+0x10]
       lea      r15, [rbx+0x05]
       cmp      r15, 261
       jbe      G_M000_IG42
 
G_M000_IG32:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r13, rax
       mov      qword ptr [r13+0x08], rbx
 
G_M000_IG33:
       lea      rdi, bword ptr [r14+0x08]
       mov      rsi, r13
       call     CORINFO_HELP_CHECKED_ASSIGN_REF
       mov      rax, r13
       mov      rcx, rax
 
G_M000_IG34:
       mov      edx, dword ptr [(reloc)]
       test     edx, edx
       jle      SHORT G_M000_IG37
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       mov      rdi, gword ptr [rdi]
       mov      rsi, rdi
       lea      r8d, [rdx-0x01]
       mov      r9d, r8d
       cmp      r9d, dword ptr [rsi+0x08]
       jae      SHORT G_M000_IG44
       cmp      dword ptr [rsi+4*r9+0x10], 12
       je       SHORT G_M000_IG37
 
G_M000_IG36:
       cmp      r8d, dword ptr [rdi+0x08]
       jae      SHORT G_M000_IG44
       mov      esi, r8d
       mov      dword ptr [rdi+4*rsi+0x10], 12
 
G_M000_IG37:
       mov      rax, qword ptr [rbp+0xB0]
       test     rax, rax
       jle      SHORT G_M000_IG40
 
G_M000_IG38:
       movsxd   rdx, edx
       cmp      rax, rdx
       jg       SHORT G_M000_IG40
 
G_M000_IG39:
       dec      eax
       mov      dword ptr [(reloc)], eax
 
G_M000_IG40:
       mov      rax, rcx
 
G_M000_IG41:
       add      rsp, 296
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG42:
       test     r15, r15
       jl       SHORT G_M000_IG45
       mov      rdi, r15
       mov      rsi, 0xD1FFAB1E
       mov      rdx, gword ptr [rsi]
       cmp      rdi, 262
       jae      SHORT G_M000_IG44
       mov      r13, gword ptr [rdx+8*rdi+0x10]
       jmp      G_M000_IG33
 
G_M000_IG43:
       mov      rcx, gword ptr [r14+0x08]
       jmp      G_M000_IG34
 
G_M000_IG44:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG45:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 910

