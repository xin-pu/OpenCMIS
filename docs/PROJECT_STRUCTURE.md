# OpenCMIS 项目结构规划

## 目录结构

```
OpenCMIS/
├── docs/                          # 文档目录
│   ├── DEVELOPMENT_GUIDE.md      # AI开发手册（本文档）
│   ├── PROJECT_STRUCTURE.md      # 项目结构说明（本文件）
│   ├── ARCHITECTURE.md           # 架构设计文档
│   ├── API_REFERENCE.md          # API参考文档
│   └── OIF-CMIS-05.2.pdf        # CMIS协议规范（引用）
│
├── src/                           # 源代码目录
│   ├── OpenCMIS.Core/            # 核心库项目
│   │   ├── Protocol/             # 协议层
│   │   │   ├── Pages/            # 页面定义和访问
│   │   │   │   ├── CmisPage.cs
│   │   │   │   ├── LowerPage.cs
│   │   │   │   ├── UpperPage.cs
│   │   │   │   └── PageManager.cs
│   │   │   ├── Registers/        # 寄存器定义
│   │   │   │   ├── RegisterDefinition.cs
│   │   │   │   ├── RegisterMap.cs
│   │   │   │   └── RegisterAccess.cs
│   │   │   └── Commands/         # 命令处理
│   │   │       ├── CmisCommand.cs
│   │   │       └── CommandProcessor.cs
│   │   │
│   │   ├── Device/               # 设备层
│   │   │   ├── Interfaces/       # 接口定义
│   │   │   │   ├── IDeviceConnection.cs
│   │   │   │   ├── IDeviceManager.cs
│   │   │   │   └── IDeviceMonitor.cs
│   │   │   ├── Connections/      # 连接管理
│   │   │   │   ├── DeviceConnection.cs
│   │   │   │   ├── I2CConnection.cs
│   │   │   │   └── ConnectionFactory.cs
│   │   │   ├── Monitoring/       # 监控功能
│   │   │   │   ├── DeviceMonitor.cs
│   │   │   │   └── StatusMonitor.cs
│   │   │   └── Models/           # 数据模型
│   │   │       ├── ModuleInfo.cs
│   │   │       ├── ModuleStatus.cs
│   │   │       └── DeviceCapabilities.cs
│   │   │
│   │   ├── CDB/                  # CDB配置数据块
│   │   │   ├── Models/           # CDB数据模型
│   │   │   │   ├── CdbStructure.cs
│   │   │   │   ├── CdbField.cs
│   │   │   │   └── CdbVersion.cs
│   │   │   ├── Operations/       # CDB操作
│   │   │   │   ├── CdbReader.cs
│   │   │   │   ├── CdbWriter.cs
│   │   │   │   └── CdbManager.cs
│   │   │   └── Validation/       # CDB验证
│   │   │       ├── CdbValidator.cs
│   │   │       └── CdbChecksum.cs
│   │   │
│   │   ├── Common/               # 公共组件
│   │   │   ├── Utilities/        # 工具类
│   │   │   │   ├── ByteArrayHelper.cs
│   │   │   │   ├── EndianConverter.cs
│   │   │   │   └── CrcCalculator.cs
│   │   │   ├── Exceptions/       # 异常定义
│   │   │   │   ├── CmisException.cs
│   │   │   │   ├── DeviceException.cs
│   │   │   │   └── CdbException.cs
│   │   │   └── Constants/        # 常量定义
│   │   │       ├── CmisConstants.cs
│   │   │       └── RegisterAddresses.cs
│   │   │
│   │   ├── OpenCMIS.Core.csproj  # 项目文件
│   │   └── AssemblyInfo.cs       # 程序集信息
│   │
│   ├── OpenCMIS.GUI/             # GUI应用程序
│   │   ├── Views/                # 视图
│   │   │   ├── MainWindow.xaml
│   │   │   ├── DeviceConnectionView.xaml
│   │   │   ├── ModuleInfoView.xaml
│   │   │   ├── ControlView.xaml
│   │   │   └── MonitorView.xaml
│   │   ├── ViewModels/           # 视图模型（MVVM）
│   │   │   ├── MainViewModel.cs
│   │   │   ├── DeviceViewModel.cs
│   │   │   └── MonitorViewModel.cs
│   │   ├── Models/               # UI模型
│   │   │   └── UiModels.cs
│   │   ├── Services/             # 服务层
│   │   │   ├── DeviceService.cs
│   │   │   └── NotificationService.cs
│   │   ├── Controls/             # 自定义控件
│   │   │   └── CustomControls/
│   │   ├── Resources/            # 资源文件
│   │   │   ├── Styles/
│   │   │   └── Images/
│   │   ├── App.xaml              # 应用程序定义
│   │   ├── App.xaml.cs
│   │   └── OpenCMIS.GUI.csproj
│   │
│   ├── OpenCMIS.CLI/             # 命令行工具
│   │   ├── Commands/             # 命令定义
│   │   │   ├── ConnectCommand.cs
│   │   │   ├── InfoCommand.cs
│   │   │   ├── ControlCommand.cs
│   │   │   ├── MonitorCommand.cs
│   │   │   └── CdbCommand.cs
│   │   ├── Options/              # 命令行选项
│   │   │   └── CommandOptions.cs
│   │   ├── Services/             # 服务
│   │   │   └── CommandService.cs
│   │   ├── Program.cs            # 程序入口
│   │   └── OpenCMIS.CLI.csproj
│   │
│   └── OpenCMIS.Tests/           # 测试项目
│       ├── UnitTests/            # 单元测试
│       │   ├── Protocol/
│       │   ├── Device/
│       │   └── CDB/
│       ├── IntegrationTests/     # 集成测试
│       │   └── DeviceTests/
│       ├── TestHelpers/          # 测试辅助类
│       │   └── MockDevice.cs
│       └── OpenCMIS.Tests.csproj
│
├── samples/                      # 示例代码
│   ├── BasicUsage/               # 基本使用示例
│   ├── AdvancedUsage/            # 高级使用示例
│   └── CDBExamples/              # CDB使用示例
│
├── tools/                        # 工具脚本
│   └── build.ps1                 # 构建脚本
│
├── .gitignore                    # Git忽略文件
├── LICENSE                       # 许可证
├── README.md                     # 项目说明
└── OpenCMIS.sln                  # 解决方案文件
```

