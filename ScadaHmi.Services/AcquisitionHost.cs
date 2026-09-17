using ScadaHmi.Communication;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;

namespace ScadaHmi.Services
//点【启动】到点【停止】之间发生的所有事,它全包了。
{
    public class AcquisitionHost : IDisposable
    {
        //一次采集会话的生命周期管理：启动、停止、状态。
        // 创建一个实例 = 一套全新的管道；Stop 之后可以再 Start（内部会重建）
        private readonly ICommDriver _driver;//数据从哪来
        private readonly Action<Dictionary<string, double>> _onData;//数据往哪去
        private readonly int _channelCapacity;//中间能攒多少
        private CancellationTokenSource? _cts; //_cts = 新建取消开关
        private Channel<Dictionary<string, double>>? _channel; //_channel = 新建传送带
        private Task? _producerTask;  //启动采集循环（把数据放上传送带）
        private Task? _consumerTask;   //启动处理循环（从传送带取数据）
        private bool _disposed;

        public bool IsRunning { get; private set; }
        public AcquisitionHost(ICommDriver driver,Action<Dictionary<string,double>>
            onData,int channelCapacity = 100)
        {
            _driver = driver;
            _onData = onData;
            _channelCapacity = channelCapacity;
        }

        public Task StartAsync()  //开始
        {
            // ① 防御：Dispose 过就别再启
            //.NET 自带的异常类型：“你已经 Dispose 了这个对象，别再用了”
            ObjectDisposedException.ThrowIf(_disposed, this);


            //方法签名声明了返回 Task，但你里面没干异步活时，
            //总不能返回 null 吧（调用方 await null 直接炸）。这时候就掏它
            if (IsRunning) return Task.CompletedTask;


            // ③ 每次启动都建一套全新的：CTS 和 Channel 都是一次性的
            var cts = new CancellationTokenSource();

            var channel = Channel.CreateBounded<Dictionary<string, double>>
                (new BoundedChannelOptions(_channelCapacity)
                {
                    FullMode = BoundedChannelFullMode.Wait,//传送带满了，生产者就地等，不丢数据。
                    SingleReader = true,//只有一个消费者，让通道放心走性能优化捷径。
                    SingleWriter = false//生产者可能不止一个，按多写者保守处理。
                }
                );
            _cts = cts;
            _channel = channel;

            // ④ 建两个服务实例，把管道两头分别递进去
            var producer = new AcquisitionService(_driver,channel.Writer);
            var consumer = new DataProcessor(channel.Reader,_onData);
            // ⑤ 两个循环都丢后台，别堵住调用方（UI 线程）
            _producerTask = Task.Run(() => producer.RunAsync(_cts.Token));
            _consumerTask = Task.Run(() => consumer.RunAsync(_cts.Token));

            IsRunning = true;

            return Task.CompletedTask;
        }
        public async Task StopAsync()   //停止
        {
            if (!IsRunning) return;
            _cts?.Cancel();

            // 等两个循环收尾。
            // 生产者退出时会 Complete 管道，消费者才会从 await foreach 里出来 ——
            // 所以这里的等待顺序由它们自己保证，我们只需等全部完成。
            try
            {
                //把生产和消费放到一个数组里面不用一个一个的await
                var pending = new[] { _producerTask, _consumerTask }
                //筛掉 null  null 不能 await，先踢出去
                .Where(t => t is not null)
                //
                .Select(t => t!)
                //把 LINQ 的懒查询真正执行，落地成数组
                .ToArray();
                //等数组里所有任务一起跑完，而不是一个一个排队等
                if (pending.Length > 0)
                    await Task.WhenAll(pending);
            }
            catch (OperationCanceledException)
            {
                //OperationCanceledException任务被取消”专用异常
                // 取消是预期行为，不是错误
            }
            finally {
                _cts?.Cancel();
                _cts = null;
                _channel = null;
                _producerTask = null;
                _consumerTask = null;
                IsRunning = false;
            }


        }
        public void Dispose() => throw new NotImplementedException(); // 销毁
    }
}
