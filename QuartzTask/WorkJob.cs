using Entitys;
using NLog;
using Quartz;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Work;

namespace QuartzTask
{
    public class WorkJob : IJob
    {
        static bool isRunning = false;
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        static Random RandomMac = new Random();
        public async Task Execute(IJobExecutionContext context)
        {
            if (isRunning)
            {
                logger.Warn("上一个任务执行中");
                return;
            }
            logger.Info($"任务开始执行时间: {DateTime.Now}");

            isRunning = true;

            // 业务逻辑
            var Db = SqlSugarHelper.InitDB();

            var siteList = Db.Queryable<SiteAccount>().Where(o => o.IsEnable == true && o.StartDate <= DateTime.Now).ToList();

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
                        await Send(Db, item, sending);
                    }
                    // 等待以免API调用间隔过短
                    await Task.Delay(30000);
                }

                // 计算执行时间点（动态计算）
                List<int> executionHours = CalculateExecutionHours(item.CountPerDay);
                // 判断当前小时是否在执行时间列表中
                if (!executionHours.Contains(DateTime.Now.Hour))
                {
                    logger.Info($"{item.Site}不在执行时间范围内");
                    continue;
                }
                // 创建新的，当前循环应该执行的
                var record = CreateRecord(Db, item);
                if (record.status && record.value != null)
                {
                    await Send(Db, item, record.value);
                }
                // 等待以免API调用间隔过短
                await Task.Delay(10000);
            }

            logger.Info($"任务结束执行时间: {DateTime.Now}");
            isRunning = false;
            await Task.CompletedTask;
        }

        // 计算每天要执行的小时数（动态计算方法）
        private static List<int> CalculateExecutionHours(int count)
        {
            List<int> hours = new List<int>();
            double interval = 24.0 / count; // 计算间隔时间

            for (int i = 0; i < count; i++)
            {
                int hour = (int)Math.Round(i * interval) % 24; // 确保小时数在 0-23 之间
                hours.Add(hour);
            }

            return hours;
        }

        /// <summary>
        /// 创建记录
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="site"></param>
        /// <param name="sendRecord"></param>
        private ReturnValue<SendRecord> CreateRecord(SqlSugarClient Db, SiteAccount setting)
        {
            var rv = new ReturnValue<SendRecord>();
            if (setting == null)
                return rv;

            // 没使用过的，最少使用量的关键词
            var siteKeyword = Db.Queryable<SiteKeyword>().Where(o => o.SiteId == setting.Id).OrderBy(o => o.UseCount).First();

            if (siteKeyword == null)
            {
                rv.False("没有设置站点关键词");
                return rv;
            }

            // 抽取指令
            var promptTempList = Db.Queryable<PromptTemplate>().ToList();
            var promptTemp = promptTempList[RandomMac.Next(0, promptTempList.Count - 1)];

            var urlList = Db.Queryable<SiteKeyword>().Where(o => o.SiteId == setting.Id)
                .Select(o => o.URL)
                .OrderBy(o => SqlFunc.GetRandom()).Take(3).ToList();

            var urlString = string.Join(",", urlList);

            SendRecord sendRecord = new SendRecord
            {
                Link = urlString,
                KeywordId = siteKeyword.Id,
                Keyword = siteKeyword.Keyword,
                TemplateId = promptTemp.Id,
                TemplateName = promptTemp.Name,
                IsSync = false,
                SyncSiteId = setting.Id,
                SyncSite = setting.Site,
                SyncTime = null,
                CreateTime = DateTime.Now,
                UpdateTime = null
            };
            var ret = Db.Insertable(sendRecord).ExecuteCommand();
            rv.status = ret > 0;
            if (rv.status)
            {
                rv.value = sendRecord;
                Db.Updateable<SiteKeyword>().SetColumns(o => o.UseCount == (o.UseCount + 1)).Where(o => o.Id == siteKeyword.Id).ExecuteCommand();
            }
            return rv;

        }

        /// <summary>
        /// 处理发送
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="site"></param>
        /// <param name="sendRecord"></param>
        private static async Task<ReturnValue<string>> Send(SqlSugarClient Db, SiteAccount site, SendRecord sendRecord)
        {
            //  没有图片时绘图
            if (string.IsNullOrEmpty(sendRecord.ImgUrl) && string.IsNullOrEmpty(sendRecord.ImgPath))
            {
                var rvDraw = await InvokeApi.DoDraw(Db, sendRecord);
                if (rvDraw.status == false)
                {
                    logger.Info("画图出错" + rvDraw.errorsimple);
                    return rvDraw;
                }
            }

            // 没有生成文章
            if (string.IsNullOrEmpty(sendRecord.Content))
            {
                var rvAi = await InvokeApi.DoAI(Db, sendRecord.Id);
                if (rvAi.status == false)
                {
                    logger.Info("生成文章出错" + rvAi.errorsimple);
                    return rvAi;
                }
            }

            // 最后同步到站点
            if (site.SiteType == SiteType.WordPress)
            {
                var rvSync = await InvokeApi.DoSync(Db, sendRecord.Id);
                return rvSync;
            }
            else
            {
                return new ReturnValue<string>() { errorsimple = $"{site.Site}是未实现的类型{site.SiteType}" };
            }

        }
    }
}
