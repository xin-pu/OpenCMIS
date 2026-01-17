# OpenCMIS 分层设计

下面是建议的分层图与职责边界，兼容当前代码并为后续 I2C/MDIO、RAL/CDB、256 字节寻址和多 UI 扩展预留空间。

```
┌─────────────────────────────┐
│           UI 层              │
│  CLI / 桌面 GUI / Web UI      │
└──────────────┬──────────────┘
               │
┌──────────────▼──────────────┐
│         Application 层       │
│  业务流程编排、权限、日志     │
│  任务/会话/设备生命周期       │
└──────────────┬──────────────┘
               │
┌──────────────▼──────────────┐
│          Service 层          │
│  RAL 服务 / CDB 服务 / 命令   │
│  把寄存器语义转为业务操作     │
└──────────────┬──────────────┘
               │
┌──────────────▼──────────────┐
│         Protocol 层          │
│  CMIS 页选择 + 寄存器访问     │
│  寻址策略（128/256 等）       │
└──────────────┬──────────────┘
               │
┌──────────────▼──────────────┐
│         Transport 层         │
│  I2C/MDIO 具体驱动实现        │
│  IRegisterTransport           │
└──────────────┬──────────────┘
               │
┌──────────────▼──────────────┐
│           硬件设备            │
└─────────────────────────────┘
```

## 当前代码映射 (目录结构)

- **01_Shared**
  - `OpenCMIS.Shared`: 基础枚举、异常、工具类
- **02_Transport**
  - `OpenCMIS.Transport.Abstractions`: `IDeviceConnection`, `IRegisterTransport`
  - `OpenCMIS.Transport.I2C`: I2C 具体驱动实现
- **03_Protocol**
  - `OpenCMIS.Protocol.Abstractions`: `IRegisterAccess` 接口、CMIS 模型
  - `OpenCMIS.Protocol.Core`: `RegisterAccess`, `PageManager` 实现
- **04_Services**
  - `OpenCMIS.Services.Abstractions`: `ICdbReader`, `ICdbWriter` 等接口
  - `OpenCMIS.Services.Core`: `CdbManager`, `CommandProcessor` 实现
- **05_Application**
  - `OpenCMIS.Application.Core`: `DeviceManager`, `CmisDevice`, `DeviceMonitor` (业务编排)
- **06_UI**
  - `OpenCMIS.UI.CLI`: 命令行交互界面

## 设计要点

- Protocol 层只关心“页选择 + 寄存器语义”，不依赖 I2C/MDIO 细节。
- Transport 层只关心“寄存器读写能力”，不承担 CMIS 业务逻辑。
- RAL/CDB 放在 Service 层，避免 UI 或 Protocol 直接操作寄存器。
- 256 字节寻址建议做成 Protocol 层的“寻址策略”扩展点，避免 UI 绑定底层规则。
