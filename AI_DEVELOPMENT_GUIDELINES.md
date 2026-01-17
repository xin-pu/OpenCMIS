# AI Development Guidelines

本文档定义了 OpenCMIS 项目的 AI 辅助开发规范，确保代码质量和一致性。

## 1. 架构分层规范 (v2.0)

项目采用“平铺目录，逻辑分层”的架构，所有子项目均位于 `src/` 目录下，并通过解决方案（SLN）文件夹进行组织。

### 1.1 六层逻辑架构 (Solution Folders)
在解决方案中，项目按以下层级和缩写组织：

1.  **01_Shared**: `OpenCMIS.Shared`。全局基础，包含通用枚举、异常、工具类。**不依赖其他任何层**。
2.  **02_Transport**: 传输层（驱动层）。
    *   `.Abstractions`: 定义 `IDeviceConnection`, `IRegisterTransport`。
    *   `.I2C`: 具体串口 I2C 实现 (TypeA, TypeB)。
3.  **03_Protocol**: 协议层。处理 CMIS 页面切换、寄存器读写逻辑。
    *   `.Abstractions`: 定义 `IRegisterAccess` 等协议契约。
    *   `.Core`: `RegisterAccess`, `PageManager` 实现。
4.  **04_CDB**: 业务服务层。针对 CDB (Configuration Data Block) 和 RAL (Register Abstraction Layer) 的领域逻辑。
5.  **05_App**: 应用编排层。`OpenCMIS.App.Core`。负责设备生命周期、多模块协同及业务流程编排。
6.  **06_UI**: 用户交互层。`OpenCMIS.UI.CLI` 等。

### 1.2 契约优先原则
*   **层间解耦**：层与层之间的调用必须通过对应的 `.Abstractions` 项目。禁止上层项目直接引用下层的 `.Core` 或具体实现项目。
*   **依赖注入**：具体实现的实例化应延迟到 `App` 层或 `UI` 层进行注入。

---

## 2. 串口通信规范 (Serial Transport)

### 2.1 短连接策略 (Short-Connection)
为了支持多应用访问串口的场景（防止 COM 口被长时间独占导致其他工具无法访问），驱动层默认采用**短连接**模式：
*   **原则**：即用即开，用完即关。
*   **基类**：所有串口驱动必须继承 `SerialDeviceConnectionBase`。
*   **执行**：必须通过 `ExecuteAsync` 模板方法执行读写操作，严禁在子类中手动维护持久的 `SerialPort` 实例。

### 2.2 线程安全
*   基类已内置 `SemaphoreSlim`。所有对串口的操作必须受此锁保护，确保同一时间内只有一个原子操作在执行。

---

## 3. 异常处理规范

### 统一异常体系
项目使用统一的异常类型 `CmisException` 配合 `CmisErrorCode` 错误代码，提供结构化的错误处理机制。

### 异常使用原则
- **统一异常类型**：所有异常必须使用 `CmisException`，禁止直接抛出 `Exception`、`IOException` 或其他裸系统异常。
- **错误代码驱动**：使用 `CmisErrorCode` 枚举标识具体错误类型。
- **本地化支持**：错误消息支持中英文描述（通过 `InfoAttribute` 定义）。
- **条件抛出**：优先使用 `CmisException.ThrowIf(condition, ...)` 提高代码可读性。

### 异常使用方式
```csharp
// 基本用法
throw new CmisException(CmisErrorCode.DeviceNotConnected);

// 带格式化参数
throw new CmisException(CmisErrorCode.InvalidRegister, address, page);

// 包装内部异常
throw new CmisException(CmisErrorCode.DeviceCommunicationError, innerException);
```

---

## 4. 文件组织与命名规范

### 4.1 文件独立性原则
**核心规则：除非是内部类，所有独立的类都必须使用独立文件。**
- **每个类使用独立文件**：每个顶级类、接口、枚举、结构体都必须拥有独立文件。
- **文件名一致性**：文件名必须与类名完全匹配（区分大小写）。

### 4.2 命名空间规范
- **反映项目名**：命名空间必须与所属的**项目名称**完全一致。
- **不包含子文件夹**：文件夹结构（如 `Implementations/`）不应反映在命名空间中。
- **示例**：`src/OpenCMIS.Protocol.Core/Implementations/RegisterAccess.cs` 的命名空间应为 `OpenCMIS.Protocol.Core`。

### 4.3 常用缩写
- **CDB**: Configuration Data Block
- **RAL**: Register Abstraction Layer
- **App**: Application
- **I2C**: Inter-Integrated Circuit

---

## 5. 代码风格规范 (C# 12+)

### 5.1 现代语法
- **集合表达式**：优先使用 `[]` 而非 `new byte[] {}` 或 `Array.Empty<byte>()`。
- **密封性**：在基类中已完成的方法实现应使用 `sealed override`，防止子类破坏核心逻辑。

### 5.2 异步编程
- **Async All the Way**：所有 I/O 操作（串口、文件等）必须提供异步接口。
- **Task 包装**：在调用 `SerialPort` 的同步阻塞 API 时，必须包装在 `Task.Run` 中执行，确保不阻塞调用线程。

### 5.3 命名约定
- **类名/方法名/属性**：PascalCase。
- **接口名**：I + PascalCase。
- **私有字段**：_camelCase。
- **参数/局部变量**：camelCase。

---

## 6. 代码注释规范

### 6.1 注释语言
- **默认使用英文注释**。所有公共 API 必须提供英文 XML 文档注释。

### 6.2 注释优先级
1. **类和接口**（必需）：说明类的职责、用途和使用场景。
2. **公共方法和属性**（必需）：说明功能、参数、返回值及异常。
3. **复杂逻辑实现**（按需）：仅针对非直观的算法、边界处理或协议拼包逻辑添加注释。

---

## 总结
遵循以上规范可以确保 OpenCMIS 项目在高度解耦的架构下保持代码一致性和可维护性。在 AI 辅助开发时，请务必先核对**项目引用关系**，确保不破坏分层原则。
