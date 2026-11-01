; Assembly listing for method RyuPy.Generated.Module:m0_loop_diamond(System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 256
       lea      rbp, [rsp+0x100]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xF0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xB0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0xF8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x58], rax
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x60], rax
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      rax, qword ptr [rbp-0x78]
       mov      qword ptr [rbp-0x88], rax
       mov      qword ptr [rbp-0x90], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0xF8]
       dec      eax
       mov      dword ptr [rbp-0xF8], eax
       cmp      dword ptr [rbp-0xF8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xF8]
       mov      esi, 88
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x90], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       mov      rax, qword ptr [rbp-0x80]
       cmp      rax, qword ptr [rbp-0x88]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       mov      rax, qword ptr [rbp-0x80]
       cmp      rax, qword ptr [rbp-0x88]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x9C], eax
       mov      eax, dword ptr [rbp-0x94]
       and      eax, dword ptr [rbp-0x98]
       movzx    rax, al
       mov      dword ptr [rbp-0xA0], eax
       cmp      dword ptr [rbp-0x94], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       mov      eax, dword ptr [rbp-0xA4]
       and      eax, dword ptr [rbp-0x9C]
       movzx    rax, al
       mov      dword ptr [rbp-0xA8], eax
       mov      eax, dword ptr [rbp-0xA0]
       or       eax, dword ptr [rbp-0xA8]
       movzx    rax, al
       mov      dword ptr [rbp-0xAC], eax
       cmp      dword ptr [rbp-0xAC], 0
       jne      SHORT G_M000_IG06
       jmp      SHORT G_M000_IG08
 
G_M000_IG06:
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x38]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0xBC], eax
       cmp      dword ptr [rbp-0xBC], 0
       jne      SHORT G_M000_IG09
       jmp      G_M000_IG10
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x80]
       add      rax, qword ptr [rbp-0x90]
       jo       G_M000_IG16
       mov      qword ptr [rbp-0x80], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xF0], rax
       jmp      G_M000_IG14
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xC8], rax
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x50], rax
       jmp      SHORT G_M000_IG11
 
G_M000_IG10:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xD0], rax
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x50], rax
 
G_M000_IG11:
       mov      eax, dword ptr [rbp-0xF8]
       dec      eax
       mov      dword ptr [rbp-0xF8], eax
       cmp      dword ptr [rbp-0xF8], 0
       jg       SHORT G_M000_IG13
 
G_M000_IG12:
       lea      rdi, [rbp-0xF8]
       mov      esi, 257
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG13:
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xD8], rax
       mov      rdx, gword ptr [rbp-0x50]
       mov      rcx, gword ptr [rbp-0xD8]
       mov      edi, 1
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdx, gword ptr [rbp-0x40]
       mov      rcx, gword ptr [rbp-0xE0]
       mov      edi, 2
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG07
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xF0]
 
G_M000_IG15:
       add      rsp, 256
       pop      rbp
       ret      
 
G_M000_IG16:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 835

; Assembly listing for method RyuPy.Generated.Module:m0_loop_diamond(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x101
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 2
; 52 inlinees with PGO data; 46 single block inlinees; 45 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 336
       mov      qword ptr [rsp+0x258], r15
       mov      qword ptr [rsp+0x250], r14
       mov      qword ptr [rsp+0x248], r13
       mov      qword ptr [rsp+0x240], r12
       mov      qword ptr [rsp+0x238], rbx
       lea      rbp, [rsp+0x150]
       xor      eax, eax
       mov      qword ptr [rbp-0xC8], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xC0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x90], zmm8
       mov      rbx, gword ptr [rbp+0xD8]
       mov      r14, gword ptr [rbp+0xD0]
       mov      rsi, gword ptr [rbp+0xC0]
       mov      r15, qword ptr [rbp+0x90]
       mov      r13, qword ptr [rbp+0x88]
       mov      r12, qword ptr [rbp+0x80]
 
G_M000_IG02:
       jmp      G_M000_IG73
 
G_M000_IG03:
       xor      edi, edi
       jmp      G_M000_IG84
 
G_M000_IG04:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdx, rax
       mov      gword ptr [rbp-0x108], rdx
       lea      rdi, bword ptr [rdx+0x08]
       mov      rsi, gword ptr [rbp-0x148]
       call     CORINFO_HELP_ASSIGN_REF
       mov      rdx, gword ptr [rbp-0x108]
       mov      edi, dword ptr [rbp-0xE4]
       mov      dword ptr [rdx+0x10], edi
       jmp      G_M000_IG86
 
G_M000_IG05:
       lea      rcx, [r8+0x05]
       cmp      rcx, 261
       jbe      SHORT G_M000_IG08
 
G_M000_IG06:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      rdi, qword ptr [rbp-0x50]
       mov      qword ptr [rax+0x08], rdi
       mov      rdx, rax
 
G_M000_IG07:
       jmp      G_M000_IG86
 
G_M000_IG08:
       test     rcx, rcx
       jl       G_M000_IG134
       mov      rdx, rcx
       mov      rdi, 0xD1FFAB1E
       mov      rcx, gword ptr [rdi]
       cmp      rdx, 262
       jae      G_M000_IG133
       mov      rdx, gword ptr [rcx+8*rdx+0x10]
       jmp      SHORT G_M000_IG07
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG27
 
G_M000_IG10:
       vmovsd   xmm0, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rdx
       vmovsd   qword ptr [rbp-0x78], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x78]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      rdx, rax
       mov      rax, rdx
       jmp      G_M000_IG28
 
G_M000_IG11:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG17
 
