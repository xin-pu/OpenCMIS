# 光模块 I2C 架构

## 目标

该架构把光模块通信从 Pulse 和具体硬件实现中独立出来，使 OpenCMIS 的核心协议可以：

- 以 .NET 10 独立构建；
- 使用同一套 API 访问 MSA 页面和 vendor HCI；
- 在不改变核心协议的情况下增加 I2C 适配器；
- 在无硬件环境中通过可控 fake 验证操作顺序、失败和并发；
- 把 Windows-only Cypress 依赖限制在最外层。

## 依赖方向

```text
CLI --------------------------> App.Core
 |                                  |
 +--> Transport.I2C.Serial          v
                             Protocol / Module.Core
WPF -------------------------->     |
 |                                  v
 +--> Transport.I2C.Serial   Transport.Abstractions
 +--> Transport.I2C.Cypress --------^
          |
          v
    OpenCMIS.Cypress (Windows-only)
```

核心项目不引用串口或 Cypress 实现：

- `Module.Core` 只依赖 `Transport.Abstractions`；
- Protocol 和 App 依赖抽象及模块核心；
- CLI 只在组合根引用串口项目；
- WPF 是唯一同时组合串口与 Cypress provider 的应用。

## 核心端口

`II2cRegisterBus` 是硬件无关端口，使用：

- `I2cDeviceAddress`：规范化的 7-bit 地址；
- `RegisterOffset`：8-bit 寄存器偏移；
- `I2cTransferCapabilities`：单次传输能力；
- 异步 open、close、read、write 和 disposal。

`II2cAdapterProvider` 负责发现和打开某类适配器。`DeviceManager` 只根据 `I2cConnectionProfile.AdapterId` 选择 provider，不再直接构造硬件类。

### 地址约定

核心地址 `0x50` 对应旧硬件 API 常见的 8-bit write address `0xA0`。转换只能发生在边界：

```text
core 0x50 --ToWriteAddress8Bit()--> hardware API 0xA0
legacy 0xA0 --FromLegacy8Bit()----> core 0x50
```

禁止在核心代码中混用 7-bit 与 8-bit 地址。

## 光模块会话与原子性

`OpticalModuleSession` 拥有一个 `II2cRegisterBus` 和一个会话级 gate。

MSA 页面读取不是两次互不相关的调用，而是一个原子操作：

```text
acquire session gate
  write page-select register
  read/write requested register range
release session gate
```

HCI 也在同一个 gate 内执行完整命令：

```text
acquire session gate
  enter/select HCI table
  poll ready
  write command
  finish command
  poll ready
  read response when applicable
release session gate
```

因此 MSA 与 HCI 并发时不会互相改变页面、表或命令状态。

## 适配器

### 串口

- `LinktelSerialI2cAdapter`：adapter ID `linktel`
- `HmSerialI2cAdapter`：adapter ID `hm`
- `HmMultiChannelI2cAdapter`：adapter ID `hm-multichannel`

串口实现把 wire-frame codec、串口 session 和 provider 分开。provider 负责发现/typed profile，adapter 负责 I2C 语义，codec 负责帧编码和解析。

### Cypress

Windows-only Cypress 分为两层：

1. `OpenCMIS.Cypress`：授权导入的 USB/PInvoke 低层源码；
2. `OpenCMIS.Transport.I2C.Cypress`：实现 `II2cRegisterBus` 的 FIC2USB/EUI3 adapter。

只有 `CypressDeviceApi` 引用 `CyUSBDevices`、`DeviceFIC2USB` 和 `DeviceEUI3`。其余代码和测试只依赖 `ICypressDeviceApi`。

阻塞 USB 调用在 worker thread 执行。取消在调用前后观察：已经进入 native/driver 调用后不能安全强制终止，但调用返回时会立即传播取消。

FIC2USB 支持逻辑 port 0-7，以及 100/400 kHz。EUI3 当前暴露逻辑 port 0，并按原驱动支持 50/90/200/400 kHz。

## 组合根

跨平台/串口 host：

```csharp
services.AddOpenCmisCore();
services.AddOpenCmisSerialAdapters();
```

Windows WPF host：

```csharp
services.AddOpenCmisCore();
services.AddOpenCmisSerialAdapters();
services.AddOpenCmisCypressAdapters();
```

不要在 `App.Core`、Protocol 或 Module.Core 中注册具体硬件。

## DDD 使用程度

该模块适合采用“轻量 DDD + Ports and Adapters”，不适合为了形式引入数据库型 Repository、复杂 Aggregate 或 Domain Event。

已经具备的 DDD 概念：

- 值对象：`I2cDeviceAddress`、`RegisterOffset`、`ModulePage`、`HciTableId`；
- 领域服务：MSA/HCI accessor；
- 一致性边界：`OpticalModuleSession` 保证一个模块会话内的原子操作；
- 应用服务：`DeviceManager` 和 `OpticalModuleFactory`；
- 基础设施适配器：Serial 与 Cypress provider。

`OpticalModuleSession` 类似会话级聚合边界，但它管理的是易失硬件状态，不应伪装成可持久化 Aggregate Root。当前不引入 Repository 是有意选择。

## 兼容策略

旧 `I2CConnectorTypeA`、`I2CConnectorTypeB` 和旧页面管理路径只用于迁移。它们转接到新的 `II2cRegisterBus`，并标记 obsolete。新功能不得依赖这些类型。

## 验证边界

自动化测试使用 fake/mock，覆盖：

- 地址和 profile 验证；
- 串口帧编码/解析与 partial read；
- transfer segmentation；
- provider 选择和发现失败；
- MSA/HCI 操作顺序及并发串行化；
- HCI timeout；
- Cypress 错误、取消、返回长度和 disposal。

未覆盖真实硬件、驱动安装、USB 热插拔、电气/时序和长时间稳定性。硬件验证矩阵记录在 `docs/verification/2026-07-31-simulated-i2c-verification.md`。
