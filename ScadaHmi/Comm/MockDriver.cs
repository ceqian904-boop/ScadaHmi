using System;
using System.Collections.Generic;

namespace ScadaHmi.Comm
{
    public class MockDriver : ICommDriver
    {
        //MockDriver 是一个假驱动，用来模拟真实设备。真实设备的温度是不断波动的，所以你需要一个随机数生成器。
        private readonly Random _r = new();
        //谁继承借口，就必须实现接口里的所有方法和属性
        public string Name
        {
            get { return "Mock"; }
        }

        public bool Isconnected { get; private set; }//自动属性，外部只能读，不能写

        public bool Connect()
        {
            Isconnected = true;
            return true;
        }

        public bool Disconnect()=> Isconnected = false;

        public Dictionary<string, double> Read()
        {
            return new Dictionary<string, double>
            {
                ["温度"] = 20 + _r.NextDouble() * 10, // 20~30
                ["压力"] = 1 + _r.NextDouble() * 0.5, // 1~1.5
                ["流量"] = 100 + _r.NextDouble() * 50 // 100~150
            };
        }

        public void Write(string tag, double value)
        {
            Console.WriteLine($"[Mock] 写入 {tag} = {value}");
        }

    }
}
    