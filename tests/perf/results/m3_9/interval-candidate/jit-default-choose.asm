; Assembly listing for method RyuPy.Generated.Module:m0_choose(System.Object):System.Object (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 96
       lea      rbp, [rsp+0x60]
       vxorps   xmm8, xmm8, xmm8
       vmovdqu32 zmmword ptr [rbp-0x60], zmm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       xor      eax, eax
       mov      qword ptr [rbp-0x10], rax
       mov      gword ptr [rbp-0x08], rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x18], rax
       mov      edi, 6
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x1C], eax
       cmp      dword ptr [rbp-0x1C], 0
       jne      SHORT G_M000_IG03
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      edi, 7
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x28], rax
       mov      rax, gword ptr [rbp-0x28]
       mov      gword ptr [rbp-0x10], rax
       jmp      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x30], rax
       mov      rax, gword ptr [rbp-0x30]
       mov      gword ptr [rbp-0x10], rax
 
G_M000_IG05:
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x40], rax
       mov      rsi, gword ptr [rbp-0x10]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x50], rax
       mov      rsi, gword ptr [rbp-0x40]
       mov      rdi, 0xD1FFAB1E
       call     [CORINFO_HELP_UNBOX]
       mov      rax, qword ptr [rax]
       mov      qword ptr [rbp-0x58], rax
       mov      rax, qword ptr [rbp-0x50]
       add      rax, qword ptr [rbp-0x58]
       jo       SHORT G_M000_IG07
       mov      qword ptr [rbp-0x60], rax
       mov      rdi, qword ptr [rbp-0x60]
       call     [RyuPy.Runtime.PyOps:BoxInt(long):System.Object]
       mov      gword ptr [rbp-0x48], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x38], rax
       mov      rdi, qword ptr [rbp-0x18]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x38]
 
G_M000_IG06:
       add      rsp, 96
       pop      rbp
       ret      
 
G_M000_IG07:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 320

; Assembly listing for method RyuPy.Generated.Module:m0_choose_unknown(System.Object,System.Object):System.Object (Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 80
       lea      rbp, [rsp+0x50]
       xor      eax, eax
       mov      qword ptr [rbp-0x48], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x20], rax
       mov      edi, 29
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x24], eax
       cmp      dword ptr [rbp-0x24], 0
       jne      SHORT G_M000_IG03
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      edi, 30
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x10]
       mov      gword ptr [rbp-0x18], rax
       jmp      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      edi, 32
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x30], rax
       mov      rax, gword ptr [rbp-0x30]
       mov      gword ptr [rbp-0x18], rax
 
G_M000_IG05:
       mov      edi, 33
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x40], rax
       mov      rdx, gword ptr [rbp-0x18]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 1
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x48], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x38], rax
       mov      rdi, qword ptr [rbp-0x20]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x38]
 
G_M000_IG06:
       add      rsp, 80
       pop      rbp
       ret      
 
; Total bytes of code 242

; Assembly listing for method RyuPy.Generated.Module:m0_choose_unknown(System.Object,System.Object):System.Object (Instrumented Tier0)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Instrumented Tier0 code
; rbp based frame
; partially interruptible
; compiling with minopt

G_M000_IG01:
       push     rbp
       sub      rsp, 80
       lea      rbp, [rsp+0x50]
       xor      eax, eax
       mov      qword ptr [rbp-0x48], rax
       vxorps   xmm8, xmm8, xmm8
       vmovdqu  ymmword ptr [rbp-0x40], ymm8
       vmovdqa  xmmword ptr [rbp-0x20], xmm8
       mov      gword ptr [rbp-0x08], rdi
       mov      gword ptr [rbp-0x10], rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x20], rax
       mov      edi, 29
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, gword ptr [rbp-0x08]
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       mov      dword ptr [rbp-0x24], eax
       cmp      dword ptr [rbp-0x24], 0
       jne      SHORT G_M000_IG03
       jmp      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 30
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, gword ptr [rbp-0x10]
       mov      gword ptr [rbp-0x18], rax
       jmp      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      edi, 32
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x30], rax
       mov      rax, gword ptr [rbp-0x30]
       mov      gword ptr [rbp-0x18], rax
 
G_M000_IG05:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 33
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      gword ptr [rbp-0x40], rax
       mov      rdx, gword ptr [rbp-0x18]
       mov      rcx, gword ptr [rbp-0x40]
       mov      edi, 1
       mov      esi, 1
       call     [RyuPy.Runtime.PyPic:Binary(int,int,System.Object,System.Object):System.Object]
       mov      gword ptr [rbp-0x48], rax
       mov      rax, gword ptr [rbp-0x48]
       mov      gword ptr [rbp-0x38], rax
       mov      rdi, qword ptr [rbp-0x20]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, gword ptr [rbp-0x38]
 
