# I2C、MSA 与 HCI 核心经验

本文收敛以下内部培训材料中的工程经验，作为 OpenCMIS 的长期协议参考：

- `Introduction to I2C 3.pptx`
- `Introduction to Module Communication 1.pptx`

原始材料日期为 2022 年。本文保留其中与实现有关的内容，但不能替代具体
MSA、器件数据手册或硬件验证结果。

## 1. I2C 基础

I2C 是双线、双向、半双工串行总线：

- `SCL`：时钟线，由 Controller/Master 控制；
- `SDA`：数据线；
- Controller 发起传输并提供时钟；
- Peripheral/Slave 只能在 Clock Stretching 等场景拉低时钟线。

SCL 和 SDA 通常使用开漏输出与上拉电阻。总线参与方主动拉低线路，释放线路
后由上拉电阻恢复高电平，因此多个设备共享线路时形成 wired-AND 行为。

常见速率：

| 模式 | 速率 |
| --- | ---: |
| Standard Mode | 100 kbit/s |
| Fast Mode | 400 kbit/s |
| Fast Mode Plus | 1 Mbit/s |
| High Speed Mode | 3.4 Mbit/s |

模块通信通常使用 400 kHz。具体模块可能支持 1 MHz，但必须由模块规范、适配器
能力和时序测试共同确认。

## 2. I2C 地址表示

I2C Peripheral 使用 7-bit 地址。总线上可正常分配的地址通常位于
`0x08..0x77`。

光模块资料经常使用包含 R/W 位的 8-bit 表示法，例如：

- 8-bit 写地址 `0xA0`；
- 对应 7-bit 地址 `0x50`；
- 8-bit 读地址 `0xA1`。

OpenCMIS 核心应统一保存 7-bit 地址，只在硬件协议边界需要时转换为 8-bit
读写地址，避免把 `0xA0` 与 `0x50` 当成两个设备。

## 3. I2C 传输语义

基本传输由以下条件组成：

- START：SCL 为高时，SDA 从高变低；
- STOP：SCL 为高时，SDA 从低变高；
- ACK：第九个时钟周期中，接收方拉低 SDA；
- NACK：第九个时钟周期中，SDA 保持高电平。

正常数据位只能在 SCL 为低时变化，并在 SCL 为高期间保持稳定。

### 3.1 顺序写

典型 byte/sequential write：

```text
START
  -> Device Address + Write
  -> ACK
  -> Register Address
  -> ACK
  -> Data Byte 1
  -> ACK
  -> ...
  -> Data Byte N
  -> ACK
  -> STOP
```

连续发送数据会写入后续地址，但一次传输的最大长度、跨页行为和硬件分段限制
必须由具体适配器实现处理。

### 3.2 随机读与 Dummy Write

常见读操作先通过一次 Dummy Write 设置模块内部地址计数器，再发 Repeated
START 切换到读方向：

```text
START
  -> Device Address + Write
  -> Register Address
  -> Repeated START
  -> Device Address + Read
  -> Data ...
  -> NACK
  -> STOP
```

因此驱动层不能把“设置寄存器地址”和“读取数据”拆成可被其他会话操作穿插的
两个无关事务。

## 4. MSA 内存结构

模块通过 I2C 暴露 MSA 内存，默认地址通常为 8-bit `0xA0`，即 7-bit
`0x50`。少数模块可能使用额外地址。

一个完整的 256-byte 地址空间分为：

- Lower：`0x00..0x7F`，128 bytes；
- Upper：`0x80..0xFF`，128 bytes。

Table/Page 0 具有 lower 和 upper。其他 Page/Table 通常只通过 upper 区访问。
Bank Select 和 Page Select 字节用于切换 upper 区映射。

OpenCMIS 必须把以下操作放在同一个模块会话锁内：

```text
select Bank
  -> select Page
  -> read/write mapped bytes
```

否则并发 MSA 或 HCI 操作可能在传输中途改变页面，导致数据来自错误的表。

## 5. HCI 访问

HCI 是 vendor-specific 扩展，用于访问不能像普通 MSA 一样直接寻址的内部
数据，例如：

- 配置和硬件默认值；
- 内部硬件寄存器；
- 其他内部表。

HCI buffer 位于 Page/Table `0x7F` upper 区。

### 5.1 关键地址与状态

| 地址 | 含义 |
| --- | --- |
| Lower password area | FNSR password `DF 5E 75 CD` |
| `0x7F` | Page Select；写入 `0x7F` 进入 HCI buffer page |
| `0x80` | HCI status |
| `0x81...` | HCI message，读命令的返回数据紧随消息 |