G_M000_IG12:
       vmovsd   xmm0, qword ptr [rsi+0x08]
       vmovsd   qword ptr [rbp-0x30], xmm0
       test     rcx, rcx
       je       G_M000_IG27
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      SHORT G_M000_IG15
 
G_M000_IG14:
       vmovsd   xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG28
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG27
 
G_M000_IG16:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rcx+0x08]
       vmovsd   qword ptr [rbp-0x80], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG28
 
G_M000_IG17:
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG27
 
G_M000_IG18:
       mov      rdx, rsi
       mov      gword ptr [rbp-0xF8], rdx
       test     rcx, rcx
       je       G_M000_IG27
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG27
 
G_M000_IG20:
       mov      rsi, rcx
       mov      gword ptr [rbp-0x100], rsi
       cmp      dword ptr [rdx+0x08], 0
       je       G_M000_IG25
 
G_M000_IG21:
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG24
 
G_M000_IG22:
       mov      ecx, dword ptr [rdx+0x08]
       mov      dword ptr [rbp-0x84], ecx
       mov      edi, ecx
       add      edi, dword ptr [rsi+0x08]
       js       G_M000_IG132
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x118], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0xF8]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0x84]
       mov      rax, gword ptr [rbp-0x118]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      rsi, gword ptr [rbp-0x100]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      rax, gword ptr [rbp-0x118]
 
G_M000_IG23:
       jmp      SHORT G_M000_IG28
 
G_M000_IG24:
       mov      rax, rdx
       jmp      SHORT G_M000_IG23
 
G_M000_IG25:
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG113
 
G_M000_IG26:
       mov      rdx, rsi
       mov      rax, rdx
       jmp      SHORT G_M000_IG23
 
G_M000_IG27:
       mov      rdx, rcx
       mov      edi, 1
       xor      ecx, ecx
       call     [RyuPy.Runtime.PyOps:BinaryCore(int,System.Object,System.Object,bool):System.Object]
 
G_M000_IG28:
       test     r14, r14
       je       G_M000_IG63
 
G_M000_IG29:
       mov      rcx, qword ptr [r14]
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG47
 
G_M000_IG30:
       mov      rsi, qword ptr [r14+0x08]
       test     rax, rax
       je       G_M000_IG63
 
G_M000_IG31:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG45
 
G_M000_IG32:
       mov      r14, qword ptr [rax+0x08]
       lea      rax, [rsi+r14]
       mov      qword ptr [rbp-0xA8], rax
       mov      rdi, rsi
       xor      rdi, rax
       mov      rcx, r14
       xor      rcx, rax
       and      rdi, rcx
       jge      G_M000_IG41
 
G_M000_IG33:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xB8], xmm0
       lea      rdi, [rbp-0xB8]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0xB8]
       mov      gword ptr [rbp-0x150], rax
       mov      ecx, dword ptr [rbp-0xB0]
       mov      dword ptr [rbp-0xE8], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0xC8], xmm0
       lea      rdi, [rbp-0xC8]
       mov      rsi, r14
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x150]
       mov      esi, dword ptr [rbp-0xE8]
       mov      rdx, gword ptr [rbp-0xC8]
       mov      rcx, qword ptr [rbp-0xC0]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      r14, rax
       mov      eax, edx
       mov      dword ptr [rbp-0xEC], edx
       test     r14, r14
       jne      G_M000_IG114
 
G_M000_IG34:
       test     r14, r14
       jne      G_M000_IG123
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jge      SHORT G_M000_IG39
 
G_M000_IG35:
       mov      edi, -1
 
G_M000_IG36:
       test     edi, edi
       jg       SHORT G_M000_IG40
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x130], rax
       mov      rdi, r14
       mov      esi, dword ptr [rbp-0xEC]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      r14, gword ptr [rbp-0x130]
       mov      qword ptr [r14+0x08], rax
 
G_M000_IG38:
       jmp      G_M000_IG64
 
G_M000_IG39:
       xor      edi, edi
       jmp      SHORT G_M000_IG36
 
G_M000_IG40:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x128], rax
       lea      rdi, bword ptr [rax+0x08]
       mov      rsi, r14
       call     CORINFO_HELP_ASSIGN_REF
       mov      r14, gword ptr [rbp-0x128]
       mov      edi, dword ptr [rbp-0xEC]
       mov      dword ptr [r14+0x10], edi
       jmp      SHORT G_M000_IG38
 
G_M000_IG41:
       lea      r14, [rax+0x05]
       cmp      r14, 261
       jbe      SHORT G_M000_IG44
 
G_M000_IG42:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, qword ptr [rbp-0xA8]
       mov      qword ptr [rax+0x08], r14
       mov      r14, rax
 
G_M000_IG43:
       jmp      SHORT G_M000_IG38
 
G_M000_IG44:
       test     r14, r14
       jl       G_M000_IG134
       mov      rdi, r14
       mov      rax, 0xD1FFAB1E
       mov      rcx, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG133
       mov      r14, gword ptr [rcx+8*rdi+0x10]
       jmp      SHORT G_M000_IG43
 
G_M000_IG45:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG63
 
G_M000_IG46:
       vmovsd   xmm0, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xA0], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, rsi
       vmovsd   qword ptr [rbp-0xD0], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xD0]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0xA0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG64
 
G_M000_IG47:
       mov      rdi, 0xD1FFAB1E
       cmp      rcx, rdi
       jne      G_M000_IG53
 
G_M000_IG48:
       vmovsd   xmm0, qword ptr [r14+0x08]
       vmovsd   qword ptr [rbp-0x90], xmm0
       test     rax, rax
       je       G_M000_IG63
 
