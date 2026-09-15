// ============================================================
// PollingService —— 轮询服务（Day1 练习版，已对齐 Day3 异步接口）
// ============================================================

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ScadaHmi.Communication;

namespace ScadaHmi.Services
{
    public class PollingService
    {
        //定义一个委托(相当于广播站，定义播放什么内容)
        public delegate void DataReceivedHandler(Dictionary<string, double> data);
        //定义一个事件(相当于广播铃，广播站播放内容时，铃响)
        //可空 ? ：没人订阅时事件就是 null，这不是错误，是正常状态
        public event DataReceivedHandler? DataReceived;

        //public event Action<Dictionary<string, double>> DataReceived;      上面两句合并

        private readonly ICommDriver _driver;

        public PollingService(ICommDriver driver)
        {
            _driver = driver;//依赖注入，传入一个通信驱动,穿什么用什么
        }

        public async Task PollOnceAsync(CancellationToken ct)
        {
            if (!_driver.Isconnected)
            {
                // 异步版：连接失败返回 false，不抛异常
                if (!await _driver.ConnectAsync(ct))
                    return;
            }

            Dictionary<string, double> data = await _driver.ReadAsync(ct);

            DataReceived?.Invoke(data); //触发事件，通知订阅者数据已到达
        }
    }
}
