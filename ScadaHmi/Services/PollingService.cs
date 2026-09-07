using System;
using System.Collections.Generic;
using ScadaHmi.Comm;

namespace ScadaHmi.Services
{
    public class PollingService
    {
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

            foreach (var kvp in data)
            {
                Console.WriteLine($"{kvp.Key}: {kvp.Value}");
            }
        }


    }
}
