; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Instrumented Tier0)
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
       vmovdqa  xmmword ptr [rbp-0x50], xmm8
       vmovsd   qword ptr [rbp-0x30], xmm0
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      qword ptr [rbp-0x40], rdi
 
G_M000_IG02:
       mov      dword ptr [rbp-0xD8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x70], rax
       mov      edi, 4
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       xor      eax, eax
       mov      qword ptr [rbp-0x48], rax
       mov      edi, 5
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       xor      eax, eax
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 6
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       xor      eax, eax
       mov      qword ptr [rbp-0x58], rax
       mov      edi, 7
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0xD8]
       dec      eax
       mov      dword ptr [rbp-0xD8], eax
       cmp      dword ptr [rbp-0xD8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xD8]
       mov      esi, 76
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       mov      rax, qword ptr [rbp-0x58]
       cmp      rax, qword ptr [rbp-0x40]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x74], eax
       cmp      dword ptr [rbp-0x74], 0
       jne      SHORT G_M000_IG06
       jmp      G_M000_IG07
 
G_M000_IG06:
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rbp-0x80], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rbp-0x60], xmm0
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm0, qword ptr [rbp-0x50]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rbp-0x88], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rbp-0x68], xmm0
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm0, qword ptr [rbp-0x60]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rbp-0x90], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vucomisd xmm0, qword ptr [reloc @RWD00]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       cmp      dword ptr [rbp-0x94], 0
       jne      SHORT G_M000_IG08
       jmp      SHORT G_M000_IG09
 
G_M000_IG07:
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x40]
       mov      qword ptr [rbp-0xA0], rax
       jmp      G_M000_IG10
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0xA0], rax
       jmp      G_M000_IG10
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [reloc @RWD08]
       vmovsd   qword ptr [rbp-0xA8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0xA8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0xB0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rbp-0x50], xmm0
       mov      edi, 13
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm0, qword ptr [rbp-0x60]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rbp-0xC0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0xC0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x30]
       vmovsd   qword ptr [rbp-0xC8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rbp-0x48], xmm0
       mov      edi, 14
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x58]
       add      rax, 1
       jo       SHORT G_M000_IG12
       mov      qword ptr [rbp-0xD0], rax
       mov      rax, qword ptr [rbp-0xD0]
       mov      qword ptr [rbp-0x58], rax
       jmp      G_M000_IG03
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x70]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, qword ptr [rbp-0xA0]
 
G_M000_IG11:
       add      rsp, 224
       pop      rbp
       ret      
 
G_M000_IG12:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
RWD00  	dq	4010000000000000h	;            4
RWD08  	dq	4000000000000000h	;            2

; Total bytes of code 687

; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x4c
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 10
; 0 inlinees with PGO data; 0 single block inlinees; 13 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       mov      rbp, rsp
       vmovsd   xmm0, qword ptr [rbp+0xC0]
       vmovsd   xmm1, qword ptr [rbp+0xB8]
       mov      rax, qword ptr [rbp+0xB0]
       vmovsd   xmm2, qword ptr [rbp+0xA8]
       vmovsd   xmm3, qword ptr [rbp+0xA0]
       mov      rcx, qword ptr [rbp+0x98]
       mov      rdx, qword ptr [rbp+0x80]
 
G_M000_IG02:
       cmp      rcx, rax
       jl       G_M000_IG22
 
G_M000_IG03:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG06
 
G_M000_IG04:
       mov      rcx, 0xD1FFAB1E
       mov      rsi, gword ptr [rcx]
       mov      rcx, rsi
       lea      r9d, [rdi-0x01]
       mov      r8d, r9d
       mov      r11d, dword ptr [rcx+0x08]
       cmp      r8d, r11d
       jae      G_M000_IG34
       cmp      dword ptr [rcx+4*r8+0x10], 15
       je       SHORT G_M000_IG06
 
