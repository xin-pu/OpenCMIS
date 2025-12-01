# OpenCMIS

基于CMIS (Common Management Interface Specification) 5.2协议的通用光模块控制库。

## 项目简介

OpenCMIS是一个开源的光模块控制库，提供了完整的CMIS协议实现，可以快速访问、控制和监控符合CMIS标准的光模块设备。项目包含核心库、图形用户界面（GUI）和命令行工具（CLI）三个主要组件。

### 核心价值

- **协议标准化** - 严格遵循CMIS 5.2协议规范，确保兼容性
- **易用性** - 提供简洁的API，降低开发门槛
- **完整性** - 涵盖协议实现、GUI工具、CLI工具的全套解决方案
- **可扩展性** - 模块化设计，易于扩展和定制
- **跨平台** - 基于.NET，支持Windows、Linux和macOS

### 目标用户

- 光模块制造商 - 用于模块测试和验证
- 系统集成商 - 用于系统开发和集成
- 研发工程师 - 用于快速原型开发
- 测试工程师 - 用于自动化测试脚本

## 主要功能

- ✅ **模块信息获取** - 读取光模块的识别信息、能力信息和状态信息
- ✅ **模块控制** - 控制光模块的各种功能和参数
- ✅ **实时监控** - 监控光模块的运行状态、告警和性能指标
- ✅ **CDB管理** - 实现Configuration Data Block的读取、写入、验证和应用
- ✅ **多接口支持** - 支持I2C、USB、串口等多种硬件接口
- ✅ **跨平台** - 基于.NET，支持Windows、Linux和macOS

## 项目组件

### OpenCMIS.Core
核心库，提供CMIS协议的基础实现和API接口。包含协议层、设备层和CDB层三个核心模块。

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
- **框架版本**：.NET 8.0
- **GUI框架**：WPF + DevExpress控件
- **日志框架**：Serilog
- **协议标准**：CMIS 5.2 (OIF-CMIS-05.2)
- **开发工具**：Visual Studio 2022

## 系统架构

### 分层架构

```
┌─────────────────────────────────────────────────────────────┐
│                        应用层                                │
├──────────────────────────┬──────────────────────────────────┤
│    OpenCMIS.GUI          │      OpenCMIS.CLI                │
│   (WPF应用程序)          │    (命令行工具)                   │
└──────────────┬───────────┴────────────┬─────────────────────┘
               │                        │
               └──────────┬─────────────┘
                          │
┌─────────────────────────┼─────────────────────────────────┐
│                   OpenCMIS.Core 核心库                     │
├─────────────────────────┼─────────────────────────────────┤
│   CDB Layer             │     Device Layer                │
│  ┌─────────────────────┐│  ┌─────────────────────────────┐│
│  │ CdbManager          ││  │ DeviceManager               ││
│  │ CdbReader/Writer    ││  │ DeviceConnection            ││
│  │ CdbValidator        ││  │ DeviceMonitor               ││
│  └─────────────────────┘│  └─────────────────────────────┘│
│                         │                                 │
│   Protocol Layer        │                                 │
│  ┌───────────────────────────────────────────────────────┐│
│  │ PageManager                                           ││
│  │ RegisterAccess                                        ││
│  │ CommandProcessor                                      ││
│  └───────────────────────────────────────────────────────┘│
└─────────────────────────┼─────────────────────────────────┘
                          │
┌─────────────────────────┼─────────────────────────────────┐
│                 硬件接口抽象层                              │
│  ┌─────────────────────┐  ┌─────────────────────────────┐│
│  │ I2C Interface       │  │ USB Interface               ││
│  │ SPI Interface       │  │ Serial Interface            ││
│  └─────────────────────┘  └─────────────────────────────┘│
└───────────────────────────────────────────────────────────┘
                          │
┌─────────────────────────┼─────────────────────────────────┐
│                    硬件设备层                               │
│              (CMIS兼容的光模块)                             │
└───────────────────────────────────────────────────────────┘
```

