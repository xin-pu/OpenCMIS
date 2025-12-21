# AI Development Guidelines

本文档定义了 OpenCMIS 项目的 AI 辅助开发规范，确保代码质量和一致性。

## 1. 异常处理规范

### 统一异常体系

项目使用统一的异常类型 `CmisException` 配合 `CmisErrorCode` 错误代码，提供结构化的错误处理机制。

### 异常使用原则

- **统一异常类型**：所有异常必须使用 `CmisException`，禁止直接抛出 `Exception` 或其他系统异常
- **错误代码驱动**：使用 `CmisErrorCode` 枚举标识具体错误类型
- **本地化支持**：错误消息支持中英文描述（通过 `InfoAttribute` 定义）
- **错误分类**：错误代码按功能模块分类（设备连接、协议、CDB等）

### 异常使用方式

```csharp
// 基本用法
throw new CmisException(CmisErrorCode.DeviceNotConnected);

// 带格式化参数
throw new CmisException(CmisErrorCode.InvalidRegister, address, page);

// 包装内部异常
throw new CmisException(CmisErrorCode.DeviceCommunicationError, innerException);

// 使用静态方法创建
throw CmisException.Create(CmisErrorCode.DeviceTimeout);

// 包装内部异常
throw CmisException.Wrap(CmisErrorCode.ProtocolViolation, innerException, param1, param2);

// 条件抛出
CmisException.ThrowIf(condition, CmisErrorCode.InvalidParameterValue);
```

### 错误代码定义

错误代码定义在 `Enums/CmisErrorCode.cs` 中，按功能模块分类：

- **0-99**：系统核心错误
- **100-199**：设备连接错误
- **200-299**：协议错误
- **300-399**：CDB错误
- **400-499**：模块状态错误
- **500-599**：数据验证错误
- **9990-9999**：未分类错误

### 错误代码示例

```csharp
public enum CmisErrorCode : ushort
{
    [Info("Device is not connected", "设备未连接")]
    DeviceNotConnected = 100,
    
    [Info("Invalid register address or page", "无效的寄存器地址或页面")]
    InvalidRegister = 200,
    
    [Info("CDB validation failed", "CDB验证失败")]
    CdbValidationFailed = 300,
}
```

## 2. 代码注释规范

### 注释语言

- **默认使用英文注释**
- 所有公共 API 必须提供英文注释
- 内部实现代码可以使用英文注释

### 注释优先级

按照以下优先级添加注释：

1. **类和接口**（必需）
   - 所有公共类和接口必须添加 XML 文档注释
   - 说明类的职责、用途和使用场景

2. **公共方法和属性**（必需）
   - 所有公共方法和属性必须添加 XML 文档注释
   - 说明方法/属性的功能、参数、返回值

3. **函数内部实现**（非必要）
   - **一般情况下不要在函数内部添加额外注释**
   - 只在以下情况添加注释：
     - 复杂的算法或业务逻辑
     - 非直观的实现细节
     - 需要说明的边界条件或特殊处理

### 注释格式

使用标准的 XML 文档注释格式：

```csharp
/// <summary>
///     Represents a CMIS device connection.
/// </summary>
public class DeviceConnection
{
    /// <summary>
    ///     Gets or sets the connection type.
    /// </summary>
    public ConnectionType ConnectionType { get; set; }

    /// <summary>
    ///     Connects to the device asynchronously.
    /// </summary>
    /// <param name="deviceInfo">The device information.</param>
    /// <returns>A task representing the connection operation.</returns>
    /// <exception cref="CmisException">Thrown when connection fails.</exception>
    public async Task ConnectAsync(DeviceInfo deviceInfo)
    {
        // Only add comments for complex logic
        if (deviceInfo == null)
            throw new CmisException(CmisErrorCode.InvalidParameterValue);
        
        // Implementation here without unnecessary comments
        await _connection.OpenAsync();
    }
}
```

### 注释原则

- **说明意图而非实现**：注释应该说明代码的目的，而不是描述代码在做什么
- **简洁明了**：避免冗长的注释，保持简洁
- **避免重复**：不要注释自解释的代码
- **及时更新**：修改代码时必须同步更新相关注释

## 3. 文件组织规范

### 文件独立性原则

**核心规则：除非是内部类，所有独立的类都必须使用独立文件。**

- **每个类使用独立文件**：除了内部类（nested classes）外，每个类都应该放在独立的文件中
- **文件命名与类名一致**：文件名必须与类名完全匹配（区分大小写）
- **禁止多个独立类放在同一文件**：每个独立的类、接口、枚举、结构体都必须有自己的文件