G_M000_IG05:
       mov      rcx, rsi
       mov      esi, r9d
       mov      dword ptr [rcx+4*rsi+0x10], 15
 
G_M000_IG06:
       test     rdx, rdx
       jle      SHORT G_M000_IG09
 
G_M000_IG07:
       movsxd   rcx, edi
       cmp      rdx, rcx
       jg       SHORT G_M000_IG09
 
G_M000_IG08:
       dec      edx
       mov      dword ptr [(reloc)], edx
 
G_M000_IG09:
       add      rsp, 240
       pop      rbp
       ret      
 
G_M000_IG10:
       test     edi, edi
       jle      SHORT G_M000_IG13
 
G_M000_IG11:
       mov      rax, 0xD1FFAB1E
       mov      rsi, gword ptr [rax]
       mov      rax, rsi
       lea      r9d, [rdi-0x01]
       mov      r8d, r9d
       mov      r11d, dword ptr [rax+0x08]
       cmp      r8d, r11d
       jae      G_M000_IG34
       mov      r11d, r8d
       cmp      dword ptr [rax+4*r11+0x10], 11
       je       SHORT G_M000_IG13
 
G_M000_IG12:
       mov      eax, r9d
       mov      dword ptr [rsi+4*rax+0x10], 11
 
G_M000_IG13:
       mov      rax, rcx
       jmp      SHORT G_M000_IG06
       align    [0 bytes for IG14]
 
G_M000_IG14:
       mov      r8d, r9d
       mov      dword ptr [rsi+4*r8+0x10], 12
 
G_M000_IG15:
       vaddsd   xmm2, xmm2, xmm2
       vmulsd   xmm3, xmm2, xmm3
       vaddsd   xmm3, xmm3, xmm1
       test     edi, edi
       jle      SHORT G_M000_IG18
 
G_M000_IG16:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       lea      r9d, [rdi-0x01]
       mov      r10d, r9d
       mov      r11d, dword ptr [r8+0x08]
       cmp      r10d, r11d
       jae      G_M000_IG34
       mov      r11d, r10d
       cmp      dword ptr [r8+4*r11+0x10], 13
       je       SHORT G_M000_IG18
 
G_M000_IG17:
       mov      r8d, r9d
       mov      dword ptr [rsi+4*r8+0x10], 13
 
G_M000_IG18:
       vsubsd   xmm2, xmm4, xmm5
       vaddsd   xmm2, xmm2, xmm0
       test     edi, edi
       jle      SHORT G_M000_IG21
 
G_M000_IG19:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       lea      r9d, [rdi-0x01]
       mov      edi, r9d
       mov      r11d, dword ptr [r8+0x08]
       cmp      edi, r11d
       jae      G_M000_IG34
       mov      r11d, edi
       cmp      dword ptr [r8+4*r11+0x10], 14
       je       SHORT G_M000_IG21
 
G_M000_IG20:
       mov      edi, r9d
       mov      dword ptr [rsi+4*rdi+0x10], 14
 
G_M000_IG21:
       add      rcx, 1
       jo       G_M000_IG35
       cmp      rcx, rax
       jge      G_M000_IG03
 
G_M000_IG22:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG25
 
G_M000_IG23:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       lea      r9d, [rdi-0x01]
       mov      r10d, r9d
       mov      r11d, dword ptr [r8+0x08]
       cmp      r10d, r11d
       jae      G_M000_IG34
       mov      r11d, r10d
       cmp      dword ptr [r8+4*r11+0x10], 8
       je       SHORT G_M000_IG25
 
G_M000_IG24:
       mov      r8d, r9d
       mov      dword ptr [rsi+4*r8+0x10], 8
 
G_M000_IG25:
       vmulsd   xmm4, xmm2, xmm2
       test     edi, edi
       jle      SHORT G_M000_IG28
 