G_M000_IG06:
       add      rsp, 80
       pop      rbp
       ret      
 
; Total bytes of code 272

; Assembly listing for method RyuPy.Generated.Module:m0_choose_unknown(System.Object,System.Object):System.Object (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; partially interruptible
; with Synthesized PGO: fgCalledCount is 10272
; 10 inlinees with PGO data; 0 single block inlinees; 5 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     r13
       push     r12
       push     rbx
       sub      rsp, 56
       lea      rbp, [rsp+0x60]
       xor      eax, eax
       mov      qword ptr [rbp-0x50], rax
       mov      qword ptr [rbp-0x48], rax
       mov      rbx, rdi
       mov      r15, rsi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r14, rax
       mov      r13, 0xD1FFAB1E
       mov      edi, dword ptr [r13]
       test     edi, edi
       jle      SHORT G_M000_IG05
 
G_M000_IG03:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG53
       cmp      dword ptr [rcx+4*rdx+0x10], 29
       je       SHORT G_M000_IG05
 
G_M000_IG04:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG53
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 29
 
G_M000_IG05:
       mov      rdi, rbx
       call     [RyuPy.Runtime.PyOps:Truthy(System.Object):bool]
       test     eax, eax
       je       G_M000_IG50
 
G_M000_IG06:
       mov      edi, dword ptr [r13]
       test     edi, edi
       jle      SHORT G_M000_IG09
 
G_M000_IG07:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG53
       cmp      dword ptr [rcx+4*rdx+0x10], 30
       je       SHORT G_M000_IG09
 
G_M000_IG08:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG53
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 30
 
G_M000_IG09:
       mov      edi, dword ptr [r13]
       test     edi, edi
       jle      SHORT G_M000_IG12
 
G_M000_IG10:
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       mov      rcx, rax
       dec      edi
       mov      edx, edi
       cmp      edx, dword ptr [rcx+0x08]
       jae      G_M000_IG53
       cmp      dword ptr [rcx+4*rdx+0x10], 33
       je       SHORT G_M000_IG12
 
G_M000_IG11:
       cmp      edi, dword ptr [rax+0x08]
       jae      G_M000_IG53
       mov      edi, edi
       mov      dword ptr [rax+4*rdi+0x10], 33
 
G_M000_IG12:
       mov      rdi, 0xD1FFAB1E
       mov      rbx, gword ptr [rdi]
       test     r15, r15
       je       G_M000_IG23
 
G_M000_IG13:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [r15], rdi
       jne      G_M000_IG23
 
G_M000_IG14:
       mov      r12, qword ptr [r15+0x08]
       test     rbx, rbx
       je       SHORT G_M000_IG18
 
G_M000_IG15:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rbx], rdi
       jne      SHORT G_M000_IG18
 
G_M000_IG16:
       mov      rbx, qword ptr [rbx+0x08]
       lea      r15, [r12+rbx]
       mov      rdi, r12
       xor      rdi, r15
       mov      rax, rbx
       xor      rax, r15
       and      rdi, rax
       jl       G_M000_IG51
       lea      rdi, [r15+0x05]
       cmp      rdi, 261
       ja       G_M000_IG52
       test     rdi, rdi
       jl       G_M000_IG54
       mov      rax, 0xD1FFAB1E
       mov      rax, gword ptr [rax]
       cmp      rdi, 262
       jae      G_M000_IG53
       mov      rax, gword ptr [rax+8*rdi+0x10]
 
G_M000_IG17:
       jmp      G_M000_IG46
 
G_M000_IG18:
       mov      rsi, rbx
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
       je       G_M000_IG49
 
G_M000_IG22:
       mov      rsi, rbx
       mov      rbx, 0xD1FFAB1E
       mov      rdi, rbx
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x40], xmm0
       vxorps   xmm1, xmm1, xmm1
       vcvtsi2sd xmm1, xmm1, r12
       vmovsd   qword ptr [rbp-0x58], xmm1
       mov      rdi, rbx
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm1, qword ptr [rbp-0x58]
       vaddsd   xmm0, xmm1, qword ptr [rbp-0x40]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG46
 
G_M000_IG23:
       mov      rsi, r15
       test     rsi, rsi
       je       SHORT G_M000_IG26
 
G_M000_IG24:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG26
 
