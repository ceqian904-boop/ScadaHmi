using System;
using System.Collections.Generic;
using ScadaHmi.Comm;

namespace ScadaHmi.Services
{
    public class PollingService
    {
        //定义一个委托(相当于广播站，定义播放什么内容)
        public delegate void DataReceivedHandler(Dictionary<string, double> data);
        //定义一个事件(相当于广播铃，广播站播放内容时，铃响)
        public event DataReceivedHandler DataReceived;

        //public event Action<Dictionary<string, double>> DataReceived;      上面两句合并

        private readonly ICommDriver _driver;

        public PollingService(ICommDriver driver)
        {
            _driver = driver;//依赖注入，传入一个通信驱动,穿什么用什么
        }

        public void PollOnce()
        {
            if (!_driver.Isconnected)
            {
                _driver.Connect();
            }

            Dictionary<string, double> data = _driver.Read();

            DataReceived?.Invoke(data); //触发事件，通知订阅者数据已到达
        }


    }
}