G_M000_IG26:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       lea      r9d, [rdi-0x01]
       mov      r10d, r9d
       mov      r11d, dword ptr [r8+0x08]
       cmp      r10d, r11d
       jae      G_M000_IG34
       mov      r11d, r10d
       cmp      dword ptr [r8+4*r11+0x10], 9
       je       SHORT G_M000_IG28
 
G_M000_IG27:
       mov      r8d, r9d
       mov      dword ptr [rsi+4*r8+0x10], 9
 
G_M000_IG28:
       vmulsd   xmm5, xmm3, xmm3
       test     edi, edi
       jle      SHORT G_M000_IG31
 
G_M000_IG29:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       lea      r9d, [rdi-0x01]
       mov      r10d, r9d
       mov      r11d, dword ptr [r8+0x08]
       cmp      r10d, r11d
       jae      SHORT G_M000_IG34
       mov      r11d, r10d
       cmp      dword ptr [r8+4*r11+0x10], 10
       je       SHORT G_M000_IG31
 
G_M000_IG30:
       mov      r8d, r9d
       mov      dword ptr [rsi+4*r8+0x10], 10
 
G_M000_IG31:
       vaddsd   xmm6, xmm4, xmm5
       vucomisd xmm6, qword ptr [reloc @RWD00]
       ja       G_M000_IG10
 
G_M000_IG32:
       test     edi, edi
       jle      G_M000_IG15
 
G_M000_IG33:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       lea      r9d, [rdi-0x01]
       mov      r10d, r9d
       mov      r11d, dword ptr [r8+0x08]
       cmp      r10d, r11d
       jae      SHORT G_M000_IG34
       mov      r11d, r10d
       cmp      dword ptr [r8+4*r11+0x10], 12
       je       G_M000_IG15
       jmp      G_M000_IG14
 
G_M000_IG34:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG35:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
RWD00  	dq	4010000000000000h	;            4

; Total bytes of code 696

; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Instrumented Tier0)
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
       vmovdqa  xmmword ptr [rbp-0x50], xmm8
       vmovsd   qword ptr [rbp-0x30], xmm0
       vmovsd   qword ptr [rbp-0x38], xmm1
       mov      qword ptr [rbp-0x40], rdi
 
G_M000_IG02:
       mov      dword ptr [rbp-0xD8], 0x3E8
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      qword ptr [rbp-0x70], rax
       mov      edi, 4
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       xor      eax, eax
       mov      qword ptr [rbp-0x48], rax
       mov      edi, 5
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       xor      eax, eax
       mov      qword ptr [rbp-0x50], rax
       mov      edi, 6
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       xor      eax, eax
       mov      qword ptr [rbp-0x58], rax
       mov      edi, 7
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
 
G_M000_IG03:
       mov      eax, dword ptr [rbp-0xD8]
       dec      eax
       mov      dword ptr [rbp-0xD8], eax
       cmp      dword ptr [rbp-0xD8], 0
       jg       SHORT G_M000_IG05
 
G_M000_IG04:
       lea      rdi, [rbp-0xD8]
       mov      esi, 76
       call     CORINFO_HELP_PATCHPOINT
 
G_M000_IG05:
       mov      rax, qword ptr [rbp-0x58]
       cmp      rax, qword ptr [rbp-0x40]
       setl     al
       movzx    rax, al
       mov      dword ptr [rbp-0x74], eax
       cmp      dword ptr [rbp-0x74], 0
       jne      SHORT G_M000_IG06
       jmp      G_M000_IG07
 
G_M000_IG06:
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x48]
       vmovsd   qword ptr [rbp-0x80], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x80]
       vmovsd   qword ptr [rbp-0x60], xmm0
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm0, qword ptr [rbp-0x50]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rbp-0x88], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x88]
       vmovsd   qword ptr [rbp-0x68], xmm0
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm0, qword ptr [rbp-0x60]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rbp-0x90], xmm0
       vmovsd   xmm0, qword ptr [rbp-0x90]
       vucomisd xmm0, qword ptr [reloc @RWD00]
       seta     al
       movzx    rax, al
       mov      dword ptr [rbp-0x94], eax
       cmp      dword ptr [rbp-0x94], 0
       jne      SHORT G_M000_IG08
       jmp      SHORT G_M000_IG09
 