G_M000_IG25:
       xor      rsi, rsi
 
G_M000_IG26:
       test     rsi, rsi
       je       G_M000_IG37
 
G_M000_IG27:
       mov      rsi, r15
       mov      rdi, 0xD1FFAB1E
       mov      r12, rdi
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x30], xmm0
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG30
 
G_M000_IG28:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG30
 
G_M000_IG29:
       xor      rsi, rsi
 
G_M000_IG30:
       test     rsi, rsi
       je       SHORT G_M000_IG32
 
G_M000_IG31:
       mov      rsi, rbx
       mov      rdi, r12
       call     CORINFO_HELP_UNBOX
       vmovsd   xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x38], xmm0
       mov      rdi, r12
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      G_M000_IG46
 
G_M000_IG32:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG35
 
G_M000_IG33:
       mov      rdi, 0xD1FFAB1E
       cmp      qword ptr [rsi], rdi
       je       SHORT G_M000_IG35
 
G_M000_IG34:
       xor      rsi, rsi
 
G_M000_IG35:
       test     rsi, rsi
       je       G_M000_IG49
 
G_M000_IG36:
       mov      rsi, rbx
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_UNBOX
       vxorps   xmm0, xmm0, xmm0
       vcvtsi2sd xmm0, xmm0, qword ptr [rax]
       vmovsd   qword ptr [rbp-0x60], xmm0
       mov      rdi, r12
       call     CORINFO_HELP_NEWSFAST
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x60]
       vmovsd   qword ptr [rax+0x08], xmm0
       jmp      SHORT G_M000_IG46
 
G_M000_IG37:
       mov      rdi, r15
       test     rdi, rdi
       je       SHORT G_M000_IG40
 
G_M000_IG38:
       mov      rsi, 0xD1FFAB1E
       cmp      qword ptr [rdi], rsi
       je       SHORT G_M000_IG40
 
G_M000_IG39:
       xor      rdi, rdi
 
G_M000_IG40:
       test     rdi, rdi
       je       SHORT G_M000_IG49
 
G_M000_IG41:
       mov      rsi, rbx
       test     rsi, rsi
       je       SHORT G_M000_IG44
 
G_M000_IG42:
       mov      rax, 0xD1FFAB1E
       cmp      qword ptr [rsi], rax
       je       SHORT G_M000_IG44
 
G_M000_IG43:
       xor      rsi, rsi
 
G_M000_IG44:
       test     rsi, rsi
       je       SHORT G_M000_IG49
 
G_M000_IG45:
       call     [System.String:Concat(System.String,System.String):System.String]
 
G_M000_IG46:
       test     r14, r14
       jle      SHORT G_M000_IG48
 
G_M000_IG47:
       movsxd   rcx, dword ptr [r13]
       cmp      r14, rcx
       jg       SHORT G_M000_IG48
       dec      r14d
       mov      dword ptr [r13], r14d
 
G_M000_IG48:
       add      rsp, 56
       pop      rbx
       pop      r12
       pop      r13
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG49:
       mov      rdx, rbx
       mov      rsi, r15
       mov      edi, 1
       call     [RyuPy.Runtime.PyOps:Binary(int,System.Object,System.Object):System.Object]
       jmp      SHORT G_M000_IG46
 
G_M000_IG50:
       mov      edi, 32
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rdi, 0xD1FFAB1E
       mov      r15, gword ptr [rdi]
       jmp      G_M000_IG09
 
G_M000_IG51:
       lea      rdi, [rbp-0x50]
       mov      rsi, r12
       call     [System.Numerics.BigInteger:.ctor(long):this]
       mov      r12, gword ptr [rbp-0x50]
       mov      r15d, dword ptr [rbp-0x48]
       mov      rdi, rbx
       call     [System.Numerics.BigInteger:op_Implicit(long):System.Numerics.BigInteger]
       mov      ecx, edx
       mov      rdi, r12
       mov      esi, r15d
       mov      rdx, rax
       call     [System.Numerics.BigInteger:op_Addition(System.Numerics.BigInteger,System.Numerics.BigInteger):System.Numerics.BigInteger]
       mov      rdi, rax
       mov      esi, edx
       call     [RyuPy.Runtime.PyInt:Normalize(System.Numerics.BigInteger):System.Object]
       jmp      G_M000_IG17
 
G_M000_IG52:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_NEWSFAST
       mov      qword ptr [rax+0x08], r15
       jmp      G_M000_IG17
 
G_M000_IG53:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG54:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
; Total bytes of code 1024

