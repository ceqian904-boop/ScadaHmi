// ============================================================
// MockDriver —— 假驱动（对齐四周清单参考代码 01）
// 用于单元测试和不接设备时跑通整条链路。
// 改造成异步版后，它的存在价值更大 —— 测试时不用真的开网络。
// ============================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaHmi.Communication
{
    public class MockDriver : ICommDriver
    {
        // MockDriver 是一个假驱动，用来模拟真实设备。
        // 真实设备的温度是不断波动的，所以你需要一个随机数生成器。
        private readonly Random _r = new();

        public string Name => "Mock";
        public bool Isconnected { get; private set; }

        public Task<bool> ConnectAsync(CancellationToken ct)
        {
            Isconnected = true;
            return Task.FromResult(true);
        }

        public Task DisconnectAsync()
        {
            Isconnected = false;
            return Task.CompletedTask;
        }

        public Task<Dictionary<string, double>> ReadAsync(CancellationToken ct)
        {
            var data = new Dictionary<string, double>
            {
                ["温度"] = 20 + _r.NextDouble() * 10,  // 20~30
                ["压力"] = 1 + _r.NextDouble() * 0.5,  // 1~1.5
                ["流量"] = 100 + _r.NextDouble() * 50  // 100~150
            };
            return Task.FromResult(data);
        }

        public Task WriteAsync(string tag, double value, CancellationToken ct)
        {
            Console.WriteLine($"[Mock] 写入 {tag} = {value}");
            return Task.CompletedTask;
        }

        public void Dispose() { }
    }
}