G_M000_IG49:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      SHORT G_M000_IG51
 
G_M000_IG50:
       vmovsd   xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0x98], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x98]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG64
 
G_M000_IG51:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG63
 
G_M000_IG52:
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rax+0x08]
       vmovsd   qword ptr [rbp-0xD8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xD8]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG64
 
G_M000_IG53:
       mov      rsi, 0xD1FFAB1E
       cmp      rcx, rsi
       jne      G_M000_IG63
 
G_M000_IG54:
       mov      rsi, r14
       mov      gword ptr [rbp-0x120], rsi
       test     rax, rax
       je       G_M000_IG63
 
G_M000_IG55:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rax], rdi
       jne      G_M000_IG63
 
G_M000_IG56:
       mov      r14, rax
       cmp      dword ptr [rsi+0x08], 0
       je       G_M000_IG61
 
G_M000_IG57:
       cmp      dword ptr [r14+0x08], 0
       je       SHORT G_M000_IG60
 
G_M000_IG58:
       mov      eax, dword ptr [rsi+0x08]
       mov      dword ptr [rbp-0xDC], eax
       mov      edi, eax
       add      edi, dword ptr [r14+0x08]
       js       G_M000_IG132
       movsxd   rsi, edi
       mov      rdi, 0xD1FFAB1E
       call     System.String:FastAllocateString(ptr,nint):System.String
       mov      gword ptr [rbp-0x138], rax
       cmp      byte  ptr [rax], al
       lea      rdi, bword ptr [rax+0x0C]
       mov      rsi, gword ptr [rbp-0x120]
       mov      edx, dword ptr [rsi+0x08]
       add      rdx, rdx
       add      rsi, 12
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      edi, dword ptr [rbp-0xDC]
       mov      rax, gword ptr [rbp-0x138]
       lea      rdi, bword ptr [rax+2*rdi+0x0C]
       mov      edx, dword ptr [r14+0x08]
       add      rdx, rdx
       lea      rsi, bword ptr [r14+0x0C]
       call     [System.SpanHelpers:Memmove(byref,byref,nuint)]
       mov      r14, gword ptr [rbp-0x138]
 
G_M000_IG59:
       jmp      SHORT G_M000_IG64
 
G_M000_IG60:
       mov      r14, rsi
       jmp      SHORT G_M000_IG59
 
G_M000_IG61:
       cmp      dword ptr [r14+0x08], 0
       je       G_M000_IG131
 
G_M000_IG62:
       jmp      SHORT G_M000_IG59
 
G_M000_IG63:
       mov      rdx, rax
       mov      rsi, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:InPlaceBinary(int,System.Object,System.Object):System.Object]
       mov      r14, rax
 
G_M000_IG64:
       add      r15, r12
       jo       G_M000_IG134
       test     r12, r12
       setg     dil
       movzx    rdi, dil
       cmp      r15, r13
       setl     al
       movzx    rax, al
       and      eax, edi
       cmp      r15, r13
       setg     cl
       movzx    rcx, cl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, ecx
       or       edi, eax
       je       G_M000_IG91
 
G_M000_IG65:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG68
 
G_M000_IG66:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG133
       cmp      dword ptr [rcx+4*rdx+0x10], 21
       je       SHORT G_M000_IG68
 
G_M000_IG67:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG133
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 21
 
G_M000_IG68:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG87
 
G_M000_IG69:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG72
 
G_M000_IG70:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      eax
       mov      edi, eax
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG133
       cmp      dword ptr [rdx+4*rdi+0x10], 22
       je       SHORT G_M000_IG72
 
G_M000_IG71:
       cmp      eax, dword ptr [rcx+0x08]
       jae      G_M000_IG133
       mov      eax, eax
       mov      dword ptr [rcx+4*rax+0x10], 22
 
G_M000_IG72:
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
 
G_M000_IG73:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG76
 
G_M000_IG74:
       mov      rcx, 0xD1FFAB1E
       mov      rcx, gword ptr [rcx]
       mov      rdx, rcx
       dec      edi
       mov      r8d, edi
       cmp      r8d, dword ptr [rdx+0x08]
       jae      G_M000_IG133
       cmp      dword ptr [rdx+4*r8+0x10], 25
       je       SHORT G_M000_IG76
 
G_M000_IG75:
       cmp      edi, dword ptr [rcx+0x08]
       jae      G_M000_IG133
       mov      edi, edi
       mov      dword ptr [rcx+4*rdi+0x10], 25
 
G_M000_IG76:
       mov      rdi, 0xD1FFAB1E
       mov      rdx, gword ptr [rdi]
       mov      rcx, rdx
       test     rsi, rsi
       je       G_M000_IG27
 
G_M000_IG77:
       mov      rdx, qword ptr [rsi]
       mov      rdi, 0xD1FFAB1E
       cmp      rdx, rdi
       jne      G_M000_IG11
 
G_M000_IG78:
       mov      rdx, qword ptr [rsi+0x08]
       test     rcx, rcx
       je       G_M000_IG27
 
G_M000_IG79:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rcx], rdi
       jne      G_M000_IG09
 
G_M000_IG80:
       mov      rcx, qword ptr [rcx+0x08]
       mov      qword ptr [rbp-0x48], rcx
       lea      r8, [rdx+rcx]
       mov      qword ptr [rbp-0x50], r8
       mov      rdi, rdx
       xor      rdi, r8
       mov      rsi, rcx
       xor      rsi, r8
       and      rdi, rsi
       jge      G_M000_IG05
 
