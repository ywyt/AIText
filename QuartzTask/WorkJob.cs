using Entitys;
using NLog;
using Quartz;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Work;

namespace QuartzTask
{
    public class WorkJob : IJob
    {
        static bool isRunning = false;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        /// <summary>
        /// 控制同时执行调用接口的数量
        /// </summary>
        public const int MAX_API_COUNT = 4;

        public async Task Execute(IJobExecutionContext context)
        {
            if (isRunning)
            {
                logger.Warn("上一个任务执行中");
                return;
            }
            logger.Info($"任务开始执行时间: {DateTime.Now}");

            // 初始化参数
            isRunning = true;

            // 业务逻辑
            var Db = SqlSugarHelper.InitDB();

            InvokeApi.Init(Db);

            var siteList = Db.Queryable<SiteAccount>().Where(o => o.IsEnable == true && o.StartDate <= DateTime.Now).ToList();

            // 控制同时执行调用接口的数量的信号量
            var semaphore = new SemaphoreSlim(MAX_API_COUNT);

            foreach (var item in siteList)
            {
                if (item.CountPerDay > 24)
                {
                    logger.Info($"异常的配置：{item.Site}");
                    continue;
                }
                // 今日执行
                var sendRecords = Db.Queryable<SendRecord>().Where(o => o.SyncSiteId == item.Id && o.CreateTime >= DateTime.Now.Date).ToList();
                var sentCount = sendRecords.Where(o => o.IsSync == true).Count();
                // 今天已发送完毕
                if (sentCount >= item.CountPerDay)
                {
                    logger.Info($"{item.Site}今日任务已达成");
                    continue;
                }
                // 待发送
                var sendings = sendRecords.Where(o => o.IsSync == false).ToList();
                if (sendings.Count > 0)
                {
                    logger.Info($"{item.Site}未完成的{sendings.Count}条");
                    // 未完成的继续处理
                    foreach (var sending in sendings)
                    {
                        await semaphore.WaitAsync();
                        _ = Task.Run(async () =>
                        {
                            await Send(Db, item, sending);
                            semaphore.Release();
                        });
                    }
                    // 等待以免API调用间隔过短
                    await Task.Delay(1000);
                }

                // 判断当前小时是否在执行时间列表中
                if (!string.IsNullOrEmpty(item.Hours) && !($",{item.Hours},").Contains($",{DateTime.Now.Hour},"))
                {
                    logger.Info($"{item.Site}不在执行时间范围{item.Hours}内");
                    continue;
                }

                if (sendRecords.Any(o => o.CreateTime.Hour == DateTime.Now.Hour))
                {
                    logger.Info($"{item.Site}当前小时已存在任务");
                    continue;
                }

                // 创建新的，当前循环应该执行的
                var record = await InvokeApi.CreateRecord(Db, item);
                if (record.status && record.value != null)
                {
                    await semaphore.WaitAsync();
                    _ = Task.Run(async () =>
                    {
                        await Send(Db, item, record.value);
                        semaphore.Release();
                    });
                }
                // 等待以免API调用间隔过短
                await Task.Delay(1000);
            }
            int semaNum = 0;
            while (true)
            {
                logger.Debug($"等待信号量释放， 等待任务结束{semaNum}/{MAX_API_COUNT}");
                await semaphore.WaitAsync();
                semaNum++;
                // 等待信号量释放完毕
                if (semaNum == MAX_API_COUNT)
                {
                    semaphore.Release(MAX_API_COUNT);
                    break;
                }
                await Task.Delay(1000);
            }
            logger.Info($"任务结束执行时间: {DateTime.Now}");
            isRunning = false;
            await Task.CompletedTask;
        }

        //// 计算每天要执行的小时数（动态计算方法）
        //private static List<int> CalculateExecutionHours(int count)
        //{
        //    List<int> hours = new List<int>();
        //    double interval = 24.0 / count; // 计算间隔时间

        //    for (int i = 0; i < count; i++)
        //    {
        //        int hour = (int)Math.Round(i * interval) % 24; // 确保小时数在 0-23 之间
        //        hours.Add(hour);
        //    }

        //    return hours;
        //}

        /// <summary>
        /// 处理发送
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="site"></param>
        /// <param name="sendRecord"></param>
        private static async Task<ReturnValue<string>> Send(SqlSugarClient Db, SiteAccount site, SendRecord sendRecord)
        {
            //  没有图片时选择图片
            if (string.IsNullOrEmpty(sendRecord.ImgUrl) && string.IsNullOrEmpty(sendRecord.ImgPath))
            {
                //var rvDraw = await InvokeApi.DoDraw(Db, sendRecord);
                //if (rvDraw.status == false)
                //{
                //    logger.Info("画图出错" + rvDraw.errorsimple);
                //    return rvDraw;
                //}
            }

            // 没有生成文章
            if (string.IsNullOrEmpty(sendRecord.Content))
            {
                var rvAi = await InvokeApi.DoAI(Db, sendRecord);
                if (rvAi.status == false)
                {
                    logger.Info("生成文章出错" + rvAi.errorsimple);
                    return rvAi;
                }
            }

            // 最后同步到站点
            if (site.SiteType == SiteType.WordPress)
            {
                var rvSync = await InvokeApi.DoSync(Db, sendRecord);
                return rvSync;
            }
            else
            {
                return new ReturnValue<string>() { errorsimple = $"{site.Site}是未实现的类型{site.SiteType}" };
            }

        }

    }
}
