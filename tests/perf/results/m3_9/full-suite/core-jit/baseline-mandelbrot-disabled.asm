; Assembly listing for method RyuPy.Generated.Module:m0_mandel(double,double,long):long (FullOpts)
; Emitting BLENDED_CODE for generic X64 + VEX + EVEX on Unix
; FullOpts code
; optimized code
; rbp based frame
; partially interruptible
; No PGO data

G_M000_IG01:
       push     rbp
       push     r15
       push     r14
       push     rbx
       sub      rsp, 56
       lea      rbp, [rsp+0x50]
       vmovsd   qword ptr [rbp-0x20], xmm0
       vmovsd   qword ptr [rbp-0x28], xmm1
       mov      rbx, rdi
 
G_M000_IG02:
       mov      rdi, 0xD1FFAB1E
       mov      rsi, 0xD1FFAB1E
       call     [RyuPy.Runtime.PyTraceback:PushFrame(System.String,System.String):long]
       mov      r15, rax
       mov      edi, 4
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vxorps   xmm0, xmm0, xmm0
       vmovsd   qword ptr [rbp-0x30], xmm0
       mov      edi, 5
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vxorps   xmm0, xmm0, xmm0
       vmovsd   qword ptr [rbp-0x38], xmm0
       mov      edi, 6
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       xor      r14d, r14d
       mov      edi, 7
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       test     rbx, rbx
       jle      G_M000_IG04
 
G_M000_IG03:
       mov      edi, 8
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vmovsd   qword ptr [rbp-0x30], xmm0
       vmulsd   xmm1, xmm0, xmm0
       vmovsd   qword ptr [rbp-0x40], xmm1
       mov      edi, 9
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm0, qword ptr [rbp-0x38]
       vmovsd   qword ptr [rbp-0x38], xmm0
       vmulsd   xmm1, xmm0, xmm0
       vmovsd   qword ptr [rbp-0x48], xmm1
       mov      edi, 10
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm1, qword ptr [rbp-0x40]
       vaddsd   xmm2, xmm1, qword ptr [rbp-0x48]
       vucomisd xmm2, qword ptr [reloc @RWD00]
       ja       SHORT G_M000_IG05
       mov      edi, 12
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm0, qword ptr [rbp-0x30]
       vaddsd   xmm0, xmm0, xmm0
       vmulsd   xmm0, xmm0, qword ptr [rbp-0x38]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x28]
       vmovsd   qword ptr [rbp-0x38], xmm0
       mov      edi, 13
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       vmovsd   xmm1, qword ptr [rbp-0x40]
       vsubsd   xmm0, xmm1, qword ptr [rbp-0x48]
       vaddsd   xmm0, xmm0, qword ptr [rbp-0x20]
       vmovsd   qword ptr [rbp-0x30], xmm0
       mov      edi, 14
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       add      r14, 1
       jo       SHORT G_M000_IG08
       cmp      r14, rbx
       jl       G_M000_IG03
 
G_M000_IG04:
       mov      edi, 15
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
       mov      r14, rbx
       jmp      SHORT G_M000_IG06
 
G_M000_IG05:
       mov      edi, 11
       call     [RyuPy.Runtime.PyTraceback:SetLine(int)]
 
G_M000_IG06:
       mov      rdi, r15
       call     [RyuPy.Runtime.PyTraceback:PopTo(long)]
       mov      rax, r14
 
G_M000_IG07:
       add      rsp, 56
       pop      rbx
       pop      r14
       pop      r15
       pop      rbp
       ret      
 
G_M000_IG08:
       call     CORINFO_HELP_OVERFLOW
       int3     
 
RWD00  	dq	4010000000000000h	;            4

; Total bytes of code 370