G_M000_IG81:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
       lea      rdi, [rbp-0x60]
       mov      rsi, rdx
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0x140], rax
       mov      ecx, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xE0], ecx
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x70], xmm0
       lea      rdi, [rbp-0x70]
       mov      rsi, qword ptr [rbp-0x48]
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rdi, gword ptr [rbp-0x140]
       mov      esi, dword ptr [rbp-0xE0]
       mov      rdx, gword ptr [rbp-0x70]
       mov      rcx, qword ptr [rbp-0x68]
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      gword ptr [rbp-0x148], rax
       mov      ecx, edx
       mov      dword ptr [rbp-0xE4], edx
       test     rax, rax
       jne      G_M000_IG96
 
G_M000_IG82:
       test     rax, rax
       jne      G_M000_IG105
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jge      G_M000_IG03
 
G_M000_IG83:
       mov      edi, -1
 
G_M000_IG84:
       test     edi, edi
       jg       G_M000_IG04
 
G_M000_IG85:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      gword ptr [rbp-0x110], rax
       mov      rdi, gword ptr [rbp-0x148]
       mov      esi, dword ptr [rbp-0xE4]
       call     [System.Numerics.BigInteger:op_Explicit(System.Numerics.BigInteger):long]
       mov      rdx, gword ptr [rbp-0x110]
       mov      qword ptr [rdx+0x08], rax
 
G_M000_IG86:
       mov      rax, rdx
       jmp      G_M000_IG28
 
G_M000_IG87:
       mov      eax, dword ptr [(reloc)]
       test     eax, eax
       jle      SHORT G_M000_IG90
 
G_M000_IG88:
       mov      rsi, 0xD1FFAB1E
       mov      rcx, gword ptr [rsi]
       mov      rdx, rcx
       dec      eax
       mov      edi, eax
       cmp      edi, dword ptr [rdx+0x08]
       jae      G_M000_IG133
       cmp      dword ptr [rdx+4*rdi+0x10], 24
       je       SHORT G_M000_IG90
 
G_M000_IG89:
       cmp      eax, dword ptr [rcx+0x08]
       jae      G_M000_IG133
       mov      eax, eax
       mov      dword ptr [rcx+4*rax+0x10], 24
 
G_M000_IG90:
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       jmp      G_M000_IG73
 
G_M000_IG91:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG94
 
G_M000_IG92:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG133
       cmp      dword ptr [rcx+4*rdx+0x10], 26
       je       SHORT G_M000_IG94
 
G_M000_IG93:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG133
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 26
 
G_M000_IG94:
       mov      rdi, qword ptr [rbp+0xB8]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r14
 
G_M000_IG95:
       add      rsp, 568
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG96:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG97
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG98
 
G_M000_IG97:
       mov      edx, ecx
       jmp      SHORT G_M000_IG104
 
G_M000_IG98:
       cmp      edi, 2
       je       SHORT G_M000_IG99
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG133
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG100
 
G_M000_IG99:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
 
G_M000_IG100:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG101
       mov      edx, -1
       jmp      SHORT G_M000_IG103
 
G_M000_IG101:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jbe      SHORT G_M000_IG102
       mov      edx, 1
       jmp      SHORT G_M000_IG103
 
G_M000_IG102:
       xor      edx, edx
 
G_M000_IG103:
       imul     edx, ecx
 
G_M000_IG104:
       test     edx, edx
       jge      G_M000_IG82
       jmp      G_M000_IG04
 
G_M000_IG105:
       movsxd   rdi, ecx
       mov      rdx, 0xD1FFAB1E
       xor      rdi, rdx
       jl       SHORT G_M000_IG106
       mov      edi, dword ptr [rax+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG107
 
G_M000_IG106:
       mov      edi, ecx
       mov      ecx, dword ptr [rbp-0xE4]
       jmp      G_M000_IG84
 
G_M000_IG107:
       cmp      edi, 2
       je       SHORT G_M000_IG108
       cmp      dword ptr [rax+0x08], 0
       jbe      G_M000_IG133
       mov      edi, dword ptr [rax+0x10]
       jmp      SHORT G_M000_IG109
 
G_M000_IG108:
       mov      edi, dword ptr [rax+0x14]
       shl      rdi, 32
       mov      edx, dword ptr [rax+0x10]
       or       rdi, rdx
       mov      rax, gword ptr [rbp-0x148]
 
G_M000_IG109:
       mov      rdx, 0xD1FFAB1E
       cmp      rdi, rdx
       jae      SHORT G_M000_IG110
       mov      edx, -1
       jmp      SHORT G_M000_IG112
 
G_M000_IG110:
       test     rdi, rdi
       jge      SHORT G_M000_IG111
       mov      edx, 1
       jmp      SHORT G_M000_IG112
 
G_M000_IG111:
       xor      edx, edx
 
G_M000_IG112:
       mov      edi, ecx
       imul     edi, edx
       jmp      G_M000_IG84
 
G_M000_IG113:
       mov      rdx, 0xD1FFAB1E
       mov      rax, rdx
       jmp      G_M000_IG23
 
G_M000_IG114:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG115
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG116
 
G_M000_IG115:
       mov      ecx, eax
       jmp      SHORT G_M000_IG122
 
G_M000_IG116:
       cmp      edi, 2
       je       SHORT G_M000_IG117
       cmp      dword ptr [r14+0x08], 0
       jbe      G_M000_IG133
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG118
 
G_M000_IG117:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r14+0x10]
       or       rdi, rcx
 
G_M000_IG118:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG119
       mov      ecx, -1
       jmp      SHORT G_M000_IG121
 
G_M000_IG119:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jbe      SHORT G_M000_IG120
       mov      ecx, 1
       jmp      SHORT G_M000_IG121
 