### 内部类规则

内部类可以在同一个文件中定义，但需满足以下条件：

- 内部类仅被外层类使用
- 内部类逻辑上与外层类紧密相关
- 内部类数量不超过 2-3 个
- 内部类必须是嵌套类（nested class），不能是顶级类

### 示例

✅ **正确做法**：

```
Protocol/
├── CommandProcessor.cs      # 只包含 CommandProcessor 类
├── PageManager.cs            # 只包含 PageManager 类
└── RegisterAccess.cs         # 只包含 RegisterAccess 类
```

✅ **包含内部类的正确做法**：

```csharp
// CommandProcessor.cs
public class CommandProcessor
{
    // 内部类可以定义在同一文件
    public class CmisCommand
    {
        public CommandType Type { get; set; }
    }

    public class CommandResult
    {
        public bool Success { get; set; }
    }
}
```

❌ **错误做法**：

```csharp
// ❌ 错误：两个独立的类放在同一文件
public class PageManager { }
public class RegisterAccess { }  // 应该分离到独立文件
```

示例：

```csharp
// CommandProcessor.cs
public class CommandProcessor
{
    // 内部类可以定义在同一文件
    public class CmisCommand
    {
        public CommandType Type { get; set; }
    }

    public class CommandResult
    {
        public bool Success { get; set; }
    }
}
```

### 文件结构示例

```
OpenCMIS.Core/
├── Protocol/
│   ├── PageManager.cs           ✅ 独立文件
│   ├── RegisterAccess.cs        ✅ 独立文件
│   └── CommandProcessor.cs      ✅ 包含内部类 CmisCommand, CommandResult
├── Device/
│   ├── DeviceConnection.cs      ✅ 独立文件
│   ├── DeviceManager.cs         ✅ 独立文件
│   └── Models/
│       ├── DeviceInfo.cs        ✅ 独立文件
│       └── ModuleStatus.cs      ✅ 独立文件
```

## 4. 目录结构规范

### 核心原则

按照 CMIS 功能模块组织代码，遵循以下原则：

1. **公共概念放入 Common**：通用工具、常量、异常、枚举等公共组件都放入 `Common/` 文件夹下的对应子文件夹
2. **功能模块独立文件夹**：按 CMIS 协议的功能模块划分（Protocol、Device、CDB等）
3. **核心契约独立放置**：核心对外的接口或标准协议接口放在上层独立文件夹

### 目录结构

```
OpenCMIS.Core/
├── Common/                      # 公共组件（通用概念）
│   ├── Attributes/             # 特性定义
│   ├── Constants/              # 常量定义
│   ├── Enums/                  # 枚举定义（统一位置）
│   │   ├── CmisErrorCode.cs
│   │   ├── ConnectionType.cs
│   │   ├── ModuleState.cs
│   │   ├── AlertType.cs
│   │   ├── CommandType.cs
│   │   └── CdbFieldType.cs
│   ├── Exceptions/             # 异常定义
│   ├── Extensions/             # 扩展方法
│   │   └── EnumExtensions.cs
│   └── Utilities/              # 工具类
│
├── Extensions/                 # 扩展方法
│   └── EnumExtensions.cs
│
├── Protocol/                   # 协议层（CMIS协议相关）
│   ├── PageManager.cs
│   ├── RegisterAccess.cs
│   ├── CommandProcessor.cs
│   └── IRegisterAccess.cs      # 协议接口
│
├── Device/                     # 设备层（设备管理相关）
│   ├── DeviceConnection.cs
│   ├── DeviceManager.cs
│   ├── DeviceMonitor.cs
│   ├── CmisDevice.cs
│   ├── ICmisDevice.cs          # 设备接口
│   ├── IDeviceConnection.cs    # 设备连接接口
│   └── Models/                 # 设备相关模型
│       ├── DeviceInfo.cs
│       ├── ModuleInfo.cs
│       └── ModuleStatus.cs
│
└── CDB/                        # CDB层（配置数据块相关）
    ├── CdbManager.cs
    ├── CdbReader.cs
    ├── CdbWriter.cs
    ├── CdbValidator.cs
    ├── ICdbReader.cs           # CDB接口
    ├── ICdbWriter.cs
    └── Models/                 # CDB相关模型
        ├── ConfigurationDataBlock.cs
        ├── CdbHeader.cs
        └── CdbField.cs
```