## 项目文件说明

### OpenCMIS.Core
核心库，提供CMIS协议的基础实现。

**依赖项**：
- .NET Standard 2.1 或更高版本
- System.IO
- System.Threading.Tasks

**主要功能**：
- CMIS协议解析和封装
- 设备连接和通信
- 寄存器访问
- CDB管理

### OpenCMIS.GUI
基于WPF的图形用户界面应用程序。

**依赖项**：
- .NET 6.0 或更高版本
- OpenCMIS.Core
- WPF框架
- CommunityToolkit.Mvvm（MVVM支持）

**主要功能**：
- 可视化设备管理
- 实时监控仪表盘
- 图形化控制界面
- CDB编辑和查看

### OpenCMIS.CLI
命令行工具，提供脚本化控制能力。

**依赖项**：
- .NET 6.0 或更高版本
- OpenCMIS.Core
- System.CommandLine（命令行解析）

**主要功能**：
- 命令行设备控制
- 批量操作支持
- 脚本执行
- 输出格式化（JSON/XML/CSV）

### OpenCMIS.Tests
测试项目，包含单元测试和集成测试。

**依赖项**：
- .NET 6.0 或更高版本
- xUnit 或 NUnit
- Moq（模拟对象）
- OpenCMIS.Core

## 命名空间结构

```
OpenCMIS
├── OpenCMIS.Protocol
│   ├── Pages
│   ├── Registers
│   └── Commands
├── OpenCMIS.Device
│   ├── Interfaces
│   ├── Connections
│   ├── Monitoring
│   └── Models
├── OpenCMIS.CDB
│   ├── Models
│   ├── Operations
│   └── Validation
└── OpenCMIS.Common
    ├── Utilities
    ├── Exceptions
    └── Constants
```

## 构建和打包

### 项目输出

- **OpenCMIS.Core**：类库 (DLL)
- **OpenCMIS.GUI**：Windows应用程序 (EXE)
- **OpenCMIS.CLI**：控制台应用程序 (EXE)
- **NuGet包**：OpenCMIS.Core可以打包为NuGet包供其他项目使用

### 构建配置

- Debug：开发调试版本
- Release：发布版本
- 目标框架：.NET 6.0 或更高版本

## 开发环境要求

- **Visual Studio 2022** 或更高版本
- **.NET 6.0 SDK** 或更高版本
- **Git** 版本控制
- **CMIS硬件设备**（用于实际测试）

## 下一步行动

1. 创建解决方案和项目结构
2. 设置项目依赖关系
3. 实现核心协议层
4. 开发设备连接接口
5. 实现CDB功能
6. 构建GUI和CLI应用

