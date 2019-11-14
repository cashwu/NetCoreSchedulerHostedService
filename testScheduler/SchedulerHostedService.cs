using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using NCrontab;

namespace testScheduler
{
    public class SchedulerHostedService : BackgroundService
    {
        private readonly List<ScheduledTaskWrapper> _scheduledTasks = new List<ScheduledTaskWrapper>();

        public event EventHandler<UnobservedTaskExceptionEventArgs> UnobservedTaskException;

        public SchedulerHostedService(IEnumerable<IScheduledTask> scheduledTasks)
        {
            var referenceTime = DateTime.UtcNow;

            foreach (var scheduledTask in scheduledTasks)
            {
                var scheduledTaskWrapper = new ScheduledTaskWrapper
                {
                    Schedule = CrontabSchedule.Parse(scheduledTask.Schedule),
                    Task = scheduledTask,
                };
                
                scheduledTaskWrapper.NextRuntTime = scheduledTaskWrapper.Schedule.GetNextOccurrence(referenceTime);

                Console.WriteLine($" ctor - {scheduledTaskWrapper.NextRuntTime} - {scheduledTaskWrapper.Schedule} ");

                _scheduledTasks.Add(scheduledTaskWrapper);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Console.WriteLine($"Execute Task - while loop");
                await ExecuteOnceAsync(stoppingToken);

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }

        private async Task ExecuteOnceAsync(CancellationToken stoppingToken)
        {
            var taskFactory = new TaskFactory(TaskScheduler.Current);
            var referenceTime = DateTime.UtcNow;

            var tasksThatShouldRun = _scheduledTasks.Where(a => a.ShouldRun(referenceTime)).ToList();

            foreach (var taskThatShouldRun in tasksThatShouldRun)
            {
                taskThatShouldRun.Increment();

                await taskFactory.StartNew(async () =>
                {
                    try
                    {
                        Console.WriteLine($"Execute Task - {DateTime.Now}");
                        await taskThatShouldRun.Task.ExecuteAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        var args = new UnobservedTaskExceptionEventArgs(ex as AggregateException ?? new AggregateException(ex));

                        UnobservedTaskException?.Invoke(this, args);

                        if (!args.Observed)
                        {
                            throw;
                        }
                    }
                }, stoppingToken);
            }
        }
    }

    internal class ScheduledTaskWrapper
    {
        public CrontabSchedule Schedule { get; set; }

        public IScheduledTask Task { get; set; }

        public DateTime NextRuntTime { get; set; }

        public DateTime LastRunTime { get; set; }

        public void Increment()
        {
            LastRunTime = NextRuntTime;
            NextRuntTime = Schedule.GetNextOccurrence(NextRuntTime);
        }

        public bool ShouldRun(DateTime currentTime)
        {
            return NextRuntTime < currentTime && LastRunTime != NextRuntTime;
        }
    }

    public interface IScheduledTask
    {
        string Schedule { get; }

        Task ExecuteAsync(CancellationToken stoppingToken);
    }
}