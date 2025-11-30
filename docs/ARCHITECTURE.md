# OpenCMIS 架构设计文档

## 架构概览

OpenCMIS采用分层架构设计，将协议实现、设备管理、应用层清晰分离，确保代码的可维护性和可扩展性。

## 系统架构图

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

## 核心模块设计

### 1. Protocol Layer（协议层）

协议层负责CMIS协议的底层实现，包括页面管理、寄存器访问和命令处理。

#### 1.1 页面管理（PageManager）

CMIS协议使用页面机制组织寄存器空间。页面管理器负责：
- 页面切换
- 页面锁定/解锁
- 页面访问控制

**关键类**：
```csharp
public class PageManager
{
    // 切换到指定页面
    Task<bool> SwitchPageAsync(byte pageNumber);
    
    // 获取当前页面
    byte GetCurrentPage();
    
    // 锁定页面
    Task<bool> LockPageAsync(byte pageNumber);
    
    // 解锁页面
    Task UnlockPageAsync(byte pageNumber);
}
```

#### 1.2 寄存器访问（RegisterAccess）

提供统一的寄存器读写接口，抽象底层通信细节。

**接口定义**：
```csharp
public interface IRegisterAccess
{
    // 读取单个字节
    Task<byte> ReadByteAsync(byte page, byte address);
    
    // 写入单个字节
    Task WriteByteAsync(byte page, byte address, byte value);
    
    // 读取数据块
    Task<byte[]> ReadBlockAsync(byte page, byte startAddress, int length);
    
    // 写入数据块
    Task WriteBlockAsync(byte page, byte startAddress, byte[] data);
}
```

#### 1.3 命令处理（CommandProcessor）

处理CMIS协议定义的各种命令，如状态机控制、模块初始化等。

### 2. Device Layer（设备层）

设备层管理硬件设备的连接、状态和监控。

#### 2.1 设备连接（DeviceConnection）

抽象不同硬件接口（I2C、USB、SPI等）的连接方式。

**接口定义**：
```csharp
public interface IDeviceConnection : IDisposable
{
    // 连接状态
    bool IsConnected { get; }
    
    // 打开连接
    Task<bool> OpenAsync();
    
    // 关闭连接
    Task CloseAsync();
    
    // 读取数据
    Task<byte[]> ReadAsync(int length);
    
    // 写入数据
    Task WriteAsync(byte[] data);
}
```

**实现类**：
- `I2CConnection`：I2C接口实现
- `USBConnection`：USB接口实现
- `SerialConnection`：串口接口实现

#### 2.2 设备管理（DeviceManager）

管理设备生命周期、枚举、识别等功能。

**关键类**：
```csharp
public class DeviceManager
{
    // 枚举可用设备
    Task<IEnumerable<DeviceInfo>> EnumerateDevicesAsync();
    
    // 打开设备
    Task<ICmisDevice> OpenDeviceAsync(DeviceInfo deviceInfo);
    
    // 关闭设备
    Task CloseDeviceAsync(ICmisDevice device);
}
```

#### 2.3 设备监控（DeviceMonitor）

提供实时监控能力，包括状态变化、告警通知等。

**关键类**：
```csharp
public class DeviceMonitor
{
    // 开始监控
    Task StartMonitoringAsync(TimeSpan interval);
    
    // 停止监控
    Task StopMonitoringAsync();
    
    // 状态变化事件
    event EventHandler<StatusChangedEventArgs> StatusChanged;
    
    // 告警事件
    event EventHandler<AlertEventArgs> Alert;
}
```

### 3. CDB Layer（配置数据块层）

CDB层处理配置数据块的读取、写入、验证和应用。

#### 3.1 CDB结构模型

```csharp
public class ConfigurationDataBlock
{
    // CDB头部信息
    public CdbHeader Header { get; set; }
    
    // CDB字段集合
    public ICollection<CdbField> Fields { get; set; }
    
    // 校验和
    public ushort Checksum { get; set; }
    
    // CDB版本
    public CdbVersion Version { get; set; }
}
```

#### 3.2 CDB管理器

```csharp
public class CdbManager
{
    // 从设备读取CDB
    Task<ConfigurationDataBlock> ReadCdbAsync(ICmisDevice device);
    
    // 写入CDB到设备
    Task WriteCdbAsync(ICmisDevice device, ConfigurationDataBlock cdb);
    
    // 验证CDB
    bool ValidateCdb(ConfigurationDataBlock cdb);
    
    // 应用CDB配置
    Task ApplyCdbAsync(ICmisDevice device, ConfigurationDataBlock cdb);
}
```

#### 3.3 CDB验证器

