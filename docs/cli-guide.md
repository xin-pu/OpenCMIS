# OpenCMIS CLI 使用指南

OpenCMIS 命令行工具（`OpenCMIS.UI.CLI`）提供对 CMIS 5.2/5.3 光模块的寄存器读写、状态查询、实时监控、CDB 读取与应用切换能力。CLI 只组合核心与串口 provider，不依赖 Cypress 或模拟器。

## 运行方式

### 通过 dotnet run（源码方式）

```powershell
dotnet run --project src/OpenCMIS.UI.CLI -- <命令> [参数]
```

### 通过已构建的可执行文件

```powershell
dotnet build src/OpenCMIS.UI.CLI/OpenCMIS.UI.CLI.csproj
.\src\OpenCMIS.UI.CLI\bin\Debug\net10.0\OpenCMIS.UI.CLI.exe <命令> [参数]
```

不带任何参数运行，或执行 `help`，会打印用法说明。

## 命令速查

| 命令 | 语法 | 说明 |
|---|---|---|
| `list` | `OpenCMIS.UI.CLI list` | 扫描并列出可用设备 |
| `help` | `OpenCMIS.UI.CLI help` | 打印用法说明 |
| `info` | `info <port>` | 显示模块信息（厂商/PN/SN/类型/版本/能力） |
| `status` | `status <port>` | 显示模块状态与告警 |
| `monitor` | `monitor <port>` | 实时状态监控（Ctrl+C 停止） |
| `set-state` | `set-state <port> <state>` | 切换模块状态 |
| `read` | `read <port> <page> <addr>` | 读取单个寄存器 |
| `write` | `write <port> <page> <addr> <value>` | 写入寄存器并回读验证 |
| `cdb` | `cdb <port> read` | 读取 CDB 并列出字段 |
| `app` | `app <port> list` / `app <port> switch <code>` | 列出/切换应用 |

`<port>` 为串口名（如 `COM3`）；`<page>`、`<addr>`、`<value>` 为十进制字节值；`<code>` 支持 `0x` 十六进制前缀。

## 逐命令详解

### list — 列出设备

```powershell
OpenCMIS.UI.CLI list
```

扫描可用设备并打印每个设备的连接类型、名称与连接参数：

```
Scanning for CMIS devices...
Found 2 device(s):
  [I2C] CMIS Module on COM3
    PortName: COM3
    BaudRate: 115200
    SlaveAddress: 0xA0
```

### info — 模块信息

```powershell
OpenCMIS.UI.CLI info COM3
```

输出厂商名、Part Number、序列号、模块类型（如 QSFP-DD）、CMIS 版本，以及 CDB / 诊断监控 / 状态控制支持能力。

### status — 模块状态

```powershell
OpenCMIS.UI.CLI status COM3
```

输出当前状态机状态（`Initialization` / `LowPwr` / `PwrUp` / `Ready` / `PwrDn` / `Fault`）、是否就绪（Ready）以及活动告警列表。

### monitor — 实时监控

```powershell
OpenCMIS.UI.CLI monitor COM3
```

每秒轮询一次状态，状态变化或产生告警时在控制台输出（带时间戳）。按 **Ctrl+C** 停止并正常退出。

### set-state — 状态切换

```powershell
OpenCMIS.UI.CLI set-state COM3 Ready
```

合法状态值（不区分大小写）：`Initialization`、`LowPwr`、`PwrUp`、`Ready`、`PwrDn`、`Fault`。命令会先打印当前状态，切换后打印新状态以确认生效。非法状态会打印合法值列表到 stderr。

### read — 读取寄存器

```powershell
OpenCMIS.UI.CLI read COM3 0 128
```

读取指定 page 与地址的单字节并打印十进制与十六进制值：

```
Page 0x00, Reg 0x80 = 0x1E (30)
```

### write — 写入寄存器

```powershell
OpenCMIS.UI.CLI write COM3 2 145 100
```

写入后立即回读验证并打印结果：

```
Written and verified: Page 0x02, Reg 0x91 = 0x64 (100)
```

### cdb — 读取 CDB

```powershell
OpenCMIS.UI.CLI cdb COM3 read
```

读取模块的 Configuration Data Block（page 0x9F 起始，跨页分段读取），打印字段数、CRC-16 校验和与每个字段（类型 / ID / 值）。

### app — 应用查询与切换

```powershell
# 列出当前应用与支持的应用
OpenCMIS.UI.CLI app COM3 list

# 切换到应用（支持 0x 十六进制）
OpenCMIS.UI.CLI app COM3 switch 0x04
OpenCMIS.UI.CLI app COM3 switch 4
```

`switch` 写入应用选择寄存器后回读验证；切换失败会抛出错误。

## 连接参数

CLI 对给定串口使用固定连接参数：

- 波特率：**115200**
- 从站地址：**0xA0**（8-bit write address 表示法，核心内部统一为 7-bit `0x50`）

## 错误处理

- 错误信息写入 **stderr**（如 "Error: ..."），进程以**退出码 1** 结束
- 缺少必要参数时打印具体提示并显示用法
- 未知命令打印 `Unknown command: <command>` 与用法
- 连接失败、协议错误（如 `MsaPageSelectionFailed`、`DeviceNotConnected`）均以异常形式报错

## 使用场景示例

### 场景 1：确认串口桥可用

```powershell
OpenCMIS.UI.CLI list
```

### 场景 2：快速体检模块

```powershell
OpenCMIS.UI.CLI info COM3
OpenCMIS.UI.CLI status COM3
OpenCMIS.UI.CLI monitor COM3
```

### 场景 3：寄存器级调试

```powershell
# 读取 page 0x00 的标识符寄存器
OpenCMIS.UI.CLI read COM3 0 0
```

### 场景 4：CDB 与应用检查

```powershell
OpenCMIS.UI.CLI cdb COM3 read
OpenCMIS.UI.CLI app COM3 list
OpenCMIS.UI.CLI app COM3 switch 0x01
```

> **提示**：CLI 不包含模拟器 provider。无真实硬件时，请使用 GUI（内置 `sim` 模拟设备）体验相同功能，见 [GUI 使用指南](gui-guide.md)。
