# OpenCMIS

OpenCMIS 是面向 CMIS 光模块的 .NET 10 通信库和工具集。核心协议不依赖 Pulse、NuGet 形式的 Cypress 包或具体硬件驱动；串口和 Cypress USB 通过可替换的 I2C provider 接入。

## 当前架构

- `OpenCMIS.Transport.Abstractions`：硬件无关的 `II2cRegisterBus`、provider、连接 profile 和 I2C 值对象。
- `OpenCMIS.Module.Core`：一个光模块会话，以及共享同一原子锁的 MSA 页面访问和 vendor HCI 访问。
- `OpenCMIS.Protocol.*`：CMIS 协议模型和兼容 facade。
- `OpenCMIS.App.Core`：设备发现、provider 选择和光模块工厂，不引用具体串口或 Cypress 实现。
- `OpenCMIS.Transport.I2C.Serial`：跨平台编译的 Linktel、HM 和 HM 多通道串口适配器。
- `OpenCMIS.Cypress`：从已授权公司源码导入的 Windows-only Cypress USB 低层库。
- `OpenCMIS.Transport.I2C.Cypress`：Windows-only FIC2USB/EUI3 I2C provider；低层阻塞调用封装在 `ICypressDeviceApi` 后。
- `OpenCMIS.UI.CLI`：只组合核心和串口 provider。
- `OpenCMIS.UI.WPF`：Windows 上组合核心、串口和 Cypress provider。

详细设计见 [光模块 I2C 架构](docs/architecture/optical-module-i2c.md)。

## I2C 地址

核心统一使用 7-bit 地址：

```csharp
var moduleAddress = new I2cDeviceAddress(0x50);
```

旧代码中的 `0xA0` 是 8-bit write address，只能在兼容边界显式转换：

```csharp
var moduleAddress = I2cDeviceAddress.FromLegacy8Bit(0xA0);
```

## MSA 与 HCI

```csharp
await using var session = new OpticalModuleSession(bus);
await session.OpenAsync();

var msa = new MsaMemoryAccessor(session);
var page0 = await msa.ReadAsync(
    moduleAddress,
    new ModulePage(0),
    new RegisterOffset(128),
    16);

var hci = new HciMemoryAccessor(
    session,
    new HciOptions(),
    TimeProvider.System);
var data = await hci.ReadAsync(
    moduleAddress,
    new HciTableId(1),
    new RegisterOffset(0),
    16);
```

MSA 的选页和数据传输，以及完整 HCI 命令序列，都通过同一个 `OpticalModuleSession` gate 串行化，避免并发任务相互改变页面或命令状态。

## Provider ID

| Provider ID | 实现 | 平台 |
|---|---|---|
| `linktel` | Linktel 串口桥 | .NET 10 支持的平台 |
| `hm` | HM 串口桥 | .NET 10 支持的平台 |
| `hm-multichannel` | HM 多通道串口桥 | .NET 10 支持的平台 |
| `cypress` | FIC2USB / EUI3 | Windows |

CLI 不引用 Cypress 项目。WPF 通过 `AddOpenCmisCypressAdapters()` 在 Windows 组合根注册 Cypress provider。

## 构建与测试

```powershell
dotnet restore OpenCMIS.sln
dotnet test OpenCMIS.sln --no-restore
dotnet build OpenCMIS.sln --no-restore
```

目前自动化测试全部使用 fake/mock。它们验证协议编码、分段、provider 选择、MSA/HCI 操作顺序、并发串行化、超时、取消和错误转换，但不代表真实硬件已经验证。

尚未验证：

- Linktel、HM、HM 多通道串口桥
- FIC2USB、EUI3 与 `CyUSB3.sys`
- 实体 CMIS 光模块的 Page 0、MSA 高页和 HCI 读写
- USB 热插拔、电气兼容性和长时间重连

## 兼容层

`I2CConnectorTypeA`、`I2CConnectorTypeB`、旧 `PageManager` 和旧 `RegisterAccess` 构造方式仅作为迁移兼容层保留，并已标记为 obsolete。新代码应使用 typed profile、provider 和 `OpticalModuleSession`。

## 许可

OpenCMIS 自有代码使用仓库根目录的 MIT License。

`src/OpenCMIS.Cypress` 包含独立的第三方 Cypress 源码，不应被解释为由 OpenCMIS 的 MIT License 重新授权。其来源、授权范围和限制见：

- `docs/third-party/cypress-license-review.md`
- `src/OpenCMIS.Cypress/THIRD-PARTY-NOTICES.md`

当前记录的授权范围不包含未经单独审查的外部公开发布。
