using FixedThreadPool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TestApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var pool = new FixedThreadPool.FixedThreadPool(5);

            for (var i = 0; i < 100; i++)
            {
                pool.Execute(new MessageWriterTask() { Message = "High" }, Priority.HIGH);
                pool.Execute(new MessageWriterTask() { Message = "Normal" }, Priority.NORMAL);
                pool.Execute(new MessageWriterTask() { Message = "Low" }, Priority.LOW); 
            }

            pool.Execute(new MessageWriterTask() { Message = "Last Low" }, Priority.LOW); 
            pool.Stop();

            pool.Execute(new MessageWriterTask() { Message = "Task after stop" }, Priority.LOW); 
        }

        class MessageWriterTask : ITask
        {
            public string Message { get; set; }

            public void Execute()
            {
                Console.WriteLine(Message);
                Thread.Sleep(100);
            }
        }

    }
}
