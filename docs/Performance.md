# 性能指标记录

> 规则：**每天收工记 1~2 个数字**（延迟 / 耗时 / 内存 / 异常数）。
> 目的：10-09 写简历时这些数字直接抄，不留数字简历只能写"实现了 XX 功能"，没人看。

---

## 09-14 单设备轮询延迟

- 场景：1 台虚拟从站 / 轮询周期 1000ms / 读 3 个保持寄存器（地址 0/1/2，Scale=10）
- 方法：`Stopwatch` 包住 `ReadAsync()`，100 次采样取平均
- 结果：**平均 0.27 ms，最大 0.75 ms**
- 补充：最小 0.09 ms，P95 0.43 ms，标准差 0.09 ms
- 备注：09-18 补测（当天没记数）。本地回环 127.0.0.1，真实网络还要加上 RTT 与交换机转发，现场量级通常是 1~10 ms。复现方式：仓库外临时工具（手写 Modbus TCP 虚拟从站 + Stopwatch 包 Read），链路与 `ModbusTcpDriver` 完全一致。

---

## 09-17 分层前后编译时间对比

- 场景：`ScadaHmi-Git.slnx`，4 个项目：`Communication` / `Data` / `Services` / `ScadaHmi`
- 方法：`dotnet clean && time dotnet build`（全量）；`touch ScadaHmi.Communication/ModbusTcpDriver.cs` 后 `time dotnet build`（增量）
- 结果：
  - 全量重编（分层前每次改动的代价）：**12.34 s**
  - 只改 Communication 后重编（分层后）：**5.60 s**，缩短约 **55%**
- 补充：只 build `Communication` 单项目 2.78 s（缩短约 77%）；无改动空跑增量 2.15 s
- 备注：Win11 + .NET SDK 10.0.400，Debug 配置，同一台机器对比才有意义。分层后改一个文件只重编 Communication 及其两个依赖项目，Data 被跳过——这就是分层的实际收益。
