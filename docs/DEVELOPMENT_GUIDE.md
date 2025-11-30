# OpenCMIS AI 开发手册

## 项目概述

### 项目名称
OpenCMIS - 基于CMIS协议的通用光模块控制库

### 项目目标
开发一个基于CMIS (Common Management Interface Specification) 5.2协议的通用库，提供快速访问光模块应用的能力，包括：

- **模块信息获取**：读取光模块的各种状态和信息
- **模块控制**：控制光模块的各种功能和参数
- **模块监控**：实时监控光模块的运行状态
- **CDB实现**：实现Configuration Data Block的管理和操作

### 项目组件

1. **OpenCMIS.Core** - 核心库
   - CMIS协议实现
   - 底层通信接口
   - 数据模型定义

2. **OpenCMIS.GUI** - 图形用户界面应用
   - 基于核心库的定制GUI
   - 可视化模块信息和控制界面
   - 实时监控仪表盘

3. **OpenCMIS.CLI** - 命令行工具
   - 基于核心库的命令行接口
   - 脚本化控制能力
   - 批量操作支持

## 技术栈

- **开发语言**：C# (.NET)
- **框架版本**：.NET 6.0 或更高版本
- **GUI框架**：WPF 或 Avalonia（跨平台支持）
- **协议标准**：CMIS 5.2
- **开发工具**：Visual Studio 2022

## 相关文档

### 协议文档
- **CMIS 5.2规范**：`OIF-CMIS-05.2.pdf`
  - 位置：解决方案中的docs文件夹引用
  - 包含完整的CMIS协议规范定义

### 关键协议概念

#### CMIS协议核心概念
- **Module State Machine**：模块状态机
- **Page Access**：页面访问机制
- **Memory Map**：内存映射结构
- **CDB (Configuration Data Block)**：配置数据块
- **I2C Interface**：I2C通信接口
- **Register Access**：寄存器访问

#### 主要功能模块
1. **Identification and Capabilities**
   - 模块识别信息
   - 能力和特性查询

2. **Control and Status**
   - 模块控制接口
   - 状态监控

3. **Diagnostic Monitoring**
   - 诊断信息
   - 性能监控

4. **Configuration**
   - 配置管理
   - CDB操作

## 项目架构

### 分层架构

```
┌─────────────────────────────────────┐
│      OpenCMIS.GUI / OpenCMIS.CLI    │  ← 应用层
├─────────────────────────────────────┤
│         OpenCMIS.Core               │  ← 核心库层
│  ┌──────────┬──────────┬──────────┐ │
│  │ Protocol │  Device  │   CDB    │ │
│  │  Layer   │  Layer   │  Layer   │ │
│  └──────────┴──────────┴──────────┘ │
├─────────────────────────────────────┤
│      Hardware Interface Layer       │  ← 硬件接口层
│      (I2C/SPI/USB 等)               │
└─────────────────────────────────────┘
```

### 核心模块设计

#### 1. Protocol Layer（协议层）
- CMIS协议解析和封装
- 寄存器映射定义
- 页面访问管理
- 命令编码/解码

#### 2. Device Layer（设备层）
- 设备连接管理
- 通信接口抽象
- 错误处理和重试机制
- 设备枚举和识别

#### 3. CDB Layer（配置数据块层）
- CDB数据模型
- CDB读写操作
- CDB验证和校验
- CDB版本管理

## 开发规范

### 代码组织

```
OpenCMIS.Core/
├── Protocol/          # 协议实现
│   ├── Pages/        # 页面定义
│   ├── Registers/    # 寄存器定义
│   └── Commands/     # 命令处理
├── Device/           # 设备管理
│   ├── Interfaces/   # 接口定义
│   ├── Connections/  # 连接管理
│   └── Monitoring/   # 监控功能
├── CDB/              # CDB相关
│   ├── Models/       # 数据模型
│   ├── Operations/   # CDB操作
│   └── Validation/   # 验证逻辑
└── Common/           # 公共组件
    ├── Utilities/    # 工具类
    └── Exceptions/   # 异常定义
```

### 命名约定

- **命名空间**：`OpenCMIS.{Module}.{SubModule}`
  - 示例：`OpenCMIS.Protocol.Pages`, `OpenCMIS.Device.Interfaces`

- **类名**：PascalCase
  - 示例：`CmisDevice`, `ConfigurationDataBlock`

- **接口名**：I + PascalCase
  - 示例：`IDeviceConnection`, `ICdbManager`

- **方法名**：PascalCase
  - 示例：`ReadRegister()`, `WriteConfiguration()`

- **私有字段**：_camelCase
  - 示例：`_deviceConnection`, `_isConnected`

### 异步编程

- 所有I/O操作使用异步方法
- 方法命名：`MethodNameAsync()`
- 返回类型：`Task<T>` 或 `Task`

```csharp
public async Task<ModuleInfo> GetModuleInfoAsync()
public async Task<bool> WriteRegisterAsync(byte page, byte address, byte value)
```

### 异常处理

- 使用自定义异常类型
- 提供详细的错误信息
- 区分可恢复和不可恢复错误

```csharp
public class CmisException : Exception
public class DeviceNotConnectedException : CmisException
public class InvalidRegisterException : CmisException
```

### 日志记录

- 使用结构化日志（如Serilog或NLog）
- 记录关键操作和错误
- 提供不同日志级别（Debug, Info, Warning, Error）

## 开发流程

### 1. 需求分析
- 分析CMIS协议文档
- 定义功能需求
- 确定接口规范

### 2. 设计阶段
- 设计类结构和接口
- 定义数据模型
- 规划模块交互

### 3. 实现阶段
- 先实现核心协议层
- 逐步添加设备层功能
- 最后实现CDB层

### 4. 测试阶段
- 单元测试
- 集成测试
- 硬件测试

### 5. 文档编写
- API文档
- 使用示例
- 故障排除指南

## 关键实现点

### CMIS寄存器访问

CMIS协议基于页面和寄存器地址访问：

```csharp
public interface IRegisterAccess
{
    Task<byte> ReadByteAsync(byte page, byte address);
    Task WriteByteAsync(byte page, byte address, byte value);
    Task<byte[]> ReadBlockAsync(byte page, byte startAddress, int length);
    Task WriteBlockAsync(byte page, byte startAddress, byte[] data);
}
```

### 页面管理

CMIS协议使用页面机制组织寄存器：
- Lower page (0x00-0x7F)
- Upper page (0x80-0xFF)
- Page selection register

### 模块状态管理

实现模块状态机：
- Initialization
- LowPwr
- PwrUp
- Ready
- PwrDn
- Fault

### CDB操作

CDB是配置数据块的二进制格式：
- CDB读取
- CDB写入
- CDB验证
- CDB应用

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

## 后续开发计划

### Phase 1: 核心库基础功能
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

## 参考资源

- [CMIS 5.2 规范文档](OIF-CMIS-05.2.pdf)
- [OIF (Optical Internetworking Forum) 官方网站](https://www.oiforum.com/)
- [相关IEEE标准](https://www.ieee802.org/3/)

## 联系方式

项目维护者：Xin.Pu

## 版本历史

- **v0.1.0** (2025-01) - 初始版本，项目规划和架构设计

