// ============================================================
// 通信核心（对齐四周清单参考代码 01）
//   1. ICommDriver   —— 异步版驱动接口（Day3 定稿）
//   2. ModbusPoint   —— 点位表：点位名 ↔ 寄存器地址 ↔ 标度
//   3. CommException —— 通信异常（带设备名/操作/原始异常）
//   4. ModbusErrorText —— Modbus 异常码翻译
// ============================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaHmi.Communication
{
    /// <summary>
    /// 通信驱动统一接口。
    /// 上层只认这个接口，不认具体协议 —— 换协议等于换一个实现类，业务代码零修改。
    ///
    /// 为什么是异步的：同步阻塞在网络 IO 上会占用线程，8 台设备并发时
    /// 8 个线程全被阻塞，线程池会饥饿。async 在 IO 等待期间把线程还给池子。
    /// </summary>
    public interface ICommDriver : IDisposable
    {
        /// <summary>驱动名，比如 "ModbusTcp"</summary>
        string Name { get; }

        /// <summary>当前是否已连接</summary>
        bool Isconnected { get; }

        /// <summary>连接设备。失败返回 false，不抛异常（让上层决定要不要重试）</summary>
        Task<bool> ConnectAsync(CancellationToken ct);

        /// <summary>
        /// 断开连接。
        /// 注意：这里故意不接收 CancellationToken —— 断开本身就是用来取消别人的，
        /// 再给它一个取消令牌会形成循环依赖。
        /// </summary>
        Task DisconnectAsync();

        /// <summary>读一批数据：点位名 → 工程值</summary>
        Task<Dictionary<string, double>> ReadAsync(CancellationToken ct);

        /// <summary>写一个点位</summary>
        Task WriteAsync(string tag, double value, CancellationToken ct);
    }

    // ------------------------------------------------------------

    /// <summary>
    /// 点位表的一项：点位名 ↔ 保持寄存器地址 ↔ 标度。
    ///
    /// 为什么需要标度：Modbus 保持寄存器只能存 16 位无符号整数（0~65535），
    /// 温度 25.3℃ 存不进去。工业标准做法是放大 10 倍存 253，读出来再除以 10。
    /// Scale = 10 表示"存的时候 ×10，读的时候 ÷10"。
    /// </summary>
    public class ModbusPoint
    {
        public string Tag { get; set; } = "";
        public ushort Address { get; set; }
        public double Scale { get; set; } = 1.0;
    }

    // ------------------------------------------------------------

    /// <summary>
    /// 通信异常。
    /// 关键点：不要 catch 完只 return false —— 那样上层永远不知道
    /// 是网线断了、超时了、还是从站返回了异常码。信息必须带上来。
    /// </summary>
    public class CommException : Exception
    {
        public string DeviceName { get; }
        public string Operation { get; }
        public string? Tag { get; }

        public CommException(string deviceName, string operation, string? tag,
                             string message, Exception? inner = null)
            : base(message, inner)
        {
            DeviceName = deviceName;
            Operation = operation;
            Tag = tag;
        }

        public override string ToString()
            => $"[{DeviceName}] {Operation} {(Tag is null ? "" : $"点位={Tag} ")}失败：{Message}"
               + (InnerException is null ? "" : $"\n  内部异常：{InnerException.Message}");
    }

    /// <summary>Modbus 异常码翻译成人话（面试会问）</summary>
    public static class ModbusErrorText
    {
        public static string Translate(byte code) => code switch
        {
            0x01 => "非法功能码：从站不支持该功能",
            0x02 => "非法数据地址：寄存器地址超出从站范围",
            0x03 => "非法数据值：写入值超出允许范围",
            0x04 => "从站设备故障：从站内部错误",
            0x05 => "确认：从站正在处理，需稍后重试",
            0x06 => "从站忙：需稍后重试",
            0x08 => "存储奇偶校验错：存储器校验失败",
            0x0A => "网关路径不可用",
            0x0B => "网关目标设备无响应",
            _    => $"未知异常码 0x{code:X2}"
        };
    }
}