### 目录划分说明

#### Common/ - 公共组件

存放跨模块使用的通用组件：

- **Attributes/**：自定义特性（如 `InfoAttribute`）
- **Constants/**：常量定义（如 `CmisConstants`）
- **Enums/**：枚举定义（所有枚举统一位置）
  - 采用扁平结构，便于查找和管理
  - 保持命名空间为 `OpenCMIS.Core`
  - 按需可以再分类（当枚举数量较多时）
- **Exceptions/**：异常定义（如 `CmisException`）
- **Extensions/**：扩展方法（如 `EnumExtensions`）
  - 使用命名空间 `OpenCMIS.Core.Extensions`（特殊规则）
  - 提供对基础类型或其他类型的扩展方法
- **Utilities/**：通用工具类（如 `ByteArrayHelper`、`CrcCalculator`）

#### Protocol/ - 协议层

CMIS 协议相关的实现：

- 页面管理（Page Manager）
- 寄存器访问（Register Access）
- 命令处理（Command Processing）
- 协议接口定义

#### Device/ - 设备层

设备管理相关的实现：

- 设备连接（Device Connection）
- 设备管理（Device Management）
- 设备监控（Device Monitoring）
- 设备模型定义

#### CDB/ - CDB层

配置数据块（Configuration Data Block）相关的实现：

- CDB 读写操作
- CDB 验证
- CDB 模型定义

### 接口放置原则

#### 核心对外接口

核心对外的契约或标准协议接口应该：

1. **与实现类同目录**：接口通常与实现类放在同一目录下
2. **独立文件**：每个接口使用独立文件
3. **命名规范**：接口名以 `I` 开头

示例：

```
Protocol/
├── IRegisterAccess.cs      # 接口定义
└── RegisterAccess.cs       # 接口实现

Device/
├── ICmisDevice.cs          # 接口定义
└── CmisDevice.cs           # 接口实现
```

#### 标准协议接口

如果接口代表标准协议定义的契约（如 CMIS 规范中的标准接口），可以考虑：

- 放在对应的功能模块文件夹中
- 与实现类保持在同一目录层级
- 确保接口定义清晰，符合协议规范

### 模型文件组织

模型文件（Models）组织在对应模块的子文件夹中：

```
Device/Models/              # 设备相关模型
CDB/Models/                 # CDB相关模型
```

每个模型使用独立文件，除非是简单的值对象（Value Object）可以放在同一文件。

## 5. 命名空间规范

### 命名空间规则

- **基础命名空间**：以项目名作为命名空间（`OpenCMIS.Core`）
- **子文件夹不参与命名空间**：文件夹结构不反映到命名空间
- **特殊规则**：只有 `Extensions` 文件夹可以使用子命名空间（`OpenCMIS.Core.Extensions`）

### 命名空间示例

```
文件位置：OpenCMIS.Core/Protocol/PageManager.cs
命名空间：OpenCMIS.Core

文件位置：OpenCMIS.Core/Device/Models/DeviceInfo.cs
命名空间：OpenCMIS.Core

文件位置：OpenCMIS.Core/Common/Extensions/EnumExtensions.cs
命名空间：OpenCMIS.Core.Extensions
```

## 6. 代码风格规范

### 命名约定

- **类名**：PascalCase（如 `DeviceManager`）
- **接口名**：I + PascalCase（如 `ICmisDevice`）
- **方法名**：PascalCase（如 `ConnectAsync`）
- **属性名**：PascalCase（如 `ConnectionType`）
- **私有字段**：_camelCase（如 `_deviceConnection`）
- **参数名**：camelCase（如 `deviceInfo`）
- **局部变量**：camelCase（如 `result`）

### 异步编程

- 所有 I/O 操作必须使用异步方法
- 异步方法命名：`MethodNameAsync()`
- 返回类型：`Task<T>` 或 `Task`
- 避免使用 `async void`（事件处理程序除外）

### 其他规范

- 使用 `var` 关键字，除非类型不明显
- 优先使用表达式主体成员（Expression-bodied members）
- 使用 null 条件运算符（`?.`）和空合并运算符（`??`）
- 优先使用 LINQ，但保持代码可读性

## 总结

遵循以上规范可以确保：

1. **代码一致性**：统一的异常处理和代码风格
2. **可维护性**：清晰的文件组织和注释
3. **可扩展性**：合理的模块划分和接口设计
4. **可读性**：适当的注释和命名规范

在 AI 辅助开发时，请严格遵循以上规范。

