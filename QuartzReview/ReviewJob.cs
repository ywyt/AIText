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

namespace QuartzReview
{
    [DisallowConcurrentExecution]
    public class ReviewJob : IJob
    {
        static Random random = new Random();
        static bool isRunning = false;
        public static bool IsRunning { get {return isRunning;} }
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        TimeSpan MAX_LONG_TIMEOUT = TimeSpan.FromMinutes(10);

        /// <summary>
        /// 控制同时执行调用接口的数量
        /// </summary>
        public const int MAX_API_COUNT = 2;

        public async Task Execute(IJobExecutionContext context)
        {
            logger.Info($"任务开始执行时间: {DateTime.Now}");
            if (isRunning)
            {
                // 这种情况不应该结束，当前时段任务要继续完成
                logger.Warn("上一个任务未结束");
                await Task.CompletedTask;
                return;
            }

            // 初始化参数
            isRunning = true;

            // 业务逻辑
            var Db = SqlSugarHelper.InitDB();

            await InvokeApi.InitReview(Db);

            var siteList = await Db.Queryable<SiteAccount>().Where(o => o.IsEnable == true && o.StartDate <= DateTime.Now).ToListAsync();

            // 控制同时执行调用接口的数量的信号量
            var semaphore = new SemaphoreSlim(MAX_API_COUNT);

            DateTime currentDate = DateTime.Now.Date;
            int currentHour = DateTime.Now.Hour;

            foreach (var item in siteList)
            {
                logger.Info($"当前处理的是{item.Site}");
                if (string.IsNullOrEmpty(item.WcKey) || string.IsNullOrEmpty(item.WcSecret))
                {
                    logger.Info($"没有配置WooCommerce授权：{item.Site}");
                    continue;
                }
                // 今日执行
                var sendReviews = await Db.Queryable<SendReview>().Where(o => o.SyncSiteId == item.Id && o.CreateTime >= currentDate).ToListAsync();

                // 待发送，继续完成
                var sendings = sendReviews.Where(o => o.IsSync == false).ToList();
                if (sendings.Count > 0)
                {
                    logger.Info($"{item.Site}未完成的{sendings.Count}条");
                    // 未完成的继续处理
                    foreach (var sending in sendings)
                    {
                        logger.Info($"{item.Site}未完成的记录{sending.Id}");
                        await semaphore.WaitAsync();
                        _ = Task.Run(async () =>
                        {
                            // 为每个并发操作创建独立的 SqlSugar 实例
                            var Db1 = SqlSugarHelper.InitDB();
                            var start = DateTime.Now;
                            await Task.WhenAny(Send(Db1, item, sending), Task.Delay(MAX_LONG_TIMEOUT));
                            if (DateTime.Now - start >= MAX_LONG_TIMEOUT)
                            {
                                logger.Info($"{item.Site}任务超时，放弃处理");
                            }
                            Db1.Dispose();
                            semaphore.Release();
                        });
                    }
                    // 等待以免API调用间隔过短
                    await Task.Delay(1000);
                }

                var sentCount = sendReviews.Where(o => o.IsSync == true).Count();
                // 今天已发送完毕
                if (sentCount >= 1000)
                {
                    logger.Info($"{item.Site}今日任务已达成");
                    continue;
                }

                // TODO: 一定的概率发评论
                //if (random.Next() < 0.2)
                //{
                //    continue;
                //}

                // 已发布（publish）且评论最少的产品，/10 可以使得评论数有所差异
                var product = await Db.Queryable<SiteProduct>().Where(o => o.SiteId == item.Id && o.status == "publish").OrderBy(o => SqlFunc.ToInt32(o.ReviewsCount / 10)).OrderBy(o => SqlFunc.GetRandom()).FirstAsync();
                if (product == null)
                {
                    logger.Info($"{item.Site}没有产品");
                    continue;
                }

                // 创建新的，当前循环应该执行的
                var record = await InvokeApi.CreateReview(Db, item, product);
                if (record.status && record.value != null)
                {
                    await semaphore.WaitAsync();
                    _ = Task.Run(async () =>
                    {   // 为每个并发操作创建独立的 SqlSugar 实例
                        var Db1 = SqlSugarHelper.InitDB();
                        var start = DateTime.Now;
                        await Task.WhenAny(Send(Db1, item, record.value), Task.Delay(MAX_LONG_TIMEOUT));
                        if (DateTime.Now - start >= MAX_LONG_TIMEOUT)
                        {
                            logger.Info($"{item.Site}任务超时，放弃处理");
                        }
                        Db1.Dispose();
                        semaphore.Release();
                    });
                }
                // 等待以免API调用间隔过短
                await Task.Delay(1000);
            }
            int semaNum = 0;
            while (true)
            {
                logger.Info($"等待信号量释放， 等待任务结束{semaNum}/{MAX_API_COUNT}");
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

        /// <summary>
        /// 处理发送
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="site"></param>
        /// <param name="sendReview"></param>
        private static async Task<ReturnValue<string>> Send(SqlSugarClient Db, SiteAccount site, SendReview sendReview)
        {
            // 没有评论
            if (string.IsNullOrEmpty(sendReview.Content))
            {
                var rvAi = await InvokeApi.DrawAIReview(Db, sendReview);
                if (rvAi.status == false)
                {
                    logger.Info("没有评论" + rvAi.errorsimple);
                    return rvAi;
                }
            }

            // 最后同步到站点
            if (site.SiteType == SiteType.WordPress)
            {
                var rvSync = await InvokeApi.DoSyncReview(Db, sendReview);
                return rvSync;
            }
            else
            {
                return new ReturnValue<string>() { errorsimple = $"{site.Site}是未实现的类型{site.SiteType}" };
            }
        }
    }
}
