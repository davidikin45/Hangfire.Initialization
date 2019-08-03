using System;
using System.Collections.Generic;
using System.Text;

namespace ConsoleApp
{
    public class HangfireJob
    {
        public void Execute()
        {
            Console.WriteLine("Hangfire job executed");
        }
    }
}
