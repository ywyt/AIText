using Entitys;
using Entitys.WP;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NLog;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Work
{
    public class InvokeApi
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static string BaseUrl = "";
        public static readonly string SEOBaseURL = "https://sz0088.oss-cn-guangzhou.aliyuncs.com/image/SEO/";
        static List<PromptTemplate> promptTempList = new List<PromptTemplate>();
        static int promptTempIdx = 0;
        static List<string> styles = new List<string>();
        static List<string> colors = new List<string>();
        static List<string> names = new List<string>();
        static int nameIdx = 0;
        static DateTime? ProductTime = null;
        const int UPDATE_PRODUCT_DAYS = 7;

        public static void Init(SqlSugarClient Db)
        {
            if (promptTempIdx > 1200)
            {
                promptTempIdx = 0;
            }

            // 可用的指令模板
            promptTempList = Db.Queryable<PromptTemplate>().Where(o => o.IsEnable == true).ToList();

            if (promptTempIdx < promptTempList.Count)
            {
                promptTempIdx = new Random().Next(0, promptTempList.Count);
            }

            // 款式
            styles = Db.Queryable<ImageResource>().GroupBy(o => o.Style).Select(o => o.Style).ToList();
            // 颜色
            colors = Db.Queryable<ImageResource>().GroupBy(o => o.Color).Select(o => o.Color).ToList();
        }

        public static async Task InitReview(SqlSugarClient Db)
        {
            if (!(names.Count > 0))
            {
                names.AddRange(new string[]  {
                    "Amelie Schmidt","Elisa Müller","Lila Hoffmann","Nora Schneider","Emma Wagner","Mia Bauer","Luna Koch","Emilia Mayer","Wolf Clara","Lea Klein","Sophie Weber","Johanna Lehmann","Luisa Baumann","Lena Schröder","Klara Vogel","Stella Schwarz","Carla Schmitt","Julia Krause","Hannah Frank","Leonie Marx","Melanie Höhne","Sarah Dorn","Laura Feld","Viola Berg","Iris Bach","Thea Hart","Elsa Kopf","Alma Ring","Tilda Stein","Amelie Weber","Elisa Lehmann","Lila Baumann","Nora Schröder","Emma Vogel","Mia Schwarz","Luna Schmitt","Emilia Krause","Clara Frank","Lea Marx","Sophie Höhne","Johanna Dorn","Luisa Feld","Lena Berg","Klara Bach","Stella Hart","Carla Kopf","Julia Ring","Hannah Stein","Leonie Schmidt","Melanie Müller","Sarah Hoffmann","Laura Schneider","Viola Wagner","Iris Bauer","Thea Koch","Elsa Mayer","Alma Wolf","Tilda Klein","Amelie Baumann","Elisa Schröder","Lila Vogel","Nora Schwarz","Emma Schmitt","Mia Krause","Luna Frank","Emilia Marx","Clara Höhne","Lea Dorn","Sophie Feld","Johanna Berg","Luisa Bach","Lena Hart","Klara Kopf","Stella Ring","Carla Stein","Julia Schmidt","Hannah Müller","Leonie Hoffmann","Melanie Schneider","Sarah Wagner","Laura Bauer","Viola Koch","Iris Mayer","Thea Wolf","Elsa Klein","Alma Weber","Tilda Lehmann","Amelie Frank","Elisa Marx","Lila Höhne","Nora Dorn","Emma Feld","Mia Berg","Luna Bach","Emilia Hart","Clara Kopf","Lea Ring","Sophie Stein","Johanna Schmidt","Luisa Müller","Klara Hoffmann","Stella Schneider","Carla Wagner","Julia Bauer","Hannah Koch","Leonie Mayer","Melanie Wolf","Sarah Klein","Laura Weber","Viola Lehmann","Iris Baumann","Thea Schröder","Elsa Vogel","Alma Schwarz","Tilda Schmitt","Amelie Krause","Elisa Frank","Lila Marx","Nora Höhne","Emma Dorn","Mia Feld","Luna Berg","Emilia Bach","Clara Hart","Lea Kopf","Sophie Ring","Johanna Stein","Luisa Schmidt","Klara Müller","Stella Hoffmann","Carla Schneider","Julia Wagner","Hannah Bauer","Leonie Koch","Melanie Mayer","Sarah Wolf","Laura Klein","Viola Weber","Iris Lehmann","Thea Baumann","Elsa Schröder","Alma Vogel","Tilda Schwarz","Amelie Schmitt","Elisa Krause","Lila Frank","Nora Marx","Emma Höhne","Mia Dorn","Luna Feld","Emilia Berg","Clara Bach","Lea Hart","Sophie Kopf","Johanna Ring","Luisa Stein","Klara Schmidt","Stella Müller","Carla Hoffmann","Julia Schneider","Hannah Wagner","Leonie Bauer","Melanie Koch","Sarah Mayer","Laura Wolf","Viola Klein","Iris Weber","Thea Lehmann","Elsa Baumann","Alma Schröder","Tilda Vogel","Amelie Schwarz","Elisa Schmitt","Lila Krause","Nora Frank","Emma Marx","Mia Höhne","Luna Dorn","Emilia Feld","Clara Berg","Lea Bach","Sophie Hart","Johanna Kopf","Luisa Ring","Klara Stein","Stella Schmidt","Carla Müller","Julia Hoffmann","Hannah Schneider","Leonie Wagner","Melanie Bauer","Sarah Koch","Laura Mayer","Viola Wolf","Iris Klein","Thea Weber","Elsa Lehmann","Alma Baumann","Tilda Schröder","Amelie Vogel","Elisa Schwarz","Lila Schmitt","Nora Krause","Emma Frank","Mia Marx","Luna Höhne","Emilia Dorn","Clara Feld","Lea Berg","Sophie Bach","Johanna Hart","Luisa Kopf","Klara Ring","Stella Stein","Carla Schmidt","Julia Müller","Hannah Hoffmann","Leonie Schneider","Melanie Wagner","Sarah Bauer","Laura Koch","Viola Mayer","Iris Wolf","Thea Klein","Elsa Weber","Alma Lehmann","Tilda Baumann","Amelie Schröder","Elisa Vogel","Lila Schwarz","Nora Schmitt","Emma Krause","Mia Frank","Luna Marx","Emilia Höhne","Clara Dorn","Lea Feld","Sophie Berg","Johanna Bach","Luisa Hart","Klara Kopf","Stella Ring","Carla Stein","Julia Schmidt","Hannah Müller","Leonie Hoffmann","Melanie Schneider","Sarah Wagner","Laura Bauer","Viola Koch","Iris Mayer","Thea Wolf","Elsa Klein","Alma Weber","Tilda Lehmann","Amelie Frank","Elisa Marx","Lila Höhne","Nora Dorn","Emma Feld","Mia Berg","Luna Bach","Emilia Hart","Clara Kopf","Lea Ring","Sophie Stein","Johanna Schmidt","Luisa Müller","Klara Hoffmann","Stella Schneider","Carla Wagner","Julia Bauer","Hannah Koch","Leonie Mayer","Melanie Wolf","Sarah Klein","Laura Weber","Viola Lehmann","Iris Baumann","Thea Schröder","Elsa Vogel","Alma Schwarz","Tilda Schmitt","Amelie Krause","Elisa Frank","Lila Marx","Nora Höhne","Emma Dorn","Mia Feld","Luna Berg","Emilia Bach","Clara Hart","Lea Kopf","Sophie Ring","Johanna Stein","Luisa Schmidt","Klara Müller","Stella Hoffmann","Carla Schneider","Julia Wagner","Hannah Bauer","Leonie Koch","Melanie Mayer","Sarah Wolf","Laura Klein","Viola Weber","Iris Lehmann","Thea Baumann","Elsa Schröder","Alma Vogel","Tilda Schwarz","Amelie Schmitt","Elisa Krause","Lila Frank","Nora Marx","Emma Höhne","Mia Dorn","Luna Feld","Emilia Berg","Clara Bach","Lea Hart","Sophie Kopf","Johanna Ring","Luisa Stein","Klara Schmidt","Stella Müller","Carla Hoffmann","Julia Schneider","Hannah Wagner","Leonie Bauer","Melanie Koch","Sarah Mayer","Laura Wolf","Viola Klein","Iris Weber","Thea Lehmann","Elsa Baumann","Alma Schröder","Tilda Vogel","Amelie Schwarz","Elisa Schmitt","Lila Krause","Nora Frank","Emma Marx","Mia Höhne","Luna Dorn","Emilia Feld","Clara Berg","Lea Bach","Sophie Hart","Johanna Kopf","Luisa Ring","Klara Stein","Stella Schmidt","Carla Müller","Julia Hoffmann","Hannah Schneider","Leonie Wagner","Melanie Bauer","Sarah Koch","Laura Mayer","Viola Wolf","Iris Klein","Thea Weber","Elsa Lehmann","Alma Baumann","Tilda Schröder","Amelie Vogel","Elisa Schwarz","Lila Schmitt","Nora Krause","Emma Frank","Mia Marx","Luna Höhne","Emilia Dorn","Clara Feld","Lea Berg","Sophie Bach","Johanna Hart","Luisa Kopf","Klara Ring","Stella Stein","Carla Schmidt","Julia Müller","Hannah Hoffmann","Leonie Schneider","Melanie Wagner","Sarah Bauer","Laura Koch","Viola Mayer","Iris Wolf","Thea Klein","Elsa Weber","Alma Lehmann","Tilda Baumann","Amelie Schröder","Elisa Vogel","Lila Schwarz","Nora Schmitt","Emma Krause","Mia Frank","Luna Marx","Emilia Höhne","Clara Dorn","Lea Feld","Sophie Berg","Johanna Bach","Luisa Hart","Klara Kopf","Stella Ring","Carla Stein","Julia Schmidt","Hannah Müller","Leonie Hoffmann","Melanie Schneider","Sarah Wagner","Laura Bauer","Viola Koch","Iris Mayer","Thea Wolf","Elsa Klein","Alma Weber","Tilda Lehmann","Amelie Frank","Elisa Marx","Lila Höhne","Nora Dorn","Emma Feld","Mia Berg","Luna Bach","Emilia Hart","Clara Kopf","Lea Ring","Sophie Stein","Johanna Schmidt","Luisa Müller","Klara Hoffmann","Stella Schneider","Carla Wagner","Julia Bauer","Hannah Koch","Leonie Mayer","Melanie Wolf","Sarah Klein","Laura Weber","Viola Lehmann","Iris Baumann","Thea Schröder","Elsa Vogel","Alma Schwarz","Tilda Schmitt","Amelie Krause","Elisa Frank","Lila Marx","Nora Höhne","Emma Dorn","Mia Feld","Luna Berg","Emilia Bach","Clara Hart","Lea Kopf","Sophie Ring","Johanna Stein","Luisa Schmidt","Klara Müller","Stella Hoffmann","Carla Schneider","Julia Wagner","Hannah Bauer","Leonie Koch","Melanie Mayer","Sarah Wolf","Laura Klein","Viola Weber","Iris Lehmann","Thea Baumann","Elsa Schröder","Alma Vogel","Tilda Schwarz","Amelie Schmitt","Elisa Krause","Lila Frank","Nora Marx","Emma Höhne","Mia Dorn","Luna Feld","Emilia Berg","Clara Bach","Lea Hart","Sophie Kopf","Johanna Ring","Luisa Stein","Klara Schmidt","Stella Müller","Carla Hoffmann","Julia Schneider","Hannah Wagner","Leonie Bauer","Melanie Koch","Sarah Mayer","Laura Wolf","Viola Klein","Iris Weber","Thea Lehmann","Elsa Baumann","Alma Schröder","Tilda Vogel","Amelie Schwarz","Elisa Schmitt","Lila Krause","Nora Frank","Emma Marx","Mia Höhne","Luna Dorn","Emilia Feld","Clara Berg","Lea Bach","Sophie Hart","Johanna Kopf","Luisa Ring","Klara Stein","Stella Schmidt","Carla Müller","Julia Hoffmann","Hannah Schneider","Leonie Wagner","Melanie Bauer","Sarah Koch","Laura Mayer","Viola Wolf","Iris Klein","Thea Weber","Elsa Lehmann","Alma Baumann","Tilda Schröder","Amelie Vogel","Elisa Schwarz","Lila Schmitt","Nora Krause","Emma Frank","Mia Marx","Luna Höhne","Emilia Dorn","Clara Feld","Lea Berg","Sophie Bach","Johanna Hart","Luisa Kopf","Klara Ring","Stella Stein","Carla Schmidt","Julia Müller","Hannah Hoffmann","Leonie Schneider","Melanie Wagner","Sarah Bauer","Laura Koch","Viola Mayer","Iris Wolf","Thea Klein","Elsa Weber","Alma Lehmann","Tilda Baumann","Amelie Schröder","Elisa Vogel","Lila Schwarz","Nora Schmitt","Emma Krause","Mia Frank","Luna Marx","Emilia Höhne","Clara Dorn","Lea Feld","Sophie Berg","Johanna Bach","Luisa Hart","Klara Kopf","Stella Ring","Carla Stein","Julia Schmidt","Hannah Müller","Leonie Hoffmann","Melanie Schneider","Sarah Wagner","Laura Bauer","Viola Koch","Iris Mayer","Thea Wolf","Elsa Klein","Alma Weber","Tilda Lehmann","Amelie Frank","Elisa Marx","Lila Höhne","Nora Dorn","Emma Feld","Mia Berg","Luna Bach","Emilia Hart","Clara Kopf","Lea Ring","Sophie Stein","Johanna Schmidt","Luisa Müller","Klara Hoffmann","Stella Schneider","Carla Wagner","Julia Bauer","Hannah Koch","Leonie May","Amelie Bauer","Anna-Lena Schmidt","Adele Hoffmann","Alena Wagner","Alexandra Müller","Andrea Koch","Antonia Mayer","Anya Vogel","Astrid Klein","Auguste Braun","Bettina Weber","Brigitte Hoffmann","Carina Fischer","Charlotte Koch","Christina Schwarz","Clara Lehmann","Constanze Wolf","Daniela Baumann","Daria Schröder","Elena Maier","Elisa Hoffmann","Emilia Schneider","Emma Wagner","Eva-Lotta Vogel","Frida Müller","Frieda Hoffmann","Friederike Schmitt","Fiona Krause","Franziska Bauer","Freya Lehmann","Gina Mayer","Greta Klein","Hanna Weber","Hedwig Wolf","Helena Baumann","Henriette Schröder","Hilda Maier","Ida Hoffmann","Ines Schneider","Ingeborg Vogel","Iris Klein","Isabella Bauer","Isolde Lehmann","Jacqueline Schmitt","Jana Wagner","Jenny Koch","Johanna Mayer","Judith Wolf","Julia Baumann","Karina Schröder","Karla Maier","Klara Hoffmann","Kristina Schneider","Lena Müller","Leonie Bauer","Lia Wagner","Lieselotte Koch","Lilly Mayer","Linda Wolf","Livia Baumann","Maja Schröder","Maren Maier","Maria Hoffmann","Marlene Schneider","Martina Vogel","Mathilda Klein","Maya Bauer","Mieke Lehmann","Mila Wagner","Mona Koch","Nadja Mayer","Nele Wolf","Nina Baumann","Noemi Schröder","Nova Maier","Oona Hoffmann","Paula Schneider","Petra Vogel","Pia Klein","Ragna Bauer","Ramona Lehmann","Rebecca Wagner","Regina Koch","Rosa Mayer","Ruby Wolf","Ruth Baumann","Sabine Schröder","Sandra Maier","Sarah Hoffmann","Seija Schneider","Silke Vogel","Sophia Klein","Stella Bauer","Svenja Lehmann","Tanja Wagner","Theresa Koch","Tina Mayer","Ursula Wolf","Valerie Baumann","Vanessa Schröder","Vera Maier","Verena Hoffmann","Veronika Schneider","Viktoria Vogel","Viola Klein","Vivian Bauer","Wanda Lehmann","Wilhelmina Wagner","Yasmin Koch","Yvonne Mayer","Zara Wolf","Anika von Berlin","Bettina aus München","Carolin Heidelberg","Daniela Köln","Elena Schwarzwald","Frieda Bayern","Greta Hamburg","Hanna Dresden","Ida Ruhr","Jana Baden","Katrin Allgäu","Lena Schleswig","Mara Stuttgart","Nina Tirol","Paula Saarbrücken","Rosa Hessisch","Sophie Hannover","Tanja Bremen","Ursula Franken","Viktoria Ostfriesland","Adele_Nürnberg","Anna.Bayer","Bettina_Hessen","Carla_Sachsen","Diana_Schleswig","Eva_BadenWürttemberg","Frieda_Hamburg","Greta_Bremen","Hanna_Köln","Ida_Frankfurt","Ines_Mainz","Jana_Dortmund","Karina_Stuttgart","Lena_Münster","Maja_Berlin","Maria_Bonn","Nina_Hannover","Paula_Dresden","Rosa_Freiburg","Sarah_Leverkusen","Tina_Mannheim","Ursula_Karlsruhe","Vera_Magdeburg","Wanda_Braunschweig","Yasmin_Lübeck","Zara_Mönchengladbach","Astrid_Hofmann","Brigitte_Weber","Christina_Klein","Daniela_Schwarz","Amalie Hoffmann","Adelheid Bauer","Alena Schmidt","Annika Wagner","Ariane Koch","Antje Mayer","Astrid Wolf","Aurelia Baumann","Amara Lehmann","Aniela Vogel","Benediktte Schwarz","Brunhilde Klein","Bärbel Schröder","Bettina Weber","Bianca Fischer","Brigitta Maier","Berta Hoffmann","Bella Wolf","Birgit Lehmann","Blanka Baumann","Carolina Koch","Cordelia Mayer","Christel Vogel","Clara Wagner","Cäcilia Schneider","Charlotte Schmitt","Catharina Bauer","Constanze Klein","Carina Wolf","Cora Lehmann","Dorothea Hoffmann","Dagmar Schmidt","Diana Maier","Delia Wagner","Daniela Koch","Dominique Mayer","Doreen Wolf","Desirée Baumann","Didem Schröder","Donna Vogel","Edeltraud Klein","Elisabeth Weber","Emilia Schneider","Erika Bauer","Elsa Hoffmann","Enya Schmidt","Estella Maier","Emma Wolf","Eugenia Lehmann","Elvira Baumann","Friederike Koch","Franziska Mayer","Felicitas Wolf","Flora Lehmann","Frieda Bauer","Fabienne Hoffmann","Frida Schmidt","Fiona Wagner","Freya Koch","Fernanda Mayer","Gertrud Klein","Gabriele Weber","Gisela Bauer","Greta Hoffmann","Gina Schmidt","Genevieve Maier","Gunda Wolf","Giovanna Lehmann","Gudrun Baumann","Grete Schröder","Helga Koch","Henriette Mayer","Hanna Wolf","Heidi Bauer","Hilda Hoffmann","Hildegard Schmidt","Harmony Maier","Hannelore Wagner","Hedwig Lehmann","Hedy Baumann","Ingrid Vogel","Isabella Koch","Ida Mayer","Ilse Wolf","Ines Schneider","Irene Bauer","Iris Hoffmann","Ingeborg Schmidt","Ivonne Maier","India Wagner","Johanna Koch","Judith Mayer","Jana Wolf","Julia Baumann","Josefine Schröder","Jasmin Hoffmann","Jeanine Schmidt","Jule Maier","Johanna-Luise Wagner","Justine Lehmann","Katharina Koch","Karolina Mayer","Klara Wolf","Karla Bauer","Kristina Hoffmann","Katja Schmidt","Kunigunde Klein","Kaiserin Weber","Karina Schröder","Kira Baumann","Leonie Koch","Luisa Mayer","Lotte Wolf","Lara Bauer","Livia Hoffmann","Lina Schmidt","Lieselotte Klein","Luna Maier","Larissa Wagner","Leonore Lehmann","Magdalena Koch","Melanie Mayer","Mia Wolf","Marlene Bauer","Mathilda Hoffmann","Mira Schmidt","Monika Maier","Marisa Wagner","Mina Lehmann","Marlene-Schörlein","Natalie Koch","Nina Mayer","Nora Wolf","Natalia Bauer","Nele Hoffmann","Nicole Schmidt","Nanna Maier","Noreen Wagner","Naomi Lehmann","Nelly Baumann","Olivia Koch","Olga Mayer","Ophelia Wolf","Ottilie Bauer","Olympia Hoffmann","Oona Schmidt","Ortrud Klein","Octavia Weber","Onora Maier","Odette Wagner","Paula Koch","Petra Mayer","Pia Wolf","Paulina Bauer","Philomena Hoffmann","Peggy Schmidt","Pamela Maier","Patrizia Wagner","Pamina Lehmann","Pia-Marie Baumann","Ramona Koch","Ronja Mayer","Regina Wolf","Rosa Bauer","Rebekka Hoffmann","Ruby Schmidt","Renate Maier","Romy Wagner","Quirin Schneider","Rahel Lehmann","Saskia Koch","Stella Mayer","Susanne Wolf","Sophie Bauer","Selma Hoffmann","Sandra Schmidt","Seraphine Maier","Silke Wagner","Stefanie Lehmann","Sonja Baumann","Theresia Koch","Tina Mayer","Tanja Wolf","Thea Bauer","Tatjana Hoffmann","Tilda Schmidt","Toni Maier","Theodora Wagner","Tabea Lehmann","Tiana Baumann","Ulrike Koch","Uschi Mayer","Valerie Wolf","Verena Bauer","Viola Hoffmann","Viktoria Schmidt","Yvonne Koch","Yasmin Mayer","Zara Wolf","Zelda Bauer","Anni-Maria Schweinsteiger","Käthe aus München","Lisl Bayer","Trude Hofbauer","Hanni Wirtz","Marlene Berg","Gerti Fuchs","Lotte Schneider","Emmy Müller","Nanni Schwarz","Retro Nickname Variants","Alwine","Brunhilde","Dietlinde","Erna","Freumina","FloraFind","SelmaSavvy","SilkeStyle","ViolaVogue","YaraYield","ZaraZest","IrisInnovate","StylishSommelier","TrendyChef","SavvyArtist","GlamArchitect","StylePhotographer","HuntJournalist"
                });
            }
            if (nameIdx == 0 || nameIdx > 5000)
            {
                nameIdx = new Random().Next(0, names.Count);
            }
            // 初始化站点商品
            if (!ProductTime.HasValue || ProductTime < DateTime.Now.AddDays(-UPDATE_PRODUCT_DAYS))
            {
                ProductTime = DateTime.Now;
                // 防止频繁刷新产品信息
                if (Db.Queryable<SiteProduct>().Any(o => o.CreateTime > DateTime.Now.AddDays(-1)))
                    return;
                // 确保是启用的，有WC授权的站点
                var siteList = Db.Queryable<SiteAccount>().Where(o => o.IsEnable == true && o.WcKey != null && o.WcKey != "" && o.WcSecret != null && o.WcSecret != "").ToList();
                foreach(var site in siteList)
                {
                    var totalProductRes = await WordpressApi.WcProductsTotal(site.Site, site.WcKey, site.WcSecret);
                    if (totalProductRes.status && totalProductRes.value > 0)
                    {
                        var productCount = Db.Queryable<SiteProduct>().Where(p => p.SiteId == site.Id).Count();
                        if (productCount < totalProductRes.value)
                        {
                            logger.Info($"{site.Site}正在拉取商品");
                            await GetProducts(Db, site, 1);
                        }
                    }
                }
            }
        }

        private static async Task<bool> GetProducts(SqlSugarClient Db, SiteAccount model, int page = 1)
        {
            int pageSize = 10;
            var rv = await WordpressApi.WcProducts(model.Site, model.WcKey, model.WcSecret, page, pageSize);
            if (rv.status)
            {
                List<WooCommerceProduct> datas = new List<WooCommerceProduct>();
                try
                {
                    datas = JsonConvert.DeserializeObject<List<WooCommerceProduct>>(rv.value);
                }
                catch (Exception ex)
                {
                    rv.False("数据解析出错");
                    return false;
                }
                if (datas != null && datas.Count > 0)
                {
                    List<SiteProduct> list = new List<SiteProduct>();
                    foreach (var item in datas)
                    {
                        // 已有记录
                        if (Db.Queryable<SiteProduct>().Any(o => o.ProductId == item.id && o.SiteId == model.Id))
                        {
                            // 近期修改的
                            if (item.date_modified_gmt > DateTime.Now.AddDays(-UPDATE_PRODUCT_DAYS - 1)) 
                            {
                                await Db.Updateable<SiteProduct>().SetColumns(o => new SiteProduct() { status = item.status, name = item.name }).Where(o => o.ProductId == item.id && o.SiteId == model.Id).ExecuteCommandAsync();
                            }
                            continue;
                        }
                        await Task.Delay(100);
                        var rcount = await WordpressApi.WcProductReviewsTotal(model.Site, model.WcKey, model.WcSecret, item.id);
                        var product = new SiteProduct()
                        {
                            SiteId = model.Id,
                            Site = model.Site,
                            ProductId = item.id,
                            Permalink = item.permalink?.ToString(),
                            date_created_gmt = item.date_created_gmt,
                            CreateTime = DateTime.Now,
                            ReviewsCount = rcount.status ? rcount.value : 0,
                            status = item.status,
                            name = item.name
                        };
                        list.Add(product);
                    }
                    if (list.Count > 0)
                    {
                        // 批量插入
                        await Db.Insertable(list).ExecuteCommandAsync();
                    }
                    else
                    {
                        return true;
                    }

                    // 当前条数不足，说明已经到头了
                    if (datas.Count < pageSize)
                    {
                        return true;
                    }
                    else
                    {
                        await Task.Delay(1000);
                        return await GetProducts(Db, model, page + 1);
                    }
                }
                else
                    return true;
            }
            else
                return false;
        }

        #region 文章相关
        /// <summary>
        /// 创建记录
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="site"></param>
        public static async Task<ReturnValue<SendRecord>> CreateRecord(SqlSugarClient Db, SiteAccount site)
        {
            var rv = new ReturnValue<SendRecord>();
            if (site == null)
                return rv;

            logger.Info($"{site.Site}开始创建发文记录");

            // 没使用过的，最少使用量的关键词
            var siteKeyword = Db.Queryable<SiteKeyword>().OrderBy(o => o.UseCount).First();

            if (siteKeyword == null)
            {
                rv.False("没有设置关键词");
                return rv;
            }

            // 抽取指令，多个站点轮流使用模板，也是一种随机（同理于随机播放的歌单）
            Interlocked.Increment(ref promptTempIdx);
            var promptTemp = promptTempList[promptTempIdx % promptTempList.Count];

            // 站点链接，取同站链接或是同关键词链接
            var urlList = Db.Queryable<SiteKeyword>().Where(o => o.Alias == siteKeyword.Alias || o.Keyword == siteKeyword.Keyword)
                .Select(o => o.URL)
                .OrderBy(o => SqlFunc.GetRandom()).Take(3).ToList();

            var urlString = string.Join(",", urlList);

            // 选择图片
            var image = await PickupImage(Db, siteKeyword.Keyword);
            if (string.IsNullOrEmpty(image?.ImagePath))
            {
                rv.False($"{site.Site}获取图片失败");
                return rv;
            }
            // 判断图片路径中是否包含斜杠或反斜杠作为分隔符
            if (image.ImagePath.Contains("/") || image.ImagePath.Contains("\\"))
            {
                // 如果是反斜杠，统一转换为斜杠
                image.ImagePath = image.ImagePath.Replace("\\", "/");

                // 检查是否有额外的斜杠，去除多余的斜杠
                image.ImagePath = image.ImagePath.TrimStart('/');
            }

            // 使用Uri类来拼接完整的URL
            Uri baseURL = new Uri(SEOBaseURL);
            Uri fullURL = new Uri(baseURL, image.ImagePath);
            SendRecord sendRecord = new SendRecord
            {
                Link = urlString,
                KeywordId = siteKeyword.Id,
                Keyword = siteKeyword.Keyword,
                TemplateId = promptTemp.Id,
                TemplateName = promptTemp.Name,
                IsSync = false,
                SyncSiteId = site.Id,
                SyncSite = site.Site,
                SyncTime = null,
                CreateTime = DateTime.Now,
                ImgResourceId = image.Id,
                ImgUrl = fullURL.ToString()
            };
            try
            {
                var id = await Db.Insertable(sendRecord).ExecuteReturnIdentityAsync();
                rv.status = id > 0;
                if (rv.status)
                {
                    sendRecord.Id = id;
                    rv.value = sendRecord;
                    await Db.Updateable<SiteKeyword>().SetColumns(o => o.UseCount == (o.UseCount + 1)).Where(o => o.Id == siteKeyword.Id).ExecuteCommandAsync();
                    await Db.Updateable<ImageResource>().SetColumns(o => o.UseCount == (o.UseCount + 1)).Where(o => o.Id == image.Id).ExecuteCommandAsync();
                }
            } 
            catch (Exception ex) 
            {
                logger.Error($"{site.Site}创建发文记录异常");
                logger.Error(ex);

            }
            logger.Info($"{site.Site}结束创建发文记录");
            return rv;

        }

        /// <summary>
        /// 选择图片
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public static async Task<ImageResource> PickupImage(SqlSugarClient Db, string keyword)
        {
            // 包含该款式+颜色的
            if (styles.Any(o => keyword.Contains(o)) && colors.Any(o => keyword.Contains(o)))
            {
                // 选择最少使用的
                var image = await Db.Queryable<ImageResource>().Where(o => keyword.Contains(o.Style) && keyword.Contains(o.Color)).OrderBy(o => o.UseCount).FirstAsync();
                return image;
            }
            // 包含该款式的
            else if (styles.Any(o => keyword.Contains(o)))
            {
                // 选择最少使用的
                var image = await Db.Queryable<ImageResource>().Where(o => keyword.Contains(o.Style)).OrderBy(o => o.UseCount).FirstAsync();
                return image;
            }
            // 从所有的文件夹中抽取
            else
            {
                var image = await Db.Queryable<ImageResource>().OrderBy(o => o.UseCount).OrderBy(o => SqlFunc.GetRandom()).FirstAsync();
                return image;
            }

        }

        /// <summary>
        /// 画图
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> DoDraw(SqlSugarClient Db, int Id)
        {
            var sendRecord = Db.Queryable<SendRecord>().Where(o => o.Id == Id).First();
            if (sendRecord == null)
                return new ReturnValue<string> { errorsimple = "记录不存在" };
            return await DoDraw(Db, sendRecord);
        }

        /// <summary>
        /// 画图
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="sendRecord"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> DoDraw(SqlSugarClient Db, SendRecord sendRecord)
        {
            if (sendRecord == null)
                return new ReturnValue<string> { errorsimple = "记录不存在" };
            var rv = new ReturnValue<string>();
            var paintAccount = Db.Queryable<PaintAccount>().Where(o => o.IsEnable == true).First();
            string imgUrl = string.Empty;
            var imgResult = await Liblibai.Text2img(paintAccount.AccessKey, paintAccount.SecretKey, $"高清产品图，{sendRecord.Keyword}");
            if (!imgResult.status)
            {
                // 生成图片出错写库
                var msg = imgResult.errorsimple;
                await Db.Updateable<SendRecord>()
                            .SetColumns(o => o.ImgErrMsg == msg)
                            .SetColumns(o => o.ImgTime == DateTime.Now)
                            .Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();
                rv.False("生成图片出错" + msg);
                return rv;
            }
            else
            {
                logger.Info($"获取到哩布绘图的UUID: {imgResult.value}");
                await Task.Delay(5000);
                rv.status = await GetImgResult(Db, paintAccount, sendRecord, imgResult.value, sendRecord.Id);
                return rv;
            }
        }

        /// <summary>
        /// 获取图片结果
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="paintAccount"></param>
        /// <param name="sendRecord"></param>
        /// <param name="uuid"></param>
        /// <param name="id"></param>
        /// <param name="retry"></param>
        /// <returns></returns>
        private static async Task<bool> GetImgResult(SqlSugarClient Db, PaintAccount paintAccount, SendRecord sendRecord, string uuid, int id, int retry = 0)
        {
            var imgUrlResult = await Liblibai.GetImage(paintAccount.AccessKey, paintAccount.SecretKey, uuid);
            if (!imgUrlResult.status)
            {
                if (retry < 3)
                {
                    await Task.Delay(3000 * (retry + 1));
                    return await GetImgResult(Db, paintAccount, sendRecord, uuid, id, ++retry);
                }
                else
                {
                    var msg = imgUrlResult.errordetailed ?? imgUrlResult.errorsimple;
                    // 更新错误信息
                    var ret = await Db.Updateable<SendRecord>()
                                .SetColumns(o => o.ImgErrMsg == msg)
                                .SetColumns(o => o.ImgTime == DateTime.Now)
                                .Where(o => o.Id == id).ExecuteCommandAsync();
                    return false;
                }
            }
            else
            {
                string imgPaths = null;
                // 下载图片
                //var imgs = imgUrlResult.Split(",", StringSplitOptions.RemoveEmptyEntries);
                //List<string> downImgs = new List<string>();
                //foreach (var url in imgs)
                //{
                //    var path = DownloadImg(url);
                //    if (!string.IsNullOrEmpty(path))
                //        downImgs.Add(path);
                //}
                //imgPaths = string.Join(",", downImgs);
                logger.Debug(imgUrlResult.value);
                sendRecord.ImgUrl = imgUrlResult.value;
                // 更新图片地址
                var ret = await Db.Updateable<SendRecord>()
                            .SetColumns(o => o.ImgUrl == imgUrlResult.value)
                            .SetColumns(o => o.ImgPath == imgPaths)
                            .SetColumns(o => o.ImgTime == DateTime.Now)
                            .Where(o => o.Id == id).ExecuteCommandAsync();
                return ret > 0;
            }
        }

        /// <summary>
        /// AI生成文章
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> DoAI(SqlSugarClient Db, int Id)
        {
            var sendRecord = Db.Queryable<SendRecord>().Where(o => o.Id == Id).First();

            return await DoAI(Db, sendRecord);
        }

        /// <summary>
        /// AI生成文章
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="sendRecord"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> DoAI(SqlSugarClient Db, SendRecord sendRecord)
        {
            logger.Info($"{sendRecord.SyncSite}开始生成文章");
            if (sendRecord == null)
                return new ReturnValue<string> { errorsimple = "记录不存在" };

            var rv = new ReturnValue<string>();

            if (!string.IsNullOrEmpty(sendRecord.Content) && !string.IsNullOrEmpty(sendRecord.Title))
            {
                rv.False("文章已经生成");
                return rv;
            }

            // AI账号
            var aiAccount = Db.Queryable<AiAccount>().Where(o => o.IsEnable == true).First();
            var promptTemp = Db.Queryable<PromptTemplate>().Where(o => o.Id == sendRecord.TemplateId).First();

            // 组装指令
            var prompt = promptTemp.Prompt.Replace("{keyword}", sendRecord.Keyword);
            prompt += $"\n需要在文章选中包含{sendRecord.Keyword}的词语插入3个链接{sendRecord.Link}";
            if (!string.IsNullOrEmpty(sendRecord.ImgUrl))
            {
                prompt += $"\n在文章的段落中插入图片{sendRecord.ImgUrl}";
            }

            logger.Info($"{sendRecord.SyncSite}发送生成文章请求");
            var contentRes = await Volcengine.ChatCompletions(aiAccount.ApiKey, prompt);
            await RecordAiRequest(Db, sendRecord, contentRes, prompt);
            logger.Info($"{sendRecord.SyncSite}生成文章请求响应了");
            if (!contentRes.status)
            {
                string msg = contentRes.errordetailed ?? contentRes.errorsimple;
                // 更新文章
                var ret = await Db.Updateable<SendRecord>()
                            .SetColumns(o => o.AiSiteId == aiAccount.Id)
                            .SetColumns(o => o.AiSite == aiAccount.Site)
                            .SetColumns(o => o.Prompt == prompt)
                            .SetColumns(o => o.ErrMsg == msg)
                            .SetColumns(o => o.AiTime == DateTime.Now)
                            .Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();

                rv.False("生成文章出错" + msg);
                logger.Info("生成文章出错" + msg);
                return rv;
            }
            else
            {
                sendRecord.Content = contentRes.value;
                // 验证文章
                var valiRes = await ValidateText(Db, aiAccount, sendRecord);
                if (!valiRes.status)
                {
                    string msg = valiRes.errordetailed ?? valiRes.errorsimple;
                    // 更新文章
                    var ret2 = await Db.Updateable<SendRecord>()
                                .SetColumns(o => o.AiSiteId == aiAccount.Id)
                                .SetColumns(o => o.AiSite == aiAccount.Site)
                                .SetColumns(o => o.Prompt == prompt)
                                .SetColumns(o => o.Content == sendRecord.Content)
                                .SetColumns(o => o.Score == sendRecord.Score)
                                .SetColumns(o => o.ErrMsg == msg)
                                .SetColumns(o => o.AiTime == DateTime.Now)
                                .Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();
                    rv.False("生成文章过不了验证" + msg);
                    logger.Info("生成文章过不了验证" + msg);
                    return rv;
                }

                sendRecord.Content = Volcengine.MD2Html(sendRecord.Content);
                // 从文章中提取标题
                (string title, string body) = ExtractTitleAndBody(sendRecord.Content);

                if (!string.IsNullOrEmpty(sendRecord.ImgPath))
                {
                    string webPath = sendRecord.ImgPath.Replace('\\', '/').TrimStart('/');
                    body.Replace(sendRecord.ImgUrl, $"{BaseUrl}/{webPath}");
                }
                sendRecord.Prompt = prompt;
                sendRecord.AiSiteId = aiAccount.Id;
                sendRecord.AiSite = aiAccount.Site;
                sendRecord.Prompt = prompt;
                sendRecord.Title = title;
                sendRecord.Content = body;
                logger.Info($"{sendRecord.SyncSite}更新记录{sendRecord.Id}的文章");
                // 更新文章
                var ret = await Db.Updateable<SendRecord>()
                            .SetColumns(o => o.AiSiteId == aiAccount.Id)
                            .SetColumns(o => o.AiSite == aiAccount.Site)
                            .SetColumns(o => o.Prompt == prompt)
                            .SetColumns(o => o.Title == title)
                            .SetColumns(o => o.Content == body)
                            .SetColumns(o => o.Score == sendRecord.Score)
                            .SetColumns(o => o.AiTime == DateTime.Now)
                            .Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();
                rv.status = ret > 0;
                return rv;
            }
        }

        /// <summary>
        /// 验证文章，评分要在7.5分以上
        /// </summary>
        /// <param name="aiAccount"></param>
        /// <param name="sendRecord"></param>
        /// <param name="retry">重试次数，不在7.5以上要重试</param>
        /// <returns></returns>
        private static async Task<ReturnValue<string>> ValidateText(SqlSugarClient Db, AiAccount aiAccount, SendRecord sendRecord, int retry = 0)
        {
            logger.Info($"{sendRecord.SyncSite}对文章进行{retry+1}次评分");
            string prompt = $"请检测下面文章是否正确采用德语并打分，满分10分，从原创性、可读性、用户价值、语法语言风格等。超过7.5分，则直接返回当前评分，不需要分析。如果文章评分低于7.5分，需要按照你评分标准继续修改该文章，直接返回修改的文章（保留图片和链接）；无法修改则直接返回评分，不需要分析：\n{sendRecord.Content}";
            var valRes = await Volcengine.ChatCompletions(aiAccount.ApiKey, prompt);
            await RecordAiRequest(Db, sendRecord, valRes, prompt);
            if (valRes.status)
            {
                if (valRes.value?.Length < 50)
                {
                    sendRecord.Score = valRes.value;
                    logger.Debug($"{sendRecord.SyncSite}评分：{valRes.value}");
                    double? score = ExtractScore(valRes.value);
                    if (score.HasValue)
                        sendRecord.Score = score.ToString();

                    if (score >= 7.5)
                    {
                        return new ReturnValue<string>() { status = true };
                    }
                    if (retry < 5)
                    {
                        return await ValidateText(Db, aiAccount, sendRecord, ++retry);
                    }
                    return new ReturnValue<string>() { status = false, errorsimple = "评分不合格" };
                }
                else
                {
                    if (retry < 5)
                    {
                        if (valRes.value?.Length > 50)
                        {
                            (double? score, string content) = FilterDeContent(valRes.value);
                            // 当前评分
                            if (score.HasValue)
                                sendRecord.Score = score.ToString();

                            // 它有修改文章， 过短的是评语，还可能是德语的评语，不进行修改
                            if (!string.IsNullOrEmpty(content) && content.Length > 1000)
                                sendRecord.Content = content;
                            else
                            {
                                // 否则返回的可能是评语
                                logger.Debug($"{sendRecord.SyncSite} 第{retry + 1}次验证文章{valRes.value}");
                            }

                            if (score >= 7.5)
                            {
                                return new ReturnValue<string>() { status = true, errorsimple = "评分合格" };
                            }
                        }
                        return await ValidateText(Db, aiAccount, sendRecord, ++retry);
                    }
                    else
                    {
                        return new ReturnValue<string>() { status = false, errorsimple = "评分不合格" };
                    }
                }
            }
            else
            {
                logger.Info($"{sendRecord.SyncSite}对文章进行评分出错了");
                if (retry < 3)
                    return await ValidateText(Db, aiAccount, sendRecord, ++retry);
                return valRes;
            }
        }

        /// <summary>
        /// 记录AI生成文章的请求
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="sendRecord"></param>
        /// <param name="returnValue"></param>
        /// <param name="prompt"></param>
        /// <returns></returns>
        private static async Task RecordAiRequest(SqlSugarClient Db, SendRecord sendRecord, ReturnValue<string> returnValue, string prompt)
        {
            string msg = returnValue.errordetailed ?? returnValue.errorsimple;
            string content = returnValue.value;
            await Db.Insertable<AiRecord>(new AiRecord
            {
                SendRecordId = sendRecord.Id,
                Prompt = prompt,
                Content = returnValue.value,
                ErrMsg = returnValue.errorsimple,
                CreateTime = DateTime.Now
            }).ExecuteCommandAsync();
        }

        /// <summary>
        /// 过滤内容，返回德语文章和评分
        /// </summary>
        /// <param name="markdownText"></param>
        /// <returns></returns>
        public static (double?, string) FilterDeContent(string markdownText)
        {
            double? score = null;
            // 保留原文中的所有空行（通过换行符分段）
            var paragraphs = markdownText.Split(new[] { "\n" }, StringSplitOptions.None);

            // 正则表达式匹配中文字符
            Regex chineseRegex = new Regex("[\u4e00-\u9fff]");
            // 正则匹配仅由数字和符号 `+-*/` 组成的段落（允许"评分"两个字，且其他中文会跳过）
            Regex numberSymbolRegex = new Regex(@"^([\d\s+\-*/.:：评分Bewertung]+)$");

            StringBuilder resultMarkdown = new StringBuilder(); // 用于存储过滤后的 Markdown 文本

            bool isDe = false;
            foreach (var paragraph in paragraphs)
            {
                if (!isDe) // 如果已经找到符合条件的段落，则跳出循环
                {
                    if (string.IsNullOrWhiteSpace(paragraph)) continue;

                    // 如果段落仅由数字和符号 `+-*/` 和冒号 `:` 或 `：` 组成（且不包含"评分"），则跳过
                    if (numberSymbolRegex.IsMatch(paragraph))
                    {
                        score = ExtractScore(paragraph);
                        continue;
                    }

                    // 如果段落包含中文，则跳过
                    if (chineseRegex.IsMatch(paragraph))
                    {
                        continue;
                    }
                }
                if (!isDe)
                    isDe = true;

                // 将符合条件的段落追加到结果中，并保持段落间空行
                resultMarkdown.AppendLine(paragraph);
                //resultMarkdown.AppendLine(); // 保持 Markdown 格式的段落间空行
            }

            // 输出过滤后的 Markdown 文本
            string filteredMarkdown = resultMarkdown.ToString(); // 去除首尾空白
            return (score, filteredMarkdown);
        }

        /// <summary>
        /// 提取评分
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public static double? ExtractScore(string input)
        {
            // 匹配评分数字，允许小数和整数，以及可能的“/10”或“分”后缀
            Regex regex = new Regex(@"(\d+(\.\d+)?)"); // 捕获数字部分

            Match match = regex.Match(input);

            if (match.Success)
            {
                if (double.TryParse(match.Groups[1].Value, out double score))
                {
                    return score;
                }
            }

            return null; // 如果没有匹配到，返回null
        }

        /// <summary>
        /// 从html中提取标题和正文
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static (string title, string body) ExtractTitleAndBody(string content)
        {
            if (string.IsNullOrWhiteSpace(content))
            {
                return (string.Empty, string.Empty);
            }

            // 将内容按段落分割
            var paragraphs = Regex.Split(content.Trim(), @"\r?\n");

            if (paragraphs.Length == 0)
            {
                return (string.Empty, content);
            }

            // 第一段是标题
            string rawTitle = paragraphs[0].Trim();
            string title = Regex.Replace(rawTitle, "<.*?>", ""); // Remove all HTML tags;

            // 获取 body（去掉第一段）
            string body = string.Join("\n", paragraphs.Length > 1 ? paragraphs[1..] : Array.Empty<string>());

            return (title, body);
        }

        /// <summary>
        /// 发布文章，同步到站点
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> DoSync(SqlSugarClient Db, int Id)
        {
            var sendRecord = Db.Queryable<SendRecord>().Where(o => o.Id == Id).First();
            return await DoSync(Db, sendRecord);
        }

        /// <summary>
        /// 发布文章，同步到站点
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="sendRecord"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> DoSync(SqlSugarClient Db, SendRecord sendRecord)
        {
            if (sendRecord == null)
                return new ReturnValue<string> { errorsimple = "记录不存在" };

            var rv = new ReturnValue<string>();
            if (sendRecord.IsSync == true)
            {
                rv.False("文章已经同步");
                return rv;
            }
            if (string.IsNullOrEmpty(sendRecord.Content))
            {
                rv.False("文章未生成");
                return rv;
            }
            // 同步站点
            var syncAccount = Db.Queryable<SiteAccount>().Where(o => o.Id == sendRecord.SyncSiteId).First();
            if (syncAccount == null)
            {
                rv.False("同步站点不存在");
                return rv;
            }
            if (syncAccount.SiteType != SiteType.WordPress)
            {
                rv.False("站点类型不支持");
                return rv;
            }

            logger.Info($"{sendRecord.SyncSite}开始同步文章到站点");

            // 没有获取JWT时，生成JWT
            if (string.IsNullOrEmpty(syncAccount.AccessKey))
            {
                var hasGotToken = await GenAccessToken(Db, syncAccount, sendRecord);
                if (!hasGotToken)
                {
                    rv.False("获取WP站点Token出错");
                    return rv;
                }
            }

            // 图片没有上传
            //if (sendRecord.Content.Contains(sendRecord.ImgUrl))
            if (string.IsNullOrEmpty(sendRecord.ImgUpload))
            {
                logger.Info($"{sendRecord.SyncSite}没有同步图片到站点，先上传图片");
                var upload = await UploadImg(Db, syncAccount, sendRecord);
                if (upload.status == false)
                {
                    rv.False("上传图片出错" + upload.errorsimple);
                    return rv;
                }
            }

            return await PublishArticle(Db, syncAccount, sendRecord);
        }

        /// <summary>
        /// 发布文章，同步到站点
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="sendRecord"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> CheckAvailable(SqlSugarClient Db, SiteAccount syncAccount, int retry = 0)
        {
            var rv = new ReturnValue<string>();

            if (syncAccount == null)
            {
                rv.False("同步站点不存在");
                return rv;
            }
            if (syncAccount.SiteType != SiteType.WordPress)
            {
                rv.False("站点类型不支持");
                return rv;
            }

            // 没有获取JWT或者JWT过期时，生成JWT
            if (string.IsNullOrEmpty(syncAccount.AccessKey) || retry > 0)
            {
                var hasGotToken = await GenAccessToken(Db, syncAccount);
                if (!hasGotToken)
                {
                    rv.False("获取WP站点Token出错");
                    return rv;
                }
            }
            rv = await WordpressApi.ValidateToken(syncAccount.Site, syncAccount.AccessKey);
            if (!rv.status && retry == 0)
            {
                return await CheckAvailable(Db, syncAccount, ++retry);
            }
            return rv;
        }

        /// <summary>
        /// 上传图片
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="syncAccount"></param>
        /// <param name="sendRecord"></param>
        /// <param name="retry"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> UploadImg(SqlSugarClient Db, SiteAccount syncAccount, SendRecord sendRecord, int retry = 0)
        {
            var rv = new ReturnValue<string>();
            if (string.IsNullOrEmpty(sendRecord.ImgPath) && string.IsNullOrEmpty(sendRecord.ImgUrl))
            {
                rv.True("没有图片");
                return rv;
            }
            // 包含了图片链接（可能的上传路径）
            if (sendRecord.Content?.Contains("wp-image-") == true || sendRecord.Content?.Contains("/wp-content/uploads/") == true)
            {
                rv.True("已经上传过图片");
                return rv;
            }

            ReturnValue<string> uploadRes = new Entitys.ReturnValue<string>();
            if (!string.IsNullOrEmpty(sendRecord.ImgPath))
            {
                uploadRes = await WordpressApi.UploadImage(syncAccount.Site, syncAccount.AccessKey, sendRecord.ImgPath, sendRecord.Keyword);
            }
            else
            {
                using (HttpClient client = new HttpClient())
                {
                    logger.Info($"{sendRecord.SyncSite}下载源图片{sendRecord.ImgUrl}");
                    byte[] imageBytes = null;
                    try
                    {
                        // 发送 GET 请求并获取响应
                        HttpResponseMessage response = await client.GetAsync(sendRecord.ImgUrl);

                        // 确保响应成功
                        response.EnsureSuccessStatusCode();

                        // 获取文件名，优先从 Content-Disposition 响应头获取
                        string filename = GetFilenameFromContentDisposition(response.Content.Headers.ContentDisposition);

                        // 如果 Content-Disposition 中没有文件名，则从 URL 中提取
                        if (string.IsNullOrEmpty(filename))
                        {
                            filename = GetFilenameFromUrl(sendRecord.ImgUrl);
                        }

                        // 读取响应内容并返回 byte[]
                        imageBytes = await response.Content.ReadAsByteArrayAsync();
                        logger.Info($"{sendRecord.SyncSite}下载原图片完毕，上传字节流");
                        uploadRes = await WordpressApi.UploadImage(syncAccount.Site, syncAccount.AccessKey, imageBytes, sendRecord.Keyword, filename);
                    }
                    catch (Exception ex)
                    {
                        logger.Error("下载图片出错");
                        logger.Error(ex);
                        uploadRes.False("下载图片出错");
                        if (retry < 1)
                        {
                            await Task.Delay(30000);
                            return await UploadImg(Db, syncAccount, sendRecord, ++retry);
                        }
                    }
                    finally {
                        imageBytes = null;
                    }
                }
            }

            if (!uploadRes.status)
            {
                logger.Info($"{sendRecord.SyncSite}上传失败");
                // JWT 无效时，重新获取JWT
                if (uploadRes.errorsimple.StartsWith("401|"))
                {
                    logger.Info($"{sendRecord.SyncSite}JWT无效/过期");
                    if (retry < 1)
                    {
                        var genRes = await GenAccessToken(Db, syncAccount, sendRecord);
                        return await UploadImg(Db, syncAccount, sendRecord, ++retry);
                    }
                    else
                    {
                        logger.Info($"再度获取WP站点{syncAccount.Site}的Token失败{uploadRes.errorsimple}");
                        rv.False($"再度获取WP站点的Token失败{uploadRes.errorsimple}");
                        return rv;
                    }
                }
                else
                {
                    string msg = uploadRes.errorsimple;
                    await Db.Updateable<SendRecord>().SetColumns(o => o.SyncErrMsg == msg).Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();
                    rv.False("上传图片出错");
                    return rv;
                }
            }
            else
            {
                rv.True(uploadRes.value);
                if (!string.IsNullOrEmpty(sendRecord.Content) && sendRecord.Content.Contains(sendRecord.ImgUrl))
                {
                    sendRecord.Content = sendRecord.Content.Replace(sendRecord.ImgUrl, uploadRes.value);
                    logger.Info($"{sendRecord.SyncSite}上传图片成功，更新图片地址{uploadRes.value}");
                    await Db.Updateable<SendRecord>().SetColumns(o => o.Content == sendRecord.Content).SetColumns(o => o.ImgUpload == uploadRes.value).Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();
                }

                return rv;
            }
        }

        /// <summary>
        /// 从Headers.ContentDisposition获取文件名
        /// </summary>
        /// <param name="contentDisposition"></param>
        /// <returns></returns>
        private static string GetFilenameFromContentDisposition(ContentDispositionHeaderValue contentDisposition)
        {
            if (contentDisposition != null)
            {
                // 优先使用 filename* 参数（RFC 5987）
                if (!string.IsNullOrEmpty(contentDisposition.FileNameStar))
                {
                    return contentDisposition.FileNameStar;
                }
                // 其次使用 filename 参数（RFC 2183）
                else if (!string.IsNullOrEmpty(contentDisposition.FileName))
                {
                    return contentDisposition.FileName;
                }
            }
            return null;
        }

        /// <summary>
        /// 从 URL 中提取文件名
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private static string GetFilenameFromUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return null;
            }

            try
            {
                // 从 URL 中提取文件名
                Uri uri = new Uri(url);
                string path = uri.AbsolutePath;
                string filename = Path.GetFileName(path);
                return filename;
            }
            catch (UriFormatException)
            {
                // 处理无效的 URL
                Console.WriteLine("Invalid URL format.");
                return null;
            }
        }

        /// <summary>
        /// 发布文章
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="syncAccount">站点账号</param>
        /// <param name="sendRecord">记录</param>
        /// <param name="retry">重试次数</param>
        /// <returns></returns>
        private static async Task<ReturnValue<string>> PublishArticle(SqlSugarClient Db, SiteAccount syncAccount, SendRecord sendRecord, int retry = 0)
        {
            var rv = new ReturnValue<string>();
            logger.Info($"{sendRecord.SyncSite}发布文章中 {sendRecord.Title}");
            var sendRes = await WordpressApi.PostToCreate(syncAccount.Site, syncAccount.AccessKey, sendRecord.Title, sendRecord.Content);
            if (!sendRes.status)
            {
                // JWT 无效时，重新获取JWT
                if (sendRes.errorsimple.StartsWith("401|"))
                {
                    logger.Info($"发布文章失败，再度获取WP站点{syncAccount.Site}的Token");
                    if (retry < 1)
                    {
                        var genRes = await GenAccessToken(Db, syncAccount, sendRecord);
                        return await PublishArticle(Db, syncAccount, sendRecord, ++retry);
                    }
                    else
                    {
                        logger.Info($"再度获取WP站点{syncAccount.Site}的Token失败{sendRes.errorsimple}");
                        rv.False($"再度获取WP站点的Token失败{sendRes.errorsimple}");
                        return rv;
                    }
                }
                else
                {
                    string msg = sendRes.errorsimple;
                    await Db.Updateable<SendRecord>().SetColumns(o => o.SyncErrMsg == msg).Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();
                    rv.False("发送出错");
                    return rv;
                }
            }
            else
            {
                logger.Info($"{sendRecord.SyncSite}同步完成");
                rv.status = await UpdateSyncResult(Db, sendRes.value, sendRecord.Id);
                return rv;
            }
        }

        /// <summary>
        /// 获取WP站点的JWT
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="syncAccount"></param>
        /// <param name="sendRecord"></param>
        /// <returns></returns>
        private static async Task<bool> GenAccessToken(SqlSugarClient Db, SiteAccount syncAccount, SendRecord sendRecord = null)
        {
            var tokenRes = await WordpressApi.GetAccessToken(syncAccount.Site, syncAccount.Username, syncAccount.Password);
            if (!tokenRes.status)
            {
                string msg = "获取WP站点的token出错" + tokenRes.errordetailed ?? tokenRes.errorsimple;
                if (sendRecord?.Id > 0)
                    await Db.Updateable<SendRecord>().SetColumns(o => o.SyncErrMsg == msg).Where(o => o.Id == sendRecord.Id).ExecuteCommandAsync();
                return false;
            }
            string token = tokenRes.value;
            syncAccount.AccessKey = token;
            await Db.Updateable<SiteAccount>().SetColumns(o => o.AccessKey == token).Where(o => o.Id == syncAccount.Id).ExecuteCommandAsync();
            return true;
        }

        /// <summary>
        /// 更新同步结果
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="sendRes"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        private static async Task<bool> UpdateSyncResult(SqlSugarClient Db, string sendRes, int Id)
        {
            var syncUrl = string.Empty;
            if (!string.IsNullOrWhiteSpace(sendRes) && (sendRes.StartsWith("{") || sendRes.StartsWith("[")))
            {
                try
                {
                    var jsonResult = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(sendRes);
                    syncUrl = jsonResult["link"];
                    logger.Info(syncUrl);
                }
                catch (Exception ex)
                {
                    await Db.Updateable<SendRecord>()
                        .SetColumns(o => o.SyncErrMsg == sendRes)
                        .Where(o => o.Id == Id)
                        .ExecuteCommandAsync();
                    logger.Error(ex);
                    logger.Info(sendRes);
                    return false;
                }
            }
            var ret = await Db.Updateable<SendRecord>()
                        .SetColumns(o => new SendRecord { IsSync = true, SyncUrl = syncUrl, SyncTime = DateTime.Now })
                        .Where(o => o.Id == Id)
                        .ExecuteCommandAsync();
            return ret > 0;

        }

        #endregion

        #region 评论相关

        /// <summary>
        /// 创建评论记录
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="site"></param>
        /// <param name="sendReview"></param>
        public static async Task<ReturnValue<SendReview>> CreateReview(SqlSugarClient Db, SiteAccount site, SiteProduct product)
        {
            var rv = new ReturnValue<SendReview>();
            if (site == null || string.IsNullOrEmpty(site.WcKey) || string.IsNullOrEmpty(site.WcSecret))
                return rv;

            logger.Info($"{site.Site}开始创建评论记录");

            // 抽取名字
            Interlocked.Increment(ref nameIdx);
            var name = names[nameIdx % names.Count];
            int rating = 5;
            // TODO: 有时候给个少于5分的
            SendReview sendReview = new SendReview
            {
                SiteProductId = product.Id,
                ProductId = product.ProductId,
                Name = name,
                Rating = 5,

                IsSync = false,
                SyncSiteId = site.Id,
                SyncSite = site.Site,
                SyncTime = null,
                CreateTime = DateTime.Now,
            };
            try
            {
                var id = await Db.Insertable(sendReview).ExecuteReturnIdentityAsync();
                rv.status = id > 0;
                if (rv.status)
                {
                    sendReview.Id = id;
                    rv.value = sendReview;
                }
            }
            catch (Exception ex)
            {
                logger.Error($"{site.Site}创建评论记录异常");
                logger.Error(ex);

            }
            logger.Info($"{site.Site}结束创建评论记录");
            return rv;

        }

        /// <summary>
        /// 抽一个商品的AI评论
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="sendReview"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> DrawAIReview(SqlSugarClient Db, SendReview sendReview, int retry = 0)
        {
            logger.Info($"{sendReview.SyncSite}开始抽取评论");
            if (sendReview == null)
                return new ReturnValue<string> { errorsimple = "记录不存在" };

            var rv = new ReturnValue<string>();

            var review = await Db.Queryable<SiteReview>().Where(o => o.SiteProductId == sendReview.SiteProductId && o.IsUse == false).FirstAsync();
            if (review == null)
            {
                if (retry < 1)
                {
                    var siteProduct = await Db.Queryable<SiteProduct>().Where(o => o.Id == sendReview.SiteProductId).FirstAsync();
                    // 没有评论时，生成评论
                    var res = await DoAIReview(Db, siteProduct);
                    if (res.status)
                    {
                        return await DrawAIReview(Db, sendReview, ++retry);
                    }
                    else
                    {
                        logger.Info($"{sendReview.SyncSite}没有{siteProduct.name}评论");
                        rv.False($"{sendReview.SyncSite}没有{siteProduct.name}评论");
                    }
                }
                else
                {
                    rv.False($"{sendReview.SyncSite}没有获取到评论");
                }
                return rv;
            }
            else
            {
                logger.Info($"{sendReview.SyncSite}抽取了评论，修改记录");
                // 抽取评论后，对应的修改记录
                sendReview.Content = review.Content;
                sendReview.SiteReviewId = review.Id;
                sendReview.AiTime = review.CreateTime;
                await Db.Updateable<SendReview>().SetColumns(o => new SendReview { Content = review.Content, SiteReviewId = review.Id, AiTime = review.CreateTime }).Where(o => o.Id == sendReview.Id).ExecuteCommandAsync();
                review.IsUse = true;
                await Db.Updateable<SiteReview>().SetColumns(o => o.IsUse == true).Where(o => o.Id == review.Id).ExecuteCommandAsync();
                rv.True("抽取评论成功");
                return rv;
            }
        }

        /// <summary>
        /// AI生成评论
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="sendReview"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> DoAIReview(SqlSugarClient Db, SiteProduct siteProduct, int retry = 0)
        {
            logger.Info($"{siteProduct.Site}开始生成商品{siteProduct.name}的评论");
            if (siteProduct == null)
                return new ReturnValue<string> { errorsimple = "记录不存在" };

            var rv = new ReturnValue<string>();

            // AI账号
            var aiAccount = Db.Queryable<AiAccount>().Where(o => o.IsEnable == true).First();

            int GEN_COUNT = 10;

            // 组装指令
            var prompt = $"请用德语写出{GEN_COUNT}条真实的德国啤酒裙女性用户好评，商品链接{siteProduct.Permalink}，要求参照德国啤酒裙用户人群职业、画像特点，重点突出啤酒裙款式丰富多样、材料环保、设计新颖、受众多。";
            prompt += "请注意：只需要回复评论的具体内容，不需要评论之外的信息，也不需要序号、引用（引号），一段为一个评论";

            logger.Info($"{siteProduct.Site}发送生成评论请求");
            var contentRes = await Volcengine.ChatCompletions(aiAccount.ApiKey, prompt);
            var aiId = await RecordAiRequest(Db, siteProduct, contentRes, prompt);
            logger.Info($"{siteProduct.Site}生成评论请求响应了");
            if (!contentRes.status)
            {
                string msg = contentRes.errordetailed ?? contentRes.errorsimple;
                rv.False("生成评论出错" + msg);
                logger.Info("生成评论出错" + msg);
                return rv;
            }
            else
            {
                var content = contentRes.value;

                content = Volcengine.MD2Html(content);

                // 从评论记录中提取合格评论
                var reviews = ExtractReviews(content);

                // 当评论数达到预期，插入评论列表
                if (reviews.Count == GEN_COUNT)
                {
                    logger.Info($"{siteProduct.Site}生成评论成功，插入{siteProduct.name}的{reviews.Count}条评论记录");
                    List<SiteReview> insertReviews = reviews.Select(o => new SiteReview
                    {
                        AiReviewId = aiId,
                        Content = o,
                        CreateTime = DateTime.Now,
                        IsUse = false,
                        ProductId = siteProduct.ProductId,
                        SiteProductId = siteProduct.Id,
                        SiteId = siteProduct.SiteId,
                    }).ToList();
                    await Db.Insertable(insertReviews).ExecuteCommandAsync();
                    rv.True("");
                }
                else 
                {
                    logger.Warn($"{siteProduct.Site}生成{siteProduct.name}的评论，解析为{reviews.Count}条评论记录");
                    if (retry < 1)
                        return await DoAIReview(Db, siteProduct, ++retry);
                }
                return rv;
            }
        }

        /// <summary>
        /// 从html中提取标题和正文
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        private static List<string> ExtractReviews(string content)
        {
            List<string> list = new List<string>();
            if (string.IsNullOrWhiteSpace(content))
            {
                return list;
            }

            // 将内容按段落分割
            var paragraphs = Regex.Split(content.Trim(), @"\r?\n");

            if (paragraphs.Length == 0)
            {
                return list;
            }
            // 正则表达式匹配中文字符
            Regex chineseRegex = new Regex("[\u4e00-\u9fff]");

            foreach (var paragraph in paragraphs)
            {
                // 如果段落包含中文，则跳过
                if (chineseRegex.IsMatch(paragraph))
                {
                    continue;
                }
                list.Add(paragraph);
            }

            return list;
        }


        /// <summary>
        /// 记录AI生成评论的请求
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="sendRecord"></param>
        /// <param name="returnValue"></param>
        /// <param name="prompt"></param>
        /// <returns></returns>
        private static async Task<int> RecordAiRequest(SqlSugarClient Db, SiteProduct siteProduct, ReturnValue<string> returnValue, string prompt)
        {
            string msg = returnValue.errordetailed ?? returnValue.errorsimple;
            string content = returnValue.value;
            var record = new AiReview
            {
                SiteProductId = siteProduct.Id,
                Prompt = prompt,
                Content = returnValue.value,
                ErrMsg = returnValue.errordetailed ?? returnValue.errorsimple,
                CreateTime = DateTime.Now
            };
            return await Db.Insertable(record).ExecuteReturnIdentityAsync();
        }

        /// <summary>
        /// 发布评论，同步到站点
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="sendReview"></param>
        /// <returns></returns>
        public static async Task<ReturnValue<string>> DoSyncReview(SqlSugarClient Db, SendReview sendReview)
        {
            if (sendReview == null)
                return new ReturnValue<string> { errorsimple = "记录不存在" };

            var rv = new ReturnValue<string>();
            if (sendReview.IsSync == true)
            {
                rv.False("评论已经同步");
                return rv;
            }
            if (string.IsNullOrEmpty(sendReview.Content))
            {
                rv.False("评论未生成");
                return rv;
            }
            // 同步站点
            var syncAccount = Db.Queryable<SiteAccount>().Where(o => o.Id == sendReview.SyncSiteId).First();
            if (syncAccount == null)
            {
                rv.False("同步站点不存在");
                return rv;
            }
            if (syncAccount.SiteType != SiteType.WordPress)
            {
                rv.False("站点类型不支持");
                return rv;
            }
            if (string.IsNullOrEmpty(syncAccount.WcKey) || string.IsNullOrEmpty(syncAccount.WcSecret))
            {
                rv.False("没有配置站点的WooCommerce授权");
                return rv;
            }

            logger.Info($"{sendReview.SyncSite}开始同步评论到站点");

            logger.Info($"{sendReview.SyncSite}发布评论中 {sendReview.ProductId}");
            // 生成邮箱（不重要的）
            var email = Regex.Replace(sendReview.Name, @"[^A-Za-z0-9]", "_");
            email += "@ryty.tk";
            var sendRes = await WordpressApi.WcReviewCreate(syncAccount.Site, syncAccount.WcKey, syncAccount.WcSecret, 
                                sendReview.ProductId, sendReview.Name, email, sendReview.Content, sendReview.Rating);
            if (!sendRes.status)
            {
                string msg = sendRes.errorsimple;
                await Db.Updateable<SendReview>().SetColumns(o => o.SyncErrMsg == msg).Where(o => o.Id == sendReview.Id).ExecuteCommandAsync();
                rv.False("发送出错");
                return rv;
            }
            else
            {
                logger.Info($"{sendReview.SyncSite}同步完成");
                rv.status = await UpdateSyncReview(Db, sendRes.value, sendReview.Id, sendReview.SiteProductId);
                logger.Info($"{sendReview.SyncSite}修改了商品{sendReview.SiteProductId}的评论数");
                return rv;
            }
        }

        /// <summary>
        /// 更新同步结果
        /// </summary>
        /// <param name="Db"></param>
        /// <param name="sendRes"></param>
        /// <param name="Id"></param>
        /// <returns></returns>
        private static async Task<bool> UpdateSyncReview(SqlSugarClient Db, string sendRes, int Id, int siteProductId)
        {
            int syncReviewId = -1;
            DateTime? date_created_gmt = null;
            if (!string.IsNullOrWhiteSpace(sendRes) && (sendRes.StartsWith("{") || sendRes.StartsWith("[")))
            {
                try
                {
                    var jsonResult = Newtonsoft.Json.JsonConvert.DeserializeObject<dynamic>(sendRes);
                    string tmp_id = jsonResult["id"];
                    string tmp_date = jsonResult["date_created_gmt"];
                    int.TryParse(tmp_id, out syncReviewId);
                    if (DateTime.TryParse(tmp_date, out DateTime date))
                    {
                        date_created_gmt = date;
                    }
                }
                catch (Exception ex)
                {
                    await Db.Updateable<SendReview>()
                        .SetColumns(o => o.SyncErrMsg == sendRes)
                        .Where(o => o.Id == Id)
                        .ExecuteCommandAsync();
                    logger.Error(ex);
                    logger.Info(sendRes);
                    return false;
                }
            }
            var ret = await Db.Updateable<SendReview>()
                        .SetColumns(o => new SendReview { IsSync = true, ReviewId = syncReviewId, SyncTime = DateTime.Now, date_created_gmt = date_created_gmt })
                        .Where(o => o.Id == Id)
                        .ExecuteCommandAsync();

            await Db.Updateable<SiteProduct>()
                    .SetColumns(o => o.ReviewsCount == (o.ReviewsCount + 1))
                    .Where(o => o.Id == siteProductId)
                    .ExecuteCommandAsync();
            return ret > 0;

        }

        #endregion
    }
}
