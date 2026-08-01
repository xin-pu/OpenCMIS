# 2026-07-31 模拟 I2C 验证记录

## 范围

- 分支：`codex/refactor-module-i2c-architecture`
- 验证前提交：`e100324ac793f6b5d0e56149818f9c235c0cde9d`
- .NET SDK：`10.0.301`
- 环境：Windows，未连接目标光模块、串口桥或 Cypress USB 硬件
- 源项目：14
- 测试项目：5

## Framework 与依赖边界

执行：

```powershell
rg -n "<TargetFramework>" src tests -g "*.csproj"
rg -n "Pulse\.|Pulse.Instruments.Cypress|PackageReference.*Cypress" src tests -g "*.csproj" -g "*.cs"
dotnet list src\OpenCMIS.Module.Core\OpenCMIS.Module.Core.csproj reference
dotnet list src\OpenCMIS.Protocol.Core\OpenCMIS.Protocol.Core.csproj reference
dotnet list src\OpenCMIS.App.Core\OpenCMIS.App.Core.csproj reference
dotnet list src\OpenCMIS.UI.CLI\OpenCMIS.UI.CLI.csproj reference
dotnet list src\OpenCMIS.UI.WPF\OpenCMIS.UI.WPF.csproj reference
```

结果：

- 核心、Protocol、App、CLI、串口实现及其测试为 `net10.0`；
- Cypress 低层库、Cypress I2C 插件、Cypress 测试和 WPF 为 `net10.0-windows`；
- 源码和项目文件中没有 Pulse 项目引用、Pulse namespace 或 Cypress NuGet package；
- `Module.Core` 只引用 Shared 和 Transport.Abstractions；
- App.Core 不引用具体串口、旧 I2C 或 Cypress 项目；
- CLI 只组合串口 provider，不引用 Cypress；
- WPF 在 Windows 组合串口和 Cypress provider。

## Release 测试

执行：

```powershell
dotnet test OpenCMIS.sln --no-restore --configuration Release
```

结果：

| 测试程序集 | 通过 | 失败 | 跳过 |
|---|---:|---:|---:|
| OpenCMIS.Transport.Abstractions.Tests | 14 | 0 | 0 |
| OpenCMIS.Module.Core.Tests | 16 | 0 | 0 |
| OpenCMIS.Transport.I2C.Serial.Tests | 25 | 0 | 0 |
| OpenCMIS.App.Core.Tests | 8 | 0 | 0 |
| OpenCMIS.Transport.I2C.Cypress.Tests | 13 | 0 | 0 |
| **合计** | **76** | **0** | **0** |

## Release 构建

执行：

```powershell
dotnet build OpenCMIS.sln --no-restore --configuration Release
```

结果：

- Build succeeded
- 0 warnings
- 0 errors

## 已验证

- I2C 地址、offset、profile 和能力值验证；
- Linktel/HM wire-frame 编码、解析和 partial-read 处理；
- 串口与 Cypress transfer segmentation；
- provider 选择、发现合并及发现失败记录；
- MSA 选页与 transfer 的原子操作顺序；
- MSA/HCI 共享会话 gate 的并发串行化；
- HCI 命令编码、ready polling、timeout 和响应验证；
- Cypress FIC2USB/EUI3 地址、port、speed 和寄存器前缀映射；
- Cypress false/malformed 结果的错误转换；
- 阻塞 Cypress 调用前后的 cancellation boundary；
- adapter close/disposal；
- CLI 串口-only 与 WPF Windows provider 组合。

## 未验证

- 与实体光模块通信；
- Linktel 串口桥；
- HM 串口桥；
- HM 多通道串口桥；
- FIC2USB；
- EUI3；
- `CyUSB3.sys` 安装、版本或签名；
- USB 热插拔；
- 电气兼容性、总线时序或长时间稳定性。

## 后续硬件验证矩阵

以下单元格全部为待验证，不应从当前模拟结果推断为通过。

| 硬件 | 发现 | 打开/关闭 | Page 0 | Page 17 | HCI 读 | HCI 写 | 超时 | 取消 | 重复重连 |
|---|---|---|---|---|---|---|---|---|---|
| Linktel | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 |
| HM | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 |
| HM 多通道 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 |
| FIC2USB | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 |
| EUI3 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 | 待验证 |

建议每种硬件至少增加：

1. 冷启动和热插拔发现；
2. 连续 100 次打开/关闭；
3. Page 0 与 Page 17 已知数据对照；
4. HCI 已知表的读写回环；
5. 人为断线下的 timeout/cancellation；
6. 8 小时以上循环访问与错误率统计。
