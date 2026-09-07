using System;
using System.Collections.Generic;

namespace ScadaHmi.Comm
{
    // 插座：所有通信协议驱动都必须长这个形状
    public interface ICommDriver
    {
        // 我是谁，比如 "Mock"、"ModbusTcp"
        string Name { get; }
        // 现在连着吗
        bool Isconnected { get; }
        // 连接设备
        bool Connect();
        // 断开
        bool Disconnect();
        // 读一批数据（点位名 → 值）   点位名是设备的标签名，值是设备的值  字典<键，值>
        Dictionary<string,double> Read();
        // 往设备写一个值
        void Write(string tag, double value);
    }
}