状态值：

- `0x00`：OK/ready；
- `0x7E`：NEW_CMD，用于提交新命令。

密码和命令格式属于 vendor-specific 协议。若未来支持不同 vendor，不能默认
所有模块都接受相同密码或 HCI 表布局；应通过 profile/options 配置。

### 5.2 HCI 完整顺序

```text
write FNSR password
  -> write 0x7F to Page Select at 0x7F
  -> poll status 0x80 until ready (0x00)
  -> write HCI message at 0x81
  -> write NEW_CMD (0x7E) to status 0x80
  -> poll status 0x80 until ready (0x00)
  -> for reads, read response bytes following the HCI message
```

整个顺序必须与 MSA Page 访问共享同一个 `OpticalModuleSession` gate。轮询必须
支持 timeout 和 cancellation，不能无限等待 busy 状态。

### 5.3 HCI 消息字段

培训材料给出的七字节头格式：

| 相对位置 | 含义 |
| ---: | --- |
| `0x00` | 命令：`0x00` 读，`0x01` 写 |
| `0x01..0x02` | 保留，当前行为写零 |
| `0x03` | 内部 Table number |
| `0x04` | 起始 byte offset |
| `0x05` | 数据类型：`0x80` Byte、`0x00` Word、`0x20` Long |
| `0x06` | 数据长度 |
| `0x07...` | 写入 payload；读响应数据位于返回消息之后 |

当前 OpenCMIS codec 使用 Byte 类型 `0x80`。Word/Long 类型若要暴露，应先增加
独立测试，不能只改变 UI 标签。

## 6. SRAM、EEPROM 与 Flash

MSA 当前映射通常位于 SRAM；模块的默认值或持久化数据可能位于 EEPROM 和
Flash。不同硬件设计的存储布局不同，例如部分模块没有外部 EEPROM，而使用
MCU 内部 EEPROM。

因此：

- 写 MSA 后立即读回相等，只证明当前映射/SRAM 中的数据可见；
- 不能据此声称 EEPROM 或 Flash 已更新；
- EEPROM→Flash 或 Flash→EEPROM 同步是独立操作；
- 属性机制可能决定哪些字段允许写入持久化区域；
- 断电重启后的保持性需要硬件测试。

OpenCMIS UI 应使用“写入并回读验证”措辞，不使用“保存到 EEPROM”或“持久化
成功”，除非执行了明确的同步命令并通过掉电/重启测试。

## 7. Memory Map、数据文件与 Mask

- Memory Map 是与特定 FW 版本对应的地址/类型/长度定义；不同 FW 可能共享
  或改变 map。
- EEPROM file 常用于更新 MSA，常见格式为 `.txt`。
- Setup file 常用于更新 HCI，常见格式为 `.csv` 或 `.txt`。
- Mask file 用于屏蔽动态 bit/byte；写入或校验时只处理未屏蔽部分。
- Setup file 必须对应明确的 Memory Map 版本。

未来导入文件功能必须记录 FW/Memory Map 兼容性，并在比较、写入和回读验证时
应用 bit-level mask，不能只按整字节比较。

## 8. I2C 适配器经验

培训材料列出的实现包括：

- EUI3 / FSB：Cypress CY7C68013A（USB 2.0、I2C、UART）；
- MIB：Lattice FPGA；
- Scott Board：STM32；
- 商用或低成本方案：Raspberry Pi Zero、Arduino；
- FintestXMLRPC：通过网络访问 Partest MIB 的特殊适配器。

这些差异支持 OpenCMIS 当前的端口/适配器架构：核心协议依赖
`II2cRegisterBus`，具体 USB、串口或网络适配器在外层实现，不把硬件类型写死
在 MSA/HCI 服务中。

## 9. 对 OpenCMIS 的直接约束

1. 核心统一使用 7-bit I2C 地址，硬件边界负责 8-bit 转换。
2. Bank Select、Page Select 和数据传输必须原子执行。
3. HCI 必须先写 vendor password，再进入 `0x7F` page 并执行命令。
4. HCI status polling 必须有 timeout、cancellation 和可测试的 ready values。
5. MSA 与 HCI 必须共享同一个模块会话同步边界。
6. MSA UI 的 Bank/Page 选择必须传到核心 API，不能只是显示字段。
7. 写入后自动回读只标记 `Read-back verified`，不标记持久化成功。
8. 没有硬件时只报告模拟测试；适配器发现、实际读写、时序与掉电保持性均为
   未验证。
