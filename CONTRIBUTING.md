# 贡献指南

本文记录构建、测试、发布与文档维护的流程。

## 环境要求

- .NET SDK 10.0 或更高
- Windows（WPF 项目仅支持 Windows）
- PowerShell 7（`pwsh`，用于覆盖率与版本脚本）

## 构建与测试

```bash
dotnet build Len.Abp.slnx -c Release -m:1
dotnet test  Len.Abp.slnx -c Release --no-build
```

`-m:1` 串行构建用于规避 DLP/加密代理拦截 `obj` 生成文件导致的
`CS2015: 是二进制文件而非文本文件`。若仍失败，清理 `obj`/`bin` 后重试。

## 覆盖率

CI 门槛为 90%，本地可用脚本检查：

```bash
dotnet test Len.Abp.slnx -c Release --collect:"XPlat Code Coverage" --results-directory ./coverage
pwsh scripts/check-coverage.ps1 -Path ./coverage -Threshold 90
```

脚本按「类名 + 行号」去重后统计行覆盖率，并列出覆盖率最低的 10 个类。

## 代码风格

`.editorconfig` 中的规则通过 `EnforceCodeStyleInBuild` 参与命令行构建，
违反即构建失败。注意：**只设 `severity` 而不定义规则本体时规则不会生效**，
新增规则必须同时给出 `dotnet_naming_rule.*` 等完整定义。

注释、日志与异常消息一律使用中文；类型名、方法名等标识符保持英文。

## 提交约定

- 提交信息只写单行 subject，不写 body；
- 前缀沿用现有风格：`feat:`、`fix:`、`refactor:`、`test:`、`docs:`、`build:`、`ci:`、`chore:`；
- 合并分支保持线性（`--ff-only` 或 rebase），不产生 merge commit。

## 文档

- 用户手册：[docs/USER-GUIDE.md](docs/USER-GUIDE.md)，面向第一次上手的使用者；
- 方向与定位：[ROADMAP.md](ROADMAP.md)，只写做什么与不做什么；
- 包说明：各项目目录下的 `PACKAGE.md`，随包发布。

**架构取舍写在代码旁边。** 涉及取舍的类型与方法，用 XML 文档注释的 `<remarks>`
说明"为什么这样做"与"改成别的会怎样"；测试里用注释记录缺陷的形态与复现条件。
文档只描述当前状态，不保留历史决策记录。

## 发布流程

### 版本号

版本由 git tag 决定，`scripts/set-version.ps1` 生成 `version.props`：

| tag | 版本 |
| --- | --- |
| `v1.2.3` / `1.2.3` | `1.2.3` |
| `v1.2.3-beta.1` | `1.2.3-beta.1` |
| 无 tag | `0.0.0-dev` |

本地验证版本生成：

```bash
pwsh scripts/set-version.ps1          # 从当前 HEAD 的 tag 生成
pwsh scripts/set-version.ps1 -Clear   # 删除 version.props，回退到 0.0.0-dev
```

### 发布 Checklist

- [ ] 已知缺陷已全部修复，回归测试已补齐
- [ ] `dotnet build Len.Abp.slnx -c Release -m:1` 通过，无警告
- [ ] `dotnet test` 全部通过
- [ ] 覆盖率 ≥ 90%
- [ ] `git tag <版本号>` 并推送
- [ ] CI `build-test` 通过
- [ ] CI `pack` 产出 3 个 `.nupkg`：`Len.Abp.Core`、`Len.Abp.Wpf.Abstractions`、`Len.Abp.Wpf`
- [ ] 每个包含自己的 `PACKAGE.md`（项目目录下，内容面向该包的使用者）
- [ ] `Len.Abp.Wpf` 包内含 `analyzers/dotnet/cs/Len.Abp.Analyzer.Wpf.dll` 与 `Len.Abp.Analyzer.dll`
- [ ] `NUGET_API_KEY` 已配置，`publish` job 成功
- [ ] NuGet 页面上所有包可见，版本号正确
- [ ] 用全新项目验证一次安装与生成

### 检查包内容

```bash
unzip -l src/Len.Abp.Wpf/bin/Release/Len.Abp.Wpf.*.nupkg
grep -rn "\[Generator\]" src/
```

新增可打包项目时，在项目目录下写一份 `PACKAGE.md`——`common.props` 检测到该文件
才会设置 `PackageReadmeFile` 并把它打进包。内容面向该包的使用者：安装方式、
公开 API、该包特有的注意事项，不要照抄仓库根 `README.md`。

## 测试约定

- 测试项目与类库一一对应，新增类库需同步新增测试项目；
- 生成器测试必须真正编译生成的代码，只断言文本会漏掉编译错误；
- 需要 WPF 对象的测试使用 `Sta.cs` 提供的 STA 环境；
- 涉及全局 `AbpServiceProvider` 的测试必须加入 `GlobalServiceProviderCollection` 串行执行。