G_M000_IG07:
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x40]
       mov      qword ptr [rbp-0xA0], rax
       jmp      G_M000_IG10
 
G_M000_IG08:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x58]
       mov      qword ptr [rbp-0xA0], rax
       jmp      G_M000_IG10
 
G_M000_IG09:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm0, qword ptr [rbp-0x48]
       vmulsd   xmm0, xmm0, qword ptr [reloc @RWD08]
       vmovsd   qword ptr [rbp-0xA8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0xA8]
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x50]
       vmovsd   qword ptr [rbp-0xB0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0xB0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rbp-0xB8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0xB8]
       vmovsd   qword ptr [rbp-0x50], xmm0
       mov      edi, 13
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm0, qword ptr [rbp-0x60]
       vsubsd   xmm0, xmm0, qword ptr [rbp-0x68]
       vmovsd   qword ptr [rbp-0xC0], xmm0
       vmovsd   xmm0, qword ptr [rbp-0xC0]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x30]
       vmovsd   qword ptr [rbp-0xC8], xmm0
       vmovsd   xmm0, qword ptr [rbp-0xC8]
       vmovsd   qword ptr [rbp-0x48], xmm0
       mov      edi, 14
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      rax, qword ptr [rbp-0x58]
       add      rax, 1
       jo       SHORT G_M000_IG12
       mov      qword ptr [rbp-0xD0], rax
       mov      rax, qword ptr [rbp-0xD0]
       mov      qword ptr [rbp-0x58], rax
       jmp      G_M000_IG03
 
G_M000_IG10:
       mov      rdi, 0xD1FFAB1E
       call     CORINFO_HELP_COUNTPROFILE32
       mov      rdi, qword ptr [rbp-0x70]
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, qword ptr [rbp-0xA0]
 
G_M000_IG11:
       add      rsp, 224
       pop      rbp
       ret      
 
G_M000_IG12:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
RWD00  	dq	4010000000000000h	;            4
RWD08  	dq	4000000000000000h	;            2

; Total bytes of code 687

; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Tier1-OSR)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1-OSR code
; OSR variant for entry point 0x4c
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 896
; 12 inlinees with PGO data; 0 single block inlinees; 1 inlinees without PGO data

G_M000_IG01:
       mov      rax, qword ptr [rbp]
       push     rax
       mov      rbp, rsp
       vmovsd   xmm0, qword ptr [rbp+0xC0]
       vmovsd   xmm1, qword ptr [rbp+0xB8]
       mov      rax, qword ptr [rbp+0xB0]
       vmovsd   xmm2, qword ptr [rbp+0xA8]
       vmovsd   xmm3, qword ptr [rbp+0xA0]
       mov      rcx, qword ptr [rbp+0x98]
       mov      rdx, qword ptr [rbp+0x80]
 
G_M000_IG02:
       cmp      rcx, rax
       jge      G_M000_IG17
       align    [0 bytes for IG03]
 
G_M000_IG03:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG05
 
G_M000_IG04:
       mov      rsi, 0xD1FFAB1E
       mov      rsi, gword ptr [rsi]
       mov      r8, rsi
       lea      r9d, [rdi-0x01]
       mov      r10d, dword ptr [r8+0x08]
       cmp      r9d, r10d
       jae      G_M000_IG26
       cmp      dword ptr [r8+4*r9+0x10], 8
       je       SHORT G_M000_IG05
       mov      dword ptr [rsi+4*r9+0x10], 8
 
G_M000_IG05:
       vmulsd   xmm4, xmm2, xmm2
       test     edi, edi
       jle      SHORT G_M000_IG07
 
