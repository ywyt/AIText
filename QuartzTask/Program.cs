using NLog;
using Quartz;
using Quartz.Impl;
using System;
using System.IO;
using System.Reflection.Metadata;
using System.Threading.Tasks;

namespace QuartzTask
{
    internal class Program
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        static async Task Main(string[] args)
        {
            string rootPath = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            var path = Path.Combine(rootPath, "config.txt");
            SqlSugarHelper.ConnectionString = System.IO.File.ReadAllText(path);

            // 1. 创建调度器工厂
            StdSchedulerFactory factory = new StdSchedulerFactory();
            IScheduler scheduler = await factory.GetScheduler();

            // 2. 启动调度器
            await scheduler.Start();

            // 3. 定义一个 Job
            IJobDetail job = JobBuilder.Create<WorkJob>()
                .WithIdentity("WorkJob", "group1") // 任务名称和分组
                .Build();

            // 4. 创建一个触发器 (基于 Cron 表达式)
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity("myTrigger", "group1") // 触发器名称和分组
                .WithCronSchedule("0 14 0/1 * * ?") // 每小时的第 15 分钟触发一次
                .Build();

            // 5. 将任务和触发器加入调度器
            await scheduler.ScheduleJob(job, trigger);

            logger.Info("任务调度已启动...");
            while(true)
                Console.ReadLine(); // 防止程序立即退出
        }
    }
}