G_M000_IG120:
       xor      ecx, ecx
 
G_M000_IG121:
       imul     ecx, eax
 
G_M000_IG122:
       test     ecx, ecx
       jge      G_M000_IG34
       jmp      G_M000_IG40
 
G_M000_IG123:
       movsxd   rdi, eax
       mov      rcx, 0xD1FFAB1E
       xor      rdi, rcx
       jl       SHORT G_M000_IG124
       mov      edi, dword ptr [r14+0x08]
       cmp      edi, 2
       jle      SHORT G_M000_IG125
 
G_M000_IG124:
       mov      edi, eax
       mov      eax, dword ptr [rbp-0xEC]
       jmp      G_M000_IG36
 
G_M000_IG125:
       cmp      edi, 2
       je       SHORT G_M000_IG126
       cmp      dword ptr [r14+0x08], 0
       jbe      SHORT G_M000_IG133
       mov      edi, dword ptr [r14+0x10]
       jmp      SHORT G_M000_IG127
 
G_M000_IG126:
       mov      edi, dword ptr [r14+0x14]
       shl      rdi, 32
       mov      ecx, dword ptr [r14+0x10]
       or       rdi, rcx
 
G_M000_IG127:
       mov      rcx, 0xD1FFAB1E
       cmp      rdi, rcx
       jae      SHORT G_M000_IG128
       mov      ecx, -1
       jmp      SHORT G_M000_IG130
 
G_M000_IG128:
       test     rdi, rdi
       jge      SHORT G_M000_IG129
       mov      ecx, 1
       jmp      SHORT G_M000_IG130
 
G_M000_IG129:
       xor      ecx, ecx
 
G_M000_IG130:
       mov      edi, eax
       imul     edi, ecx
       jmp      G_M000_IG36
 
G_M000_IG131:
       mov      r14, 0xD1FFAB1E
       jmp      G_M000_IG59
 
G_M000_IG132:
       call     [System.ThrowHelper:ThrowOutOfMemoryException_StringTooLong()]
       int3     
 
G_M000_IG133:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG134:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 3131

