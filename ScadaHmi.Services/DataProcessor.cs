using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace ScadaHmi.Services
{
    /// <summary>
    /// 【消费者】从管道里往外拿数据，一条一条处理。
    /// 它只认识 ChannelReader —— 不认识驱动、不认识窗口，所以好测。
    /// </summary>
    public class DataProcessor
    {
        private readonly ChannelReader<Dictionary<string, double>> _reader;
        private readonly Action<Dictionary<string, double>> _onData;

        public DataProcessor(ChannelReader<Dictionary<string, double>> reader,
                             Action<Dictionary<string, double>> onData)
        {
            _reader = reader;
            _onData = onData;
        }

        public async Task RunAsync(CancellationToken ct)
        {
            try
            {
                // await foreach：来一条处理一条；管道 Complete 后自动结束。
                await foreach (var data in _reader.ReadAllAsync(ct))
                {
                    _onData(data);
                }
            }
            catch (OperationCanceledException)
            {
                // 收到停车信号，正常退出
            }
        }
    }
}