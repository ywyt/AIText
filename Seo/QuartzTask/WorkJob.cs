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
        public static bool IsRunning { get {return isRunning;} }
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        TimeSpan MAX_LONG_TIMEOUT = TimeSpan.FromMinutes(10);

        /// <summary>
        /// 控制同时执行调用接口的数量
        /// </summary>
        public const int MAX_API_COUNT = 4;

        public async Task Execute(IJobExecutionContext context)
        {
            logger.Info($"任务开始执行时间: {DateTime.Now}");
            if (isRunning)
            {
                // 这种情况不应该结束，当前时段任务要继续完成
                logger.Warn("上一个任务未结束");
                return;
            }

            // 初始化参数
            isRunning = true;

            // 业务逻辑
            var Db = SqlSugarHelper.InitDB();

            InvokeApi.Init(Db);

            var siteList = await Db.Queryable<SiteAccount>().Where(o => o.IsEnable == true && o.StartDate <= DateTime.Now).ToListAsync();

            // 控制同时执行调用接口的数量的信号量
            var semaphore = new SemaphoreSlim(MAX_API_COUNT);

            DateTime currentDate = DateTime.Now.Date;
            int currentHour = DateTime.Now.Hour;

            foreach (var item in siteList)
            {
                logger.Info($"当前处理的是{item.Site}");
                if (item.CountPerDay > 24)
                {
                    logger.Info($"异常的配置：{item.Site}");
                    continue;
                }
                // 今日执行
                var sendRecords = await Db.Queryable<SendRecord>().Where(o => o.SyncSiteId == item.Id && o.CreateTime >= currentDate).ToListAsync();

                // 待发送，继续完成
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

                var sentCount = sendRecords.Where(o => o.IsSync == true).Count();
                // 今天已发送完毕
                if (sentCount >= item.CountPerDay)
                {
                    logger.Info($"{item.Site}今日任务已达成");
                    continue;
                }

                // 判断当前小时是否在执行时间列表中
                if (string.IsNullOrEmpty(item.Hours)) continue;

                // 计算出执行时间小时段
                int[] hours = item.Hours
                                .Split(',')                // 按逗号分割字符串
                                .Select(int.Parse)         // 将每个字符串转换为整数
                                .ToArray();                // 转换成数组

                if (hours.Length > 0 && !hours.Contains(currentHour))
                {
                    logger.Info($"{item.Site}不在执行时间范围{item.Hours}内");
                    // 由于异常等原因，时间都过了，之前的还没执行（个数没达到要求），这时候要补上
                    if (hours.Where(o => o < currentHour).Count() > sendRecords.Where(o => o.CreateTime.Hour < currentHour).Count())
                    {
                        logger.Info($"{item.Site}由于之前时段的任务未达成，需要补上任务");
                    }
                    // 否则跳过
                    else
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

            // 验证可否发送
            if (!string.IsNullOrEmpty(sendRecord.Content) && !string.IsNullOrEmpty(sendRecord.Title) 
                && double.TryParse(sendRecord.Score, out var score) && score > 7.5)
            {
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
            else
            {
                string errMsg = $"{site.Site}记录{sendRecord.Id}异常，未执行文章发布。文章标题{sendRecord.Title} 评分{sendRecord.Score}";
                logger.Error(errMsg);
                return new ReturnValue<string>() { errorsimple = errMsg };
            }
        }

    }
}