### 核心模块设计

#### 1. Protocol Layer（协议层）
负责CMIS协议的底层实现，包括：
- **页面管理（PageManager）**：页面切换、锁定/解锁、访问控制
- **寄存器访问（RegisterAccess）**：统一的寄存器读写接口
- **命令处理（CommandProcessor）**：处理CMIS协议定义的各种命令

#### 2. Device Layer（设备层）
管理硬件设备的连接、状态和监控：
- **设备连接（DeviceConnection）**：抽象不同硬件接口（I2C、USB、SPI等）
- **设备管理（DeviceManager）**：设备生命周期、枚举、识别
- **设备监控（DeviceMonitor）**：实时监控、状态变化、告警通知

#### 3. CDB Layer（配置数据块层）
处理配置数据块的读取、写入、验证和应用：
- **CDB结构模型**：定义CDB数据结构和字段
- **CDB管理器**：CDB读写、验证和应用操作
- **CDB验证器**：确保CDB数据的完整性和有效性

### 关键设计原则

1. **分层解耦** - 各层职责清晰，接口明确
2. **依赖倒置** - 依赖抽象而非具体实现
3. **单一职责** - 每个类只负责一个功能
4. **开闭原则** - 对扩展开放，对修改关闭
5. **异步优先** - I/O操作全部采用异步模式

## 项目结构

```
OpenCMIS/
├── docs/                  # 文档目录
│   └── OIF-CMIS-05.2.pdf # CMIS协议规范
├── src/
│   ├── OpenCMIS.Core/    # 核心库
│   │   ├── Protocol/     # 协议层（页面、寄存器、命令）
│   │   ├── Device/       # 设备层（连接、管理、监控）
│   │   ├── CDB/          # CDB层（模型、操作、验证）
│   │   └── Common/       # 公共组件（工具类、异常、常量）
│   ├── OpenCMIS.GUI/     # GUI应用程序
│   │   ├── Views/        # 视图
│   │   ├── ViewModels/   # 视图模型（MVVM）
│   │   └── Services/     # 服务层
│   ├── OpenCMIS.CLI/     # CLI工具
│   │   ├── Commands/     # 命令定义
│   │   └── Services/     # 服务
│   └── OpenCMIS.Tests/   # 测试项目
├── samples/              # 示例代码
├── LICENSE               # 许可证
└── README.md             # 项目说明
```

### 命名空间结构

```
OpenCMIS.Core
├── Protocol/            # 协议相关文件，命名空间：OpenCMIS.Core
├── Device/              # 设备相关文件，命名空间：OpenCMIS.Core
├── CDB/                 # CDB相关文件，命名空间：OpenCMIS.Core
├── Common/              # 公共组件文件，命名空间：OpenCMIS.Core
└── Extensions/          # 扩展方法，命名空间：OpenCMIS.Core.Extensions

OpenCMIS.GUI
├── Views/               # 视图文件，命名空间：OpenCMIS.GUI
├── ViewModels/          # 视图模型，命名空间：OpenCMIS.GUI
├── Services/            # 服务文件，命名空间：OpenCMIS.GUI
└── Extensions/          # 扩展方法，命名空间：OpenCMIS.GUI.Extensions

OpenCMIS.CLI
├── Commands/            # 命令文件，命名空间：OpenCMIS.CLI
├── Services/            # 服务文件，命名空间：OpenCMIS.CLI
└── Extensions/          # 扩展方法，命名空间：OpenCMIS.CLI.Extensions
```

**说明**：
- 子文件夹下的代码默认直接以项目名作为命名空间，子文件夹名不参与命名空间
- 只有 `Extensions` 文件夹可以放入命名空间（如 `OpenCMIS.Core.Extensions`）

## 开发规范

### 命名约定

