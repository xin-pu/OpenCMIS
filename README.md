# OpenCMIS

<img src="src/OpenCMIS.UI.WPF/Resources/mote-green.ico" alt="OpenCMIS" width="48"/>

OpenCMIS 是面向 CMIS 5.2/5.3 光模块的 .NET 10 通信库与工具集，提供 **CLI** 与 **GUI** 双入口，支持 **串口桥**、**Cypress USB** 与 **内置模拟器** 三种 I2C provider。

核心协议不依赖 Pulse、NuGet 形式的 Cypress 包或具体硬件驱动；串口和 Cypress USB 通过可替换的 I2C provider 接入，无真实硬件时可直接使用内置模拟器开发与演示。

## 功能特性

- **CMIS 协议读写**：MSA 页选择（bank/page）、寄存器级 `read` / `write`（写后回读验证）
- **模块信息与状态**：厂商、PN/SN、模块类型、CMIS 版本、能力标志、运行状态（Ready / LowPwr / PwrDn…）
- **实时监控**：温度、电压、Per-Lane 发射/接收参数，支持噪声模拟与告警事件
- **CDB（Configuration Data Block）**：读取、字段编辑、校验（CRC-16）、写入、`.cdb` / `.json` 导入导出
- **应用切换**：枚举模块支持的 CMIS Applications，查询当前应用并执行切换
- **模拟设备**：内置 800G QSFP-DD / 1.6T OSFP（CMIS 5.2 / 5.3）四种模拟模块，无硬件即可体验全部功能

## 架构分层

| 层 | 项目 | 职责 |
|---|---|---|
| 传输抽象 | `OpenCMIS.Transport.Abstractions` | 硬件无关的 `II2cRegisterBus`、provider、连接 profile 与 I2C 值对象 |
| 传输实现 | `OpenCMIS.Transport.I2C.Serial` | 跨平台串口桥适配器（Linktel / HM / HM 多通道） |
| 传输实现 | `OpenCMIS.Transport.I2C.Cypress` + `OpenCMIS.Cypress` | Windows-only FIC2USB / EUI3 USB 适配器（`OpenCMIS.Cypress` 为第三方低层库，低层阻塞调用封装在 `ICypressDeviceApi` 后） |
| 传输实现 | `OpenCMIS.Transport.Simulated` | 内置模拟 I2C 总线，含噪声与确定性 seed，无硬件演示/测试 |
| 模块会话 | `OpenCMIS.Module.Core` | 光模块会话：MSA 页访问与 vendor HCI 访问共享同一原子锁串行化 |
| 协议 | `OpenCMIS.Protocol.Abstractions` / `OpenCMIS.Protocol.Core` | CMIS 协议模型、`IRegisterAccess`、命令处理与兼容 facade |
| 应用核心 | `OpenCMIS.App.Core` | 设备发现、provider 选择、光模块工厂，不引用具体串口/Cypress 实现 |
| 配置数据 | `OpenCMIS.CDB.Abstractions` / `OpenCMIS.CDB.Core` | CDB 读取、写入（跨页分段）、校验（CRC-16/CCITT） |
| 共享 | `OpenCMIS.Shared` | CMIS 常量、枚举、异常与工具 |
| 用户界面 | `OpenCMIS.UI.CLI` | 命令行工具：只组合核心与串口 provider |
| 用户界面 | `OpenCMIS.UI.WPF` | Windows 桌面应用：组合核心、串口、Cypress 与模拟 provider |

详细设计见 [光模块 I2C 架构](docs/architecture/optical-module-i2c.md)。

## 快速开始

环境要求：**.NET 10 SDK**（WPF 另需 Windows）。

```powershell
# 还原并构建
dotnet restore OpenCMIS.sln
dotnet build OpenCMIS.sln

# 运行全部测试
dotnet test OpenCMIS.sln
```

### CLI

```powershell
# 列出可用设备
dotnet run --project src/OpenCMIS.UI.CLI -- list

# 查看模块信息（串口）
dotnet run --project src/OpenCMIS.UI.CLI -- info COM3
```

完整命令说明见 [CLI 使用指南](docs/cli-guide.md)。

### GUI（WPF）

```powershell
dotnet run --project src/OpenCMIS.UI.WPF
```

**无真实硬件？** 在 Device Connection 页点击 **Scan**，Adapter 选择 **`sim`**，设备选择 **"Simulated 800G CMIS Module (5.2)"**（或 5.3 / 1.6T 变体），点 **Connect** 即可体验全部功能。

界面功能与截图见 [GUI 使用指南](docs/gui-guide.md)。

## 用户文档入口

| 文档 | 内容 |
|---|---|
| [CLI 使用指南](docs/cli-guide.md) | 命令速查、逐命令详解与示例 |
| [GUI 使用指南](docs/gui-guide.md) | 主窗口布局、六大页面功能、模拟设备使用（含截图） |
| [光模块 I2C 架构](docs/architecture/optical-module-i2c.md) | MSA / HCI 会话、传输抽象与 provider 设计 |

## 技术参考

### I2C 地址

核心统一使用 7-bit 地址：

```csharp
var moduleAddress = new I2cDeviceAddress(0x50);
```

旧代码中的 `0xA0` 是 8-bit write address，只能在兼容边界显式转换：

```csharp
var moduleAddress = I2cDeviceAddress.FromLegacy8Bit(0xA0);
```

### MSA 与 HCI

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

### Provider ID

| Provider ID | 实现 | 平台 | 注册入口 |
|---|---|---|---|
| `linktel` | Linktel 串口桥 | .NET 10 支持的平台 | `AddOpenCmisSerialAdapters()` |
| `hm` | HM 串口桥 | .NET 10 支持的平台 | `AddOpenCmisSerialAdapters()` |
| `hm-multichannel` | HM 多通道串口桥 | .NET 10 支持的平台 | `AddOpenCmisSerialAdapters()` |
| `cypress` | FIC2USB / EUI3 | Windows | `AddOpenCmisCypressAdapters()` |
| `sim` | 内置模拟器 | 全平台 | `AddOpenCmisSimulatedAdapters()` |

CLI 只注册串口 provider；WPF 在组合根注册串口、Cypress 与模拟 provider。

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
