using System;
using System.Threading;
using System.Threading.Tasks;

namespace testScheduler
{
    public class TestTask : IScheduledTask
    {
        public string Schedule => "* * * * *";

        public Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($" 1 min - {DateTime.Now}");
            
            return Task.CompletedTask;
        }
    }
    
    public class TestTask2 : IScheduledTask
    {
        public string Schedule => "*/2 * * * *";

        public Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine($" 2 min - {DateTime.Now}");
            
            return Task.CompletedTask;
        }
    }
}