G_M000_IG06:
       mov      r10, 0xD1FFAB1E
       mov      rsi, gword ptr [r10]
       mov      r8, rsi
       lea      r9d, [rdi-0x01]
       mov      r10d, dword ptr [r8+0x08]
       cmp      r9d, r10d
       jae      G_M000_IG26
       cmp      dword ptr [r8+4*r9+0x10], 9
       je       SHORT G_M000_IG07
       mov      dword ptr [rsi+4*r9+0x10], 9
 
G_M000_IG07:
       vmulsd   xmm5, xmm3, xmm3
       test     edi, edi
       jle      SHORT G_M000_IG09
 
G_M000_IG08:
       mov      r10, 0xD1FFAB1E
       mov      rsi, gword ptr [r10]
       mov      r8, rsi
       lea      r9d, [rdi-0x01]
       mov      r10d, dword ptr [r8+0x08]
       cmp      r9d, r10d
       jae      G_M000_IG26
       cmp      dword ptr [r8+4*r9+0x10], 10
       je       SHORT G_M000_IG09
       mov      dword ptr [rsi+4*r9+0x10], 10
 
G_M000_IG09:
       vaddsd   xmm6, xmm4, xmm5
       vucomisd xmm6, qword ptr [reloc @RWD00]
       ja       G_M000_IG23
 
G_M000_IG10:
       test     edi, edi
       jle      SHORT G_M000_IG12
 
G_M000_IG11:
       mov      r10, 0xD1FFAB1E
       mov      rsi, gword ptr [r10]
       mov      r8, rsi
       lea      r9d, [rdi-0x01]
       mov      r10d, dword ptr [r8+0x08]
       cmp      r9d, r10d
       jae      G_M000_IG26
       cmp      dword ptr [r8+4*r9+0x10], 12
       je       SHORT G_M000_IG12
       mov      dword ptr [rsi+4*r9+0x10], 12
 
G_M000_IG12:
       vaddsd   xmm2, xmm2, xmm2
       vmulsd   xmm3, xmm2, xmm3
       vaddsd   xmm3, xmm3, xmm1
       test     edi, edi
       jle      SHORT G_M000_IG14
 
G_M000_IG13:
       mov      r10, 0xD1FFAB1E
       mov      rsi, gword ptr [r10]
       mov      r8, rsi
       lea      r9d, [rdi-0x01]
       mov      r10d, dword ptr [r8+0x08]
       cmp      r9d, r10d
       jae      G_M000_IG26
       cmp      dword ptr [r8+4*r9+0x10], 13
       je       SHORT G_M000_IG14
       mov      dword ptr [rsi+4*r9+0x10], 13
 
G_M000_IG14:
       vsubsd   xmm2, xmm4, xmm5
       vaddsd   xmm2, xmm2, xmm0
       test     edi, edi
       jle      SHORT G_M000_IG16
 
G_M000_IG15:
       mov      r10, 0xD1FFAB1E
       mov      rsi, gword ptr [r10]
       mov      r8, rsi
       lea      r9d, [rdi-0x01]
       mov      edi, r9d
       mov      r10d, dword ptr [r8+0x08]
       cmp      edi, r10d
       jae      G_M000_IG26
       mov      r9d, edi
       cmp      dword ptr [r8+4*r9+0x10], 14
       je       SHORT G_M000_IG16
       mov      dword ptr [rsi+4*r9+0x10], 14
 
G_M000_IG16:
       add      rcx, 1
       jo       G_M000_IG27
       cmp      rcx, rax
       jl       G_M000_IG03
 
G_M000_IG17:
       mov      edi, dword ptr [(reloc)]
       test     edi, edi
       jle      SHORT G_M000_IG19
 
