; 分析器诊断的发布跟踪文件。
; 格式见 https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md
; 新增诊断时在 Unshipped 段追加一行；发布后移入 Shipped 段，ID 不可更改。

## Unshipped

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
LENABP006 | Len.Abp | Warning | 视图模型未声明 partial，无法自动注册
LENABP007 | Len.Abp | Warning | 视图模型的构造函数包含不可注入的参数
LENABP015 | Len.Abp | Warning | 视图不在 Views 目录下
LENABP016 | Len.Abp | Warning | 视图模型不在 ViewModels 目录下

## Shipped