; Assembly listing for method (dynamicClass):InvokeStub_Module.m0_loop_diamond(System.Object,System.Span`1[System.Object]):System.Object (FullOpts)
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
       cmp      edx, 1
       jbe      SHORT G_M000_IG04
       mov      rsi, gword ptr [rsi+0x08]
       call     [RyuPy.Generated.Module:m0_loop_diamond(System.Object,System.Object):System.Object]
       nop      
 
G_M000_IG03:
       add      rsp, 8
       ret      
 
G_M000_IG04:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
; Total bytes of code 35

; Assembly listing for method RyuPy.Generated.Module:m0_loop_diamond(System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 256
       lea      rbp, [rsp+0x100]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xF0], zmm8
       vmovdqu32 zmmword ptr [rbp-0xB0], zmm8
       vmovdqu32 zmmword ptr [rbp-0x80], zmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x40], rax
       mov      gword ptr [rbp-0x30], rdi
       mov      gword ptr [rbp-0x38], rsi
 
G_M000_IG02:
       mov      dword ptr [rbp-0xF8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x58], rax
       mov      edi, 19
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x60], rax
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0x40], rax
       mov      edi, 20
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x68], rax
       mov      rdi, gword ptr [rbp-0x68]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x70], rax
       mov      rdi, gword ptr [rbp-0x30]
       call     [RyuPy.Runtime.PyCast:ToInt64(System.Object):long]
       mov      qword ptr [rbp-0x78], rax
       mov      rax, qword ptr [rbp-0x70]
       mov      qword ptr [rbp-0x80], rax
       mov      rax, qword ptr [rbp-0x78]
       mov      qword ptr [rbp-0x88], rax
       mov      qword ptr [rbp-0x90], 1
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0xF8]
       dec      eax
       mov      dword ptr [rbp-0xF8], eax
       cmp      dword ptr [rbp-0xF8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xF8]
       mov      esi, 88
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       cmp      qword ptr [rbp-0x90], 0
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       mov      rax, qword ptr [rbp-0x80]
       cmp      rax, qword ptr [rbp-0x88]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x98], eax
       mov      rax, qword ptr [rbp-0x80]
       cmp      rax, qword ptr [rbp-0x88]
       setg     al
       movzx    rax, al
       mov      dword ptr [rbp-0x9C], eax
       mov      eax, dword ptr [rbp-0x94]
       and      eax, dword ptr [rbp-0x98]
       movzx    rax, al
       mov      dword ptr [rbp-0xA0], eax
       cmp      dword ptr [rbp-0x94], 0
       sete     al
       movzx    rax, al
       mov      dword ptr [rbp-0xA4], eax
       mov      eax, dword ptr [rbp-0xA4]
       and      eax, dword ptr [rbp-0x9C]
       movzx    rax, al
       mov      dword ptr [rbp-0xA8], eax
       mov      eax, dword ptr [rbp-0xA0]
       or       eax, dword ptr [rbp-0xA8]
       movzx    rax, al
       mov      dword ptr [rbp-0xAC], eax
       cmp      dword ptr [rbp-0xAC], 0
       jne      SHORT G_M000_IG06
       jmp      SHORT G_M000_IG08
 
G_M000_IG06:
       mov      edi, 21
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x38]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0xBC], eax
       cmp      dword ptr [rbp-0xBC], 0
       jne      SHORT G_M000_IG09
       jmp      G_M000_IG10
 
G_M000_IG07:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rax, qword ptr [rbp-0x80]
       add      rax, qword ptr [rbp-0x90]
       jo       G_M000_IG16
       mov      qword ptr [rbp-0x80], rax
       jmp      G_M000_IG03
 
G_M000_IG08:
       mov      edi, 26
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x40]
       mov      gword ptr [rbp-0xF0], rax
       jmp      G_M000_IG14
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 22
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xC8], rax
       mov      rax, gword ptr [rbp-0xC8]
       mov      gword ptr [rbp-0x50], rax
       jmp      SHORT G_M000_IG11
 
G_M000_IG10:
       mov      edi, 24
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xD0], rax
       mov      rax, gword ptr [rbp-0xD0]
       mov      gword ptr [rbp-0x50], rax
 
G_M000_IG11:
       mov      eax, dword ptr [rbp-0xF8]
       dec      eax
       mov      dword ptr [rbp-0xF8], eax
       cmp      dword ptr [rbp-0xF8], 0
       jg       SHORT G_M000_IG13
 
G_M000_IG12:
       lea      rdi, [rbp-0xF8]
       mov      esi, 257
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG13:
       mov      edi, 25
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0xD8], rax
       mov      rdx, gword ptr [rbp-0x50]
       mov      rcx, gword ptr [rbp-0xD8]
       mov      edi, 1
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE0], rax
       mov      rdx, gword ptr [rbp-0x40]
       mov      rcx, gword ptr [rbp-0xE0]
       mov      edi, 2
       mov      esi, 257
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0xE8], rax
       mov      rax, gword ptr [rbp-0xE8]
       mov      gword ptr [rbp-0x40], rax
       jmp      G_M000_IG07
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x58]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0xF0]
 
G_M000_IG15:
       add      rsp, 256
       pop      rbp
       ret      
 
G_M000_IG16:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 835

; Assembly listing for method RyuPy.Generated.Module:m0_loop_diamond(System.Object,System.Object):System.Object (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x101
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 2
; 20 inlinees with PGO data; 0 single block inlinees; 10 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       sub      rsp, 208
       mov      qword ptr [rsp+0x1D8], r15
       mov      qword ptr [rsp+0x1D0], r14
       mov      qword ptr [rsp+0x1C8], r13
       mov      qword ptr [rsp+0x1C0], r12
       mov      qword ptr [rsp+0x1B8], rbx
       lea      rbp, [rsp+0xD0]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0xA0], zmm8
       vmovdqa  xmmword ptr [rbp-0x60], xmm8
       mov      rbx, gword ptr [rbp+0xD8]
       mov      r15, gword ptr [rbp+0xD0]
       mov      r14, gword ptr [rbp+0xC0]
       mov      r13, qword ptr [rbp+0x90]
       mov      r12, qword ptr [rbp+0x88]
       mov      rax, qword ptr [rbp+0x80]
 
G_M000_IG02:
       mov      qword ptr [rbp+0x80], rax
       test     rax, rax
       setg     dl
       movzx    rdx, dl
       mov      dword ptr [rbp-0xBC], edx
       jmp      G_M000_IG77
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      rdi, qword ptr [rbp-0x50]
       mov      qword ptr [r14+0x08], rdi
       jmp      G_M000_IG85
 
G_M000_IG04:
       mov      rdi, r8
       test     rdi, rdi
       je       SHORT G_M000_IG07
 
G_M000_IG05:
       mov      r10, 0xD1FFAB1E
       cmp      qword ptr [rdi], r10
       je       SHORT G_M000_IG07
 
G_M000_IG06:
       xor      rdi, rdi
 
G_M000_IG07:
       test     rdi, rdi
       je       G_M000_IG24
 
G_M000_IG08:
       mov      rsi, r8
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x48], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rbp-0x68], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x68]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG34
 
G_M000_IG09:
       mov      rdi, r14
       test     rdi, rdi
       je       SHORT G_M000_IG12
 
G_M000_IG10:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rdi], r9
       je       SHORT G_M000_IG12
 
G_M000_IG11:
       xor      rdi, rdi
 
G_M000_IG12:
       test     rdi, rdi
       je       G_M000_IG25
 
G_M000_IG13:
       mov      rsi, r14
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x30], xmm0
       mov      rax, gword ptr [rbp+0x38]
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG16
 
G_M000_IG14:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG16
 
G_M000_IG15:
       xor      rsi, rsi
 
G_M000_IG16:
       test     rsi, rsi
       je       SHORT G_M000_IG18
 
G_M000_IG17:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x38], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      G_M000_IG34
 
G_M000_IG18:
       mov      rsi, rax
       test     rsi, rsi
       je       SHORT G_M000_IG21
 
G_M000_IG19:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG21
 
G_M000_IG20:
       xor      rsi, rsi
 
G_M000_IG21:
       test     rsi, rsi
       je       SHORT G_M000_IG23
 
G_M000_IG22:
       mov      rsi, rax
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x70], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x70]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r14, rax
       jmp      SHORT G_M000_IG34
 
G_M000_IG23:
       mov      r8, rax
 
G_M000_IG24:
       mov      rdx, r8
       mov      rsi, r14
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      r14, rax
       jmp      SHORT G_M000_IG34
 
G_M000_IG25:
       mov      rdi, r14
       test     rdi, rdi
       je       SHORT G_M000_IG28
 
G_M000_IG26:
       mov      r9, 0xD1FFAB1E
       cmp      qword ptr [rdi], r9
       je       SHORT G_M000_IG28
 
G_M000_IG27:
       xor      rdi, rdi
 
G_M000_IG28:
       test     rdi, rdi
       je       SHORT G_M000_IG24
 
G_M000_IG29:
       mov      r9, r8
       test     r9, r9
       je       SHORT G_M000_IG32
 
G_M000_IG30:
       mov      r10, 0xD1FFAB1E
       cmp      qword ptr [r9], r10
       je       SHORT G_M000_IG32
 
G_M000_IG31:
       xor      r9, r9
 
G_M000_IG32:
       test     r9, r9
       je       SHORT G_M000_IG24
 
G_M000_IG33:
       mov      rsi, r9
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r14, rax
 
G_M000_IG34:
       test     r15, r15
       je       G_M000_IG47
 
G_M000_IG35:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG47
 
G_M000_IG36:
       mov      rax, qword ptr [r15+0x08]
       mov      qword ptr [rbp-0x88], rax
       test     r14, r14
       je       G_M000_IG42
 
G_M000_IG37:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      SHORT G_M000_IG42
 
G_M000_IG38:
       mov      r14, qword ptr [r14+0x08]
       lea      r15, [rax+r14]
       mov      rdi, rax
       xor      rdi, r15
       mov      rcx, r14
       xor      rcx, r15
       and      rdi, rcx
       jl       G_M000_IG96
       lea      rdi, [r15+0x05]
       cmp      rdi, 261
       ja       SHORT G_M000_IG41
 
G_M000_IG39:
       test     rdi, rdi
       jl       G_M000_IG98
       mov      rax, 0xD1FFAB1E
       mov      r8, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG97
       mov      r14, gword ptr [r8+8*rdi+0x10]
 
G_M000_IG40:
       mov      r15, r14
       jmp      G_M000_IG70
 
G_M000_IG41:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      r14, rax
       mov      qword ptr [r14+0x08], r15
       jmp      SHORT G_M000_IG40
 
G_M000_IG42:
       mov      rsi, r14
       test     rsi, rsi
       je       SHORT G_M000_IG45
 
G_M000_IG43:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG45
 
G_M000_IG44:
       xor      rsi, rsi
 
G_M000_IG45:
       test     rsi, rsi
       je       G_M000_IG86
 
G_M000_IG46:
       mov      rsi, r14
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x90], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rbp-0xA8], xmm1
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0xA8]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x90]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG70
 
G_M000_IG47:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG50
 
G_M000_IG48:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG50
 
G_M000_IG49:
       xor      rsi, rsi
 
G_M000_IG50:
       test     rsi, rsi
       je       G_M000_IG61
 
G_M000_IG51:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x78], xmm0
       mov      rsi, r14
       test     rsi, rsi
       je       SHORT G_M000_IG54
 
G_M000_IG52:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG54
 
G_M000_IG53:
       xor      rsi, rsi
 
G_M000_IG54:
       test     rsi, rsi
       je       SHORT G_M000_IG56
 
G_M000_IG55:
       mov      rsi, r14
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x80], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x78]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      G_M000_IG70
 
G_M000_IG56:
       mov      rsi, r14
       test     rsi, rsi
       je       SHORT G_M000_IG59
 
G_M000_IG57:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG59
 
G_M000_IG58:
       xor      rsi, rsi
 
G_M000_IG59:
       test     rsi, rsi
       je       G_M000_IG86
 
G_M000_IG60:
       mov      rsi, r14
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x78]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0xB0]
       vmovsd   qword ptr [rax+0x08], xmm0
       mov      r15, rax
       jmp      SHORT G_M000_IG70
 
G_M000_IG61:
       mov      rdi, r15
       test     rdi, rdi
       je       SHORT G_M000_IG64
 
G_M000_IG62:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG64
 
G_M000_IG63:
       xor      rdi, rdi
 
G_M000_IG64:
       test     rdi, rdi
       je       G_M000_IG86
 
G_M000_IG65:
       mov      rsi, r14
       test     rsi, rsi
       je       SHORT G_M000_IG68
 
G_M000_IG66:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       je       SHORT G_M000_IG68
 
G_M000_IG67:
       xor      rsi, rsi
 
G_M000_IG68:
       test     rsi, rsi
       je       G_M000_IG86
 
G_M000_IG69:
       call     [System.String:Concat(System.String,System.String):System.String]
       mov      r15, rax
 
G_M000_IG70:
       mov      r14, qword ptr [rbp+0x80]
       add      r13, r14
       jo       G_M000_IG98
       mov      eax, dword ptr [rbp-0xBC]
       mov      edi, eax
       cmp      r13, r12
       setl     cl
       movzx    rcx, cl
       and      ecx, edi
       cmp      r13, r12
       setg     dl
       movzx    rdx, dl
       test     edi, edi
       sete     dil
       movzx    rdi, dil
       and      edi, edx
       or       edi, ecx
       je       G_M000_IG90
 
G_M000_IG71:
       mov      rcx, qword ptr [rbp-0xC8]
       mov      edi, dword ptr [rcx]
       test     edi, edi
       jle      SHORT G_M000_IG73
 
G_M000_IG72:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rsi, rdx
       dec      edi
       mov      r8d, edi
       mov      r9d, dword ptr [rsi+0x08]
       cmp      r8d, r9d
       jae      G_M000_IG97
       cmp      dword ptr [rsi+4*r8+0x10], 21
       je       SHORT G_M000_IG73
       mov      dword ptr [rdx+4*rdi+0x10], 21
 
G_M000_IG73:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG87
 
G_M000_IG74:
       mov      rax, qword ptr [rbp-0xC8]
       mov      ecx, dword ptr [rax]
       test     ecx, ecx
       jle      SHORT G_M000_IG76
 
G_M000_IG75:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rdx, rax
       dec      ecx
       mov      edi, ecx
       mov      esi, dword ptr [rdx+0x08]
       cmp      edi, esi
       jae      G_M000_IG97
       mov      esi, edi
       cmp      dword ptr [rdx+4*rsi+0x10], 22
       je       SHORT G_M000_IG76
       mov      dword ptr [rax+4*rcx+0x10], 22
 
G_M000_IG76:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      qword ptr [rbp+0x80], r14
       mov      r14, rax
 
G_M000_IG77:
       mov      rsi, 0xD1FFAB1E
       mov      qword ptr [rbp-0xC8], rsi
       mov      edi, dword ptr [rsi]
       test     edi, edi
       jle      SHORT G_M000_IG79
 
G_M000_IG78:
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       mov      r9, r8
       dec      edi
       mov      r10d, edi
       mov      r11d, dword ptr [r9+0x08]
       cmp      r10d, r11d
       jae      G_M000_IG97
       cmp      dword ptr [r9+4*r10+0x10], 25
       je       SHORT G_M000_IG79
       mov      dword ptr [r8+4*rdi+0x10], 25
 
G_M000_IG79:
       mov      rdi, 0xD1FFAB1E
       mov      r8, gword ptr [rdi]
       mov      gword ptr [rbp+0x38], r8
       test     r14, r14
       je       G_M000_IG09
 
G_M000_IG80:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r14], rdi
       jne      G_M000_IG09
 
G_M000_IG81:
       mov      r9, qword ptr [r14+0x08]
       mov      qword ptr [rbp-0x40], r9
       test     r8, r8
       je       G_M000_IG04
 
G_M000_IG82:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r8], rdi
       jne      G_M000_IG04
 
G_M000_IG83:
       mov      r14, qword ptr [r8+0x08]
       lea      r8, [r9+r14]
       mov      qword ptr [rbp-0x50], r8
       mov      rdi, r9
       xor      rdi, r8
       mov      r10, r14
       xor      r10, r8
       and      rdi, r10
       jl       G_M000_IG95
       lea      rdi, [r8+0x05]
       cmp      rdi, 261
       ja       G_M000_IG03
 
G_M000_IG84:
       test     rdi, rdi
       jl       G_M000_IG98
       mov      r8, 0xD1FFAB1E
       mov      r8, gword ptr [r8]
       cmp      rdi, 262
       jae      G_M000_IG97
       mov      r14, gword ptr [r8+8*rdi+0x10]
 
G_M000_IG85:
       jmp      G_M000_IG34
 
G_M000_IG86:
       mov      rdx, r14
       mov      rsi, r15
       mov      edi, 257
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       mov      r15, rax
       jmp      G_M000_IG70
 
G_M000_IG87:
       mov      rcx, qword ptr [rbp-0xC8]
       mov      ecx, dword ptr [rcx]
       test     ecx, ecx
       jle      SHORT G_M000_IG89
 
G_M000_IG88:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rdx, rax
       dec      ecx
       mov      edi, ecx
       mov      esi, dword ptr [rdx+0x08]
       cmp      edi, esi
       jae      G_M000_IG97
       cmp      dword ptr [rdx+4*rdi+0x10], 24
       je       SHORT G_M000_IG89
       mov      dword ptr [rax+4*rcx+0x10], 24
 
G_M000_IG89:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      qword ptr [rbp+0x80], r14
       mov      r14, rax
       jmp      G_M000_IG77
 
G_M000_IG90:
       mov      rbx, qword ptr [rbp-0xC8]
       mov      edi, dword ptr [rbx]
       test     edi, edi
       jle      SHORT G_M000_IG92
 
G_M000_IG91:
       mov      rax, 0xD1FFAB1E
       mov      rdx, gword ptr [rax]
       mov      rax, rdx
       lea      ecx, [rdi-0x01]
       mov      esi, ecx
       mov      r9d, dword ptr [rax+0x08]
       cmp      esi, r9d
       jae      G_M000_IG97
       cmp      dword ptr [rax+4*rsi+0x10], 26
       je       SHORT G_M000_IG92
       mov      rax, rdx
       mov      dword ptr [rax+4*rcx+0x10], 26
 
G_M000_IG92:
       mov      r14, qword ptr [rbp+0xB8]
       test     r14, r14
       jle      SHORT G_M000_IG93
       movsxd   rax, edi
       cmp      r14, rax
       jg       SHORT G_M000_IG93
       dec      r14d
       mov      dword ptr [rbx], r14d
 
G_M000_IG93:
       mov      rax, r15
 
G_M000_IG94:
       add      rsp, 440
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG95:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu  xmmword ptr [rbp-0x60], xmm0
       lea      rdi, [rbp-0x60]
       mov      rsi, r9
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      rax, gword ptr [rbp-0x60]
       mov      gword ptr [rbp-0xD0], rax
       mov      ecx, dword ptr [rbp-0x58]
       mov      dword ptr [rbp-0xB4], ecx
       mov      rdi, r14
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, gword ptr [rbp-0xD0]
       mov      esi, dword ptr [rbp-0xB4]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r14, rax
       jmp      G_M000_IG85
 
G_M000_IG96:
       vxorps   xmm0, xmm0, xmm0
       vmovdqu32 xmmword ptr [rbp-0xA0], xmm0
       lea      rdi, [rbp-0xA0]
       mov      rsi, rax
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r15, gword ptr [rbp-0xA0]
       mov      eax, dword ptr [rbp-0x98]
       mov      dword ptr [rbp-0xB8], eax
       mov      rdi, r14
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, r15
       mov      esi, dword ptr [rbp-0xB8]
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       mov      r14, rax
       jmp      G_M000_IG40
 
G_M000_IG97:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG98:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 2189

