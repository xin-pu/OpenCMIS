# OpenCMIS

基于CMIS (Common Management Interface Specification) 5.2协议的通用光模块控制库。

## 项目简介

OpenCMIS是一个开源的光模块控制库，提供了完整的CMIS协议实现，可以快速访问、控制和监控符合CMIS标准的光模块设备。项目包含核心库、图形用户界面（GUI）和命令行工具（CLI）三个主要组件。

## 主要功能

- ✅ **模块信息获取** - 读取光模块的识别信息、能力信息和状态信息
- ✅ **模块控制** - 控制光模块的各种功能和参数
- ✅ **实时监控** - 监控光模块的运行状态、告警和性能指标
- ✅ **CDB管理** - 实现Configuration Data Block的读取、写入、验证和应用
- ✅ **多接口支持** - 支持I2C、USB、串口等多种硬件接口
- ✅ **跨平台** - 基于.NET，支持Windows、Linux和macOS

## 项目组件

### OpenCMIS.Core
核心库，提供CMIS协议的基础实现和API接口。

```csharp
// 基本使用示例
var device = await deviceManager.OpenDeviceAsync(deviceInfo);
var moduleInfo = await device.GetModuleInfoAsync();
var status = await device.GetStatusAsync();
```

### OpenCMIS.GUI
基于WPF的图形用户界面应用程序，提供可视化的设备管理和控制界面。

**主要特性**：
- 设备连接管理
- 模块信息可视化
- 实时监控仪表盘
- 图形化控制面板
- CDB编辑和查看

### OpenCMIS.CLI
命令行工具，提供脚本化的设备控制能力。

**主要功能**：
- 命令行设备控制
- 批量操作支持
- 脚本执行
- 多种输出格式（JSON/XML/CSV）

## 技术栈

- **开发语言**：C#
- **框架版本**：.NET 6.0+
- **GUI框架**：WPF
- **协议标准**：CMIS 5.2 (OIF-CMIS-05.2)
- **开发工具**：Visual Studio 2022

## 快速开始

### 安装

```bash
# 从NuGet安装核心库（待发布）
dotnet add package OpenCMIS.Core
```

### 基本使用

```csharp
using OpenCMIS.Device;
using OpenCMIS.Protocol;

// 创建设备管理器
var deviceManager = new DeviceManager();

// 枚举可用设备
var devices = await deviceManager.EnumerateDevicesAsync();

// 打开设备
var device = await deviceManager.OpenDeviceAsync(devices.First());

// 读取模块信息
var moduleInfo = await device.GetModuleInfoAsync();
Console.WriteLine($"Module: {moduleInfo.VendorName} {moduleInfo.PartNumber}");

// 读取状态
var status = await device.GetStatusAsync();
Console.WriteLine($"State: {status.CurrentState}");

// 控制模块
await device.SetStateAsync(ModuleState.Ready);

// 读取CDB
var cdbManager = new CdbManager();
var cdb = await cdbManager.ReadCdbAsync(device);

// 关闭设备
await device.CloseAsync();
```

## 项目结构

```
OpenCMIS/
├── docs/                  # 文档目录
│   ├── DEVELOPMENT_GUIDE.md    # AI开发手册
│   ├── PROJECT_STRUCTURE.md    # 项目结构说明
│   └── ARCHITECTURE.md         # 架构设计文档
├── src/
│   ├── OpenCMIS.Core/    # 核心库
│   ├── OpenCMIS.GUI/     # GUI应用程序
│   └── OpenCMIS.CLI/     # CLI工具
└── samples/              # 示例代码
```

详细的项目结构说明请参考 [PROJECT_STRUCTURE.md](docs/PROJECT_STRUCTURE.md)

## 文档

- [AI开发手册](docs/DEVELOPMENT_GUIDE.md) - 完整的开发指南和规范
- [项目结构](docs/PROJECT_STRUCTURE.md) - 详细的项目结构说明
- [架构设计](docs/ARCHITECTURE.md) - 系统架构和设计文档
- [CMIS 5.2规范](docs/OIF-CMIS-05.2.pdf) - CMIS协议规范文档（需要引用）

## 开发计划

### Phase 1: 核心库基础功能 ✅
- [x] 项目规划和架构设计
- [ ] 协议层基础实现
- [ ] 设备连接管理
- [ ] 基本寄存器访问
- [ ] 模块信息读取

### Phase 2: 核心库高级功能
- [ ] 模块控制接口
- [ ] 状态监控
- [ ] CDB完整实现
- [ ] 错误处理机制

### Phase 3: GUI应用开发
- [ ] 主界面设计
- [ ] 设备连接界面
- [ ] 信息显示面板
- [ ] 控制操作界面
- [ ] 监控仪表盘

### Phase 4: CLI工具开发
- [ ] 命令行参数解析
- [ ] 基本命令实现
- [ ] 脚本支持
- [ ] 批量操作

### Phase 5: 完善和优化
- [ ] 性能优化
- [ ] 文档完善
- [ ] 示例代码
- [ ] 错误处理增强

## 贡献

欢迎贡献代码、报告问题或提出建议！

## 许可证

本项目采用 [MIT License](LICENSE) 许可证。

## 相关资源

- [CMIS规范](https://www.oiforum.com/) - OIF官方网站
- [CMIS 5.2 PDF](docs/OIF-CMIS-05.2.pdf) - 协议规范文档

## 联系方式

项目维护者：Xin.Pu

---

**注意**：本项目目前处于开发阶段，API可能会发生变化。建议在生产环境使用前仔细测试。