G_M000_IG18:
       mov      r10, 0xD1FFAB1E
       mov      rsi, gword ptr [r10]
       mov      rcx, rsi
       lea      r9d, [rdi-0x01]
       mov      r8d, r9d
       mov      r10d, dword ptr [rcx+0x08]
       cmp      r8d, r10d
       jae      SHORT G_M000_IG26
       mov      r9d, r8d
       cmp      dword ptr [rcx+4*r9+0x10], 15
       je       SHORT G_M000_IG19
       mov      rcx, rsi
       mov      dword ptr [rcx+4*r9+0x10], 15
 
G_M000_IG19:
       test     rdx, rdx
       jle      SHORT G_M000_IG22
 
G_M000_IG20:
       movsxd   rcx, edi
       cmp      rdx, rcx
       jg       SHORT G_M000_IG22
 
G_M000_IG21:
       dec      edx
       mov      dword ptr [(reloc)], edx
 
G_M000_IG22:
       add      rsp, 240
       pop      rbp
       ret      
 
G_M000_IG23:
       test     edi, edi
       jle      SHORT G_M000_IG25
 
G_M000_IG24:
       mov      r10, 0xD1FFAB1E
       mov      rsi, gword ptr [r10]
       mov      rax, rsi
       lea      r9d, [rdi-0x01]
       mov      r10d, dword ptr [rax+0x08]
       cmp      r9d, r10d
       jae      SHORT G_M000_IG26
       cmp      dword ptr [rax+4*r9+0x10], 11
       je       SHORT G_M000_IG25
       mov      dword ptr [rsi+4*r9+0x10], 11
 
G_M000_IG25:
       mov      rax, rcx
       jmp      SHORT G_M000_IG19
 
G_M000_IG26:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG27:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
RWD00  	dq	4010000000000000h	;            4

; Total bytes of code 629

; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (Tier1)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; Tier1 code
; optimized code
; optimized using Synthesized PGO
; rbp based frame
; fully interruptible
; with Synthesized PGO: fgCalledCount is 929
; 12 inlinees with PGO data; 0 single block inlinees; 1 inlinees without PGO data

G_M000_IG01:
       push     rbp
       push     rbx
       sub      rsp, 24
       lea      rbp, [rsp+0x20]
       vmovsd   qword ptr [rbp-0x10], xmm0
       vmovsd   qword ptr [rbp-0x18], xmm1
       mov      rbx, rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      ecx, dword ptr [(reloc)]
       test     ecx, ecx
       jle      SHORT G_M000_IG04
 
G_M000_IG03:
       mov      rdx, 0xD1FFAB1E
       mov      rdx, gword ptr [rdx]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, dword ptr [rdi+0x08]
       cmp      esi, r8d
       jae      G_M000_IG38
       cmp      dword ptr [rdi+4*rsi+0x10], 4
       je       SHORT G_M000_IG04
       mov      dword ptr [rdx+4*rsi+0x10], 4
 
G_M000_IG04:
       vxorps   xmm0, xmm0, xmm0
       test     ecx, ecx
       jle      SHORT G_M000_IG06
 
G_M000_IG05:
       mov      r8, 0xD1FFAB1E
       mov      rdx, gword ptr [r8]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, dword ptr [rdi+0x08]
       cmp      esi, r8d
       jae      G_M000_IG38
       cmp      dword ptr [rdi+4*rsi+0x10], 5
       je       SHORT G_M000_IG06
       mov      dword ptr [rdx+4*rsi+0x10], 5
 
G_M000_IG06:
       vxorps   xmm1, xmm1, xmm1
       test     ecx, ecx
       jle      SHORT G_M000_IG08
 
G_M000_IG07:
       mov      r8, 0xD1FFAB1E
       mov      rdx, gword ptr [r8]
       mov      rdi, rdx
       lea      esi, [rcx-0x01]
       mov      r8d, dword ptr [rdi+0x08]
       cmp      esi, r8d
       jae      G_M000_IG38
       cmp      dword ptr [rdi+4*rsi+0x10], 6
       je       SHORT G_M000_IG08
       mov      dword ptr [rdx+4*rsi+0x10], 6
 
