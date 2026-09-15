using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using ScadaHmi.Communication;

namespace ScadaHmi.Services
{
    /// <summary>
    /// 【生产者】后台异步循环读设备，把每次读到的数据丢进管道。
    /// </summary>
    public class AcquisitionService   //数据采集服务
    {
        private readonly ICommDriver _driver;
        private readonly ChannelWriter<Dictionary<string, double>> _writer;

        public AcquisitionService(ICommDriver driver,
                                  ChannelWriter<Dictionary<string, double>> writer)
        {
            _driver = driver;
            _writer = writer;
        }

        public async Task RunAsync(CancellationToken ct)   //异步      CancellationToken意思是停止标志，它不会强制终止线程，而是通过一个“信号”礼貌地通知异步操作
        {
            if (!_driver.Isconnected)
                await _driver.ConnectAsync(ct);

            try
            {
                while (!ct.IsCancellationRequested)       //IsCancellationRequested类似于一个红绿灯，
                {
                    var data = await _driver.ReadAsync(ct);

                    // 写进管道。管道满时这里会"让路"等空位（背压），不是死等。
                    await _writer.WriteAsync(data, ct);

                    // 每秒采一次。ct 取消时这行会抛，直接跳出循环。
                    await Task.Delay(1000, ct);
                }
            }
            catch (OperationCanceledException)
            {
                // 收到停车信号，正常退出，不是错误
            }
            finally
            {
                // 关键：关掉传送带。消费者靠这个信号才知道"不会再有数据了"。
                _writer.Complete();
                await _driver.DisconnectAsync();
            }
        }
    }
}