- **命名空间**：以项目名作为命名空间，子文件夹下的代码默认直接以项目名作为命名空间，子文件夹名不参与到命名空间
  - 示例：`OpenCMIS.Core`、`OpenCMIS.GUI`、`OpenCMIS.CLI`
  - 子文件夹下的文件：`OpenCMIS.Core/Protocol/PageManager.cs` 使用命名空间 `OpenCMIS.Core`（不包含 Protocol）
  - 特殊规则：只有 `Extensions` 文件夹可以放入命名空间，如 `OpenCMIS.Core.Extensions`
- **类名**：PascalCase
- **接口名**：I + PascalCase
- **方法名**：PascalCase
- **私有字段**：_camelCase

### 代码注释

- **全代码使用英文注释**
- 所有公共类、方法、属性必须添加XML文档注释
- 复杂逻辑和算法需要添加行内注释说明
- 注释应清晰、简洁，说明代码的意图而非实现细节

### 异步编程

- 所有I/O操作使用异步方法
- 方法命名：`MethodNameAsync()`
- 返回类型：`Task<T>` 或 `Task`

### 异常处理

- 使用自定义异常类型
- 提供详细的错误信息
- 区分可恢复和不可恢复错误

主要异常类型：
- `CmisException` - 基础异常
- `DeviceNotConnectedException` - 设备未连接
- `DeviceTimeoutException` - 设备超时
- `InvalidRegisterException` - 无效寄存器
- `CdbValidationException` - CDB验证失败

### 日志记录

使用Serilog进行结构化日志记录：

**日志级别使用规范**：
- **Debug**：详细的调试信息，仅在开发时使用
- **Information**：一般信息，如操作成功、状态变化
- **Warning**：警告信息，如超时、重试等
- **Error**：错误信息，需要记录异常详情
- **Fatal**：严重错误，可能导致程序终止

## CMIS协议核心概念

### 关键协议概念

- **Module State Machine**：模块状态机（Initialization → LowPwr → PwrUp → Ready → PwrDn/Fault）
- **Page Access**：页面访问机制（Lower page 0x00-0x7F, Upper page 0x80-0xFF）
- **Memory Map**：内存映射结构
- **CDB (Configuration Data Block)**：配置数据块
- **I2C Interface**：I2C通信接口
- **Register Access**：寄存器访问

### 主要功能模块

1. **Identification and Capabilities** - 模块识别信息和能力查询
2. **Control and Status** - 模块控制接口和状态监控
3. **Diagnostic Monitoring** - 诊断信息和性能监控
4. **Configuration** - 配置管理和CDB操作

## 开发计划

### Phase 1: 核心库基础功能
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

## 开发阶段规划

### 第一阶段：核心库基础（4-6周）
完成核心库的基础功能，能够基本访问和控制光模块。

### 第二阶段：核心库增强（4-6周）
完善核心库功能，增加CDB支持和监控能力。

### 第三阶段：GUI开发（6-8周）
开发易用的图形用户界面。

### 第四阶段：CLI开发（3-4周）
开发命令行工具。

### 第五阶段：完善和发布（2-3周）
完善文档、示例代码和发布准备。

## 测试策略

### 单元测试
- 协议解析逻辑
- 数据转换函数
- CDB验证算法

### 集成测试
- 设备连接流程
- 完整操作序列
- 错误恢复机制

### 硬件测试
- 真实设备测试
- 边界条件测试
- 性能测试

## 相关资源

- [CMIS规范](https://www.oiforum.com/) - OIF官方网站
- CMIS 5.2 PDF - 协议规范文档（位于docs目录）

## 贡献

欢迎贡献代码、报告问题或提出建议！

## 许可证

本项目采用 [MIT License](LICENSE) 许可证。

## 联系方式

项目维护者：Xin.Pu

---

**注意**：本项目目前处于开发阶段，API可能会发生变化。建议在生产环境使用前仔细测试。