G_M000_IG08:
       xor      edx, edx
       test     ecx, ecx
       jle      SHORT G_M000_IG10
 
G_M000_IG09:
       mov      r8, 0xD1FFAB1E
       mov      rsi, gword ptr [r8]
       mov      rdi, rsi
       lea      r8d, [rcx-0x01]
       mov      r9d, dword ptr [rdi+0x08]
       cmp      r8d, r9d
       jae      G_M000_IG38
       mov      r9d, r8d
       cmp      dword ptr [rdi+4*r9+0x10], 7
       je       SHORT G_M000_IG10
       mov      dword ptr [rsi+4*r9+0x10], 7
 
G_M000_IG10:
       cmp      rdx, rbx
       jl       G_M000_IG27
 
G_M000_IG11:
       test     ecx, ecx
       jle      SHORT G_M000_IG13
 
G_M000_IG12:
       mov      r9, 0xD1FFAB1E
       mov      rsi, gword ptr [r9]
       mov      rdx, rsi
       lea      r8d, [rcx-0x01]
       mov      edi, r8d
       mov      r9d, dword ptr [rdx+0x08]
       cmp      edi, r9d
       jae      G_M000_IG38
       mov      r9d, edi
       cmp      dword ptr [rdx+4*r9+0x10], 15
       je       SHORT G_M000_IG13
       mov      rdx, rsi
       mov      dword ptr [rdx+4*r9+0x10], 15
 
G_M000_IG13:
       mov      rdx, rbx
 
G_M000_IG14:
       test     rax, rax
       jle      SHORT G_M000_IG17
 
G_M000_IG15:
       movsxd   rcx, ecx
       cmp      rax, rcx
       jg       SHORT G_M000_IG17
 
G_M000_IG16:
       dec      eax
       mov      dword ptr [(reloc)], eax
 
G_M000_IG17:
       mov      rax, rdx
 
G_M000_IG18:
       add      rsp, 24
       pop      rbx
       pop      rbp
       ret      
 
G_M000_IG19:
       align    [0 bytes for IG20]
 
G_M000_IG20:
       mov      r9, 0xD1FFAB1E
       mov      rsi, gword ptr [r9]
       mov      rdi, rsi
       lea      r8d, [rcx-0x01]
       mov      r9d, dword ptr [rdi+0x08]
       cmp      r8d, r9d
       jae      G_M000_IG38
       mov      r9d, r8d
       cmp      dword ptr [rdi+4*r9+0x10], 12
       je       SHORT G_M000_IG21
       mov      dword ptr [rsi+4*r9+0x10], 12
 
G_M000_IG21:
       vaddsd   xmm0, xmm0, xmm0
       vmulsd   xmm1, xmm0, xmm1
       vmovsd   xmm0, qword ptr [rbp-0x18]
       vaddsd   xmm1, xmm1, xmm0
       test     ecx, ecx
       jle      SHORT G_M000_IG23
 
G_M000_IG22:
       mov      r9, 0xD1FFAB1E
       mov      rsi, gword ptr [r9]
       mov      rdi, rsi
       lea      r8d, [rcx-0x01]
       mov      r9d, dword ptr [rdi+0x08]
       cmp      r8d, r9d
       jae      G_M000_IG38
       mov      r9d, r8d
       cmp      dword ptr [rdi+4*r9+0x10], 13
       je       SHORT G_M000_IG23
       mov      dword ptr [rsi+4*r9+0x10], 13
 
G_M000_IG23:
       vsubsd   xmm2, xmm2, xmm3
       vmovsd   xmm3, qword ptr [rbp-0x10]
       vaddsd   xmm2, xmm2, xmm3
       test     ecx, ecx
       jle      SHORT G_M000_IG25
 