确保CDB数据的完整性和有效性：
- 校验和验证
- 字段范围检查
- 依赖关系验证

### 4. 公共组件层（Common）

提供通用的工具类和基础设施。

#### 4.1 异常处理

```csharp
// 基础异常
public class CmisException : Exception

// 设备相关异常
public class DeviceNotConnectedException : CmisException
public class DeviceTimeoutException : CmisException
public class DeviceCommunicationException : CmisException

// 协议相关异常
public class InvalidRegisterException : CmisException
public class ProtocolViolationException : CmisException

// CDB相关异常
public class CdbValidationException : CmisException
public class CdbVersionMismatchException : CmisException
```

#### 4.2 工具类

- `ByteArrayHelper`：字节数组操作工具
- `EndianConverter`：字节序转换
- `CrcCalculator`：CRC校验计算
- `LoggingHelper`：日志记录辅助

## 数据流设计

### 读取寄存器数据流

```
GUI/CLI应用
    ↓
DeviceManager.GetDevice()
    ↓
ICmisDevice.ReadRegisterAsync()
    ↓
PageManager.SwitchPageAsync() (如需要)
    ↓
RegisterAccess.ReadByteAsync()
    ↓
IDeviceConnection.ReadAsync()
    ↓
硬件设备
```

### CDB写入数据流

```
GUI/CLI应用
    ↓
CdbManager.WriteCdbAsync()
    ↓
CdbValidator.Validate()
    ↓
CdbWriter.Serialize()
    ↓
DeviceManager.WriteBlockAsync()
    ↓
硬件设备
```

## 状态管理

### 模块状态机

CMIS协议定义了模块的状态机，需要正确管理状态转换：

```
Initialization → LowPwr → PwrUp → Ready
                                    ↓
                                PwrDn → Fault
```

**状态管理类**：
```csharp
public class ModuleStateMachine
{
    public ModuleState CurrentState { get; }
    
    // 状态转换
    Task<bool> TransitionToAsync(ModuleState targetState);
    
    // 检查状态转换是否合法
    bool CanTransitionTo(ModuleState targetState);
}
```

### 连接状态管理

设备连接状态包括：
- Disconnected：未连接
- Connecting：连接中
- Connected：已连接
- Error：连接错误

## 错误处理策略

### 重试机制

对于临时性错误（如通信超时），实现自动重试：

```csharp
public class RetryPolicy
{
    public int MaxRetries { get; set; }
    public TimeSpan Delay { get; set; }
    
    Task<T> ExecuteAsync<T>(Func<Task<T>> operation);
}
```

### 错误分类

1. **可恢复错误**：网络超时、临时通信失败
   - 策略：自动重试

2. **不可恢复错误**：设备不存在、协议版本不兼容
   - 策略：抛出异常，终止操作

3. **业务逻辑错误**：无效参数、状态不允许
   - 策略：返回错误码或抛出特定异常

## 性能优化考虑

### 批量操作

对于需要读取多个寄存器的场景，支持批量读取：

```csharp
// 批量读取多个寄存器
Task<Dictionary<RegisterAddress, byte>> ReadRegistersAsync(
    IEnumerable<RegisterAddress> addresses);
```

### 缓存机制

缓存静态信息（如模块识别信息），减少不必要的访问：

```csharp
public class DeviceInfoCache
{
    private readonly Dictionary<string, ModuleInfo> _cache;
    
    Task<ModuleInfo> GetModuleInfoAsync(ICmisDevice device);
}
```

### 异步操作

所有I/O操作采用异步模式，避免阻塞UI线程。

## 扩展性设计

### 插件架构

支持通过插件扩展功能：

```csharp
public interface IDevicePlugin
{
    string Name { get; }
    void Initialize(IDeviceManager deviceManager);
    void Shutdown();
}
```

### 自定义硬件接口

通过实现`IDeviceConnection`接口，可以支持新的硬件连接方式。

### 自定义CDB格式

通过实现`ICdbReader`和`ICdbWriter`接口，可以支持不同版本的CDB格式。

## 安全考虑

### 访问控制

- 只读操作和读写操作的权限控制
- 关键操作的确认机制

### 数据验证

- 输入参数验证
- CDB数据完整性校验
- 寄存器值范围检查

## 测试策略

### 单元测试

- 协议层：寄存器访问、页面管理
- CDB层：CDB读写、验证逻辑
- 工具类：数据转换、校验计算

### 集成测试

- 完整设备操作流程
- 错误恢复机制
- 状态转换验证

### 模拟测试

使用模拟设备进行测试，避免依赖真实硬件：

```csharp
public class MockDevice : IDeviceConnection
{
    // 模拟设备行为
}
```

