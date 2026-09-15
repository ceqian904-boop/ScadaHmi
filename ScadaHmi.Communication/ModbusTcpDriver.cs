using NModbus;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaHmi.Communication
{
    public class ModbusTcpDriver : ICommDriver
    {
        private readonly string _ip;
        private readonly int _port;
        private readonly byte _slaveId;
        private readonly List<ModbusPoint> _points; //点位表

        // 两个都可空：没 Connect 的时候是 null
        private TcpClient? _tcp;                 //TcpClient 就是负责“拨号、接通并说话”的 客户端程序
        private IModbusMaster? _master;         //通信的核心接口

        /// <summary>最近一次的错误信息，供上层做日志</summary>
        public string? LastError { get; private set; }

        public ModbusTcpDriver(string ip, int port, byte slaveId, List<ModbusPoint> points)
        {
            _ip = ip;
            _port = port;
            _slaveId = slaveId;
            _points = points;
        }

        public string Name => "ModbusTcp";
        public bool Isconnected { get; private set; }

        // --------------------------------------------------------

        public async Task<bool> ConnectAsync(CancellationToken ct)
        {
            if (Isconnected) return true;

            try
            {
                _tcp = new TcpClient();
                // .NET 5+ 支持带 CancellationToken 的重载，超时可控
                await _tcp.ConnectAsync(_ip, _port, ct);

                _master = new ModbusFactory().CreateMaster(_tcp);

                // 超时别写太短：现场网络抖动 1s 很正常
                _master.Transport.ReadTimeout = 1000;
                _master.Transport.WriteTimeout = 1000;

                Isconnected = true;
                LastError = null;
                return true;
            }
            catch (Exception ex)
            {
                LastError = ex.Message;
                await DisconnectAsync();   // 失败要清干净，别留半个连接
                return false;
            }
        }

        public async Task DisconnectAsync()
        {
            // 每个都单独 try/catch：一个失败不能影响另一个
            try { _master?.Dispose(); } catch { /* 忽略 */ }
            _master = null;

            try { _tcp?.Close(); } catch { /* 忽略 */ }
            _tcp = null;

            Isconnected = false;
            await Task.CompletedTask;
        }

        // --------------------------------------------------------

        public async Task<Dictionary<string, double>> ReadAsync(CancellationToken ct)
        {
            if (_master is null || !Isconnected)
                throw new CommException(Name, "读", null, "驱动未连接");

            if (_points.Count == 0)
                return new Dictionary<string, double>();

            // ===== 关键优化：把地址合并成一次连续读 =====
            ushort start = _points.Min(p => p.Address);
            ushort end = _points.Max(p => p.Address);
            ushort count = (ushort)(end - start + 1);

            try
            {
                ushort[] raw = await _master.ReadHoldingRegistersAsync(_slaveId, start, count);
                ct.ThrowIfCancellationRequested();

                var result = new Dictionary<string, double>(_points.Count);
                foreach (var p in _points)
                {
                    // raw 数组是从 start 开始的，所以下标要减掉偏移
                    ushort v = raw[p.Address - start];
                    result[p.Tag] = v / p.Scale;   // 标度换算：253 ÷ 10 = 25.3
                }
                return result;
            }
            catch (SlaveException ex)
            {
                // 从站返回的协议级异常，带 Modbus 异常码
                byte code = (byte)ex.SlaveExceptionCode;
                throw new CommException(Name, "读", null,
                    ModbusErrorText.Translate(code), ex);
            }
            catch (Exception ex) when (ex is not CommException)
            {
                // 网络层异常 → 视为断开，让上层触发重连
                Isconnected = false;
                LastError = ex.Message;
                throw new CommException(Name, "读", null, $"读取失败：{ex.Message}", ex);
            }
        }

        // --------------------------------------------------------

        public async Task WriteAsync(string tag, double value, CancellationToken ct)
        {
            if (_master is null || !Isconnected)
                throw new CommException(Name, "写", tag, "驱动未连接");

            var point = _points.FirstOrDefault(x => x.Tag == tag)
                        ?? throw new ArgumentException($"点位表里没有 {tag}", nameof(tag));

            // 反算成寄存器原始值，并夹在 ushort 范围内防溢出
            double scaled = Math.Round(value * point.Scale);
            ushort raw = (ushort)Math.Clamp(scaled, 0, ushort.MaxValue);

            try
            {
                // 功能码 0x06：写单个保持寄存器
                await _master.WriteSingleRegisterAsync(_slaveId, point.Address, raw);
            }
            catch (SlaveException ex)
            {
                byte code = (byte)ex.SlaveExceptionCode;
                throw new CommException(Name, "写", tag,
                    ModbusErrorText.Translate(code), ex);
            }
            catch (Exception ex)
            {
                Isconnected = false;
                LastError = ex.Message;
                throw new CommException(Name, "写", tag, $"写入失败：{ex.Message}", ex);
            }
        }

        // --------------------------------------------------------

        public void Dispose()
        {
            // 同步等待异步清理。驱动销毁场景下可接受。
            DisconnectAsync().GetAwaiter().GetResult();
        }
    }
}