G_M000_IG24:
       mov      r9, 0xD1FFAB1E
       mov      rsi, gword ptr [r9]
       mov      rdi, rsi
       lea      r8d, [rcx-0x01]
       mov      r9d, dword ptr [rdi+0x08]
       cmp      r8d, r9d
       jae      G_M000_IG38
       mov      r9d, r8d
       cmp      dword ptr [rdi+4*r9+0x10], 14
       je       SHORT G_M000_IG25
       mov      dword ptr [rsi+4*r9+0x10], 14
 
G_M000_IG25:
       add      rdx, 1
       jo       G_M000_IG39
       cmp      rdx, rbx
       vmovsd   qword ptr [rbp-0x10], xmm3
       vmovsd   qword ptr [rbp-0x18], xmm0
       jge      G_M000_IG11
 
G_M000_IG26:
       vmovaps  xmm0, xmm2
 
G_M000_IG27:
       test     ecx, ecx
       jle      SHORT G_M000_IG29
 
G_M000_IG28:
       mov      r9, 0xD1FFAB1E
       mov      rsi, gword ptr [r9]
       mov      rdi, rsi
       lea      r8d, [rcx-0x01]
       mov      r9d, dword ptr [rdi+0x08]
       cmp      r8d, r9d
       jae      G_M000_IG38
       mov      r9d, r8d
       cmp      dword ptr [rdi+4*r9+0x10], 8
       je       SHORT G_M000_IG29
       mov      dword ptr [rsi+4*r9+0x10], 8
 
G_M000_IG29:
       vmulsd   xmm2, xmm0, xmm0
       test     ecx, ecx
       jle      SHORT G_M000_IG31
 
G_M000_IG30:
       mov      r9, 0xD1FFAB1E
       mov      rsi, gword ptr [r9]
       mov      rdi, rsi
       lea      r8d, [rcx-0x01]
       mov      r9d, dword ptr [rdi+0x08]
       cmp      r8d, r9d
       jae      G_M000_IG38
       mov      r9d, r8d
       cmp      dword ptr [rdi+4*r9+0x10], 9
       je       SHORT G_M000_IG31
       mov      dword ptr [rsi+4*r9+0x10], 9
 
G_M000_IG31:
       vmulsd   xmm3, xmm1, xmm1
       test     ecx, ecx
       jle      SHORT G_M000_IG33
 
G_M000_IG32:
       mov      r9, 0xD1FFAB1E
       mov      rsi, gword ptr [r9]
       mov      rdi, rsi
       lea      r8d, [rcx-0x01]
       mov      r9d, dword ptr [rdi+0x08]
       cmp      r8d, r9d
       jae      SHORT G_M000_IG38
       mov      r9d, r8d
       cmp      dword ptr [rdi+4*r9+0x10], 10
       je       SHORT G_M000_IG33
       mov      dword ptr [rsi+4*r9+0x10], 10
 
G_M000_IG33:
       vaddsd   xmm4, xmm2, xmm3
       vucomisd xmm4, qword ptr [reloc @RWD00]
       ja       SHORT G_M000_IG35
 
G_M000_IG34:
       test     ecx, ecx
       jle      G_M000_IG21
       jmp      G_M000_IG20
 
G_M000_IG35:
       test     ecx, ecx
       jle      SHORT G_M000_IG37
 
G_M000_IG36:
       mov      r9, 0xD1FFAB1E
       mov      rsi, gword ptr [r9]
       mov      rdi, rsi
       lea      r8d, [rcx-0x01]
       mov      r9d, dword ptr [rdi+0x08]
       cmp      r8d, r9d
       jae      SHORT G_M000_IG38
       mov      r9d, r8d
       cmp      dword ptr [rdi+4*r9+0x10], 11
       je       SHORT G_M000_IG37
       mov      dword ptr [rsi+4*r9+0x10], 11
 
G_M000_IG37:
       jmp      G_M000_IG14
 
G_M000_IG38:
       call     CORINFO_HELP_RNGCHKFAIL
       int3     
 
G_M000_IG39:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
RWD00  	dq	4010000000000000h	;            4

; Total bytes of code 884

