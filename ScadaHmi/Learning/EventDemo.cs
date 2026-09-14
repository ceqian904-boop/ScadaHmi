using System;

namespace ScadaHmi.Learning
{
    // 发布者：广播站
    public class Broadcaster
    {
        // Action<string> 是一个现成委托：接收 string 参数，无返回值
        // 可空 ?：没人订阅时事件就是 null，这是正常状态，不是错误
        public event Action<string>? OnMessage;   // 事件：广播铃

        public void Broadcast(string msg)
        {
            // 触发事件！?. 表示"如果有人订阅才广播"，没人订阅就不报错
            OnMessage?.Invoke(msg);
        }
    }

    // 这个类的作用：演示一下事件怎么用（不放进正式流程）
    public static class EventDemo
    {
        public static void Run()
        {
            var b = new Broadcaster();          // 建广播站

            // 订阅者 A：控制台
            b.OnMessage += m => Console.WriteLine($"[订阅者A] 收到: {m}");
            // 订阅者 B：还是控制台，但换个说法
            b.OnMessage += m => Console.WriteLine($"[订阅者B] 收到: {m}");

            b.Broadcast("大家好，数据到了！");    // 广播一声
        }
    }
}
