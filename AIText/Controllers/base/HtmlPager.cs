using commons.util;
using Microsoft.AspNetCore.Html;

namespace Microsoft.AspNetCore.Mvc.Rendering
{
    /// <summary>
    /// 分页控件
    /// dyq 2012-3-30
    /// 分页参数固定为pageindex ，使用post是需要引用外部脚本
    /// </summary>
    public static class HtmlPager
    {


        /// <summary>
        /// Ajax分页重载
        /// dyq 2012-3-30
        /// </summary>
        /// <param name="htmlHelper"></param>
        /// <param name="pagerList"></param>
        /// <param name="formId"></param>
        /// <param name="pageNum"></param>
        /// <param name="isPost"></param>
        /// <returns></returns>
        public static IHtmlContent Pager<T>(this IHtmlHelper htmlHelper,
             PageList<T> pagerList, string formId = "form0", int pageNum = 10, bool isPost = true, string filePath = "~/Views/Shared/Pager.cshtml")
        {
            var task = (htmlHelper.PartialAsync(
                filePath,
                new PageConfig
                {
                    TotalItemCount = pagerList.PagerModel.Count,
                    PageSize = pagerList.PagerModel.PageSize,
                    CurrentPageIndex = pagerList.PagerModel.PageIndex,
                    PageNum = pageNum,
                    FormId = formId,
                    IsPost = isPost
                }));
            task.Wait();
            return task.Result;
        }


    }


    /// <summary>
    /// 分页配置
    /// </summary>
    public class PageConfig
    {

        #region 必须传入参数

        int _CurrentPageIndex = 1;
        /// <summary>
        /// 当前页
        /// </summary>
        public int CurrentPageIndex
        {
            get { return _CurrentPageIndex; }
            set { _CurrentPageIndex = value; }
        }

        /// <summary>
        /// 记录总数
        /// </summary>
        public int TotalItemCount
        {
            get;
            set;
        }

        /// <summary>
        /// 每页数量
        /// </summary>
        public int PageSize
        {
            get;
            set;
        }

        #endregion

        #region 动态属性，根据其他参数运算的结果

        /// <summary>
        /// 
        /// </summary>
        int _HalfPagerNum = -1;

        /// <summary>
        /// PageNum的一半，以下很多公式会用到，所以预先计算
        /// </summary>
        public int HalfPagerNum
        {
            get
            {
                if (_HalfPagerNum == -1)
                {
                    _HalfPagerNum = PageNum / 2;
                }
                return _HalfPagerNum;
            }
        }

        /// <summary>
        /// /
        /// </summary>
        int _IndexPagerNum = -1;
        /// <summary>
        /// 输出页面开始的位置
        /// </summary>
        public int IndexPagerNum
        {

            get
            {
                if (_IndexPagerNum == -1)
                {
                    int _Index;
                    if (CurrentPageIndex < HalfPagerNum + 1)
                    {
                        _Index = 1;
                    }
                    else if (CurrentPageIndex > TotalPages - HalfPagerNum)
                    {
                        _Index = TotalPages - PageNum + 1;
                    }
                    else
                    {
                        _Index = CurrentPageIndex - HalfPagerNum;
                    }
                    _Index = _Index < 1 ? 1 : _Index;
                    _IndexPagerNum = _Index;
                }
                return _IndexPagerNum;
            }

        }


        int _TotalPages = -1;
        /// <summary>
        /// 总页数
        /// </summary>
        public int TotalPages
        {
            get
            {
                int temp = TotalItemCount / PageSize;
                _TotalPages = TotalItemCount % PageSize == 0 ? temp : ++temp;
                return _TotalPages;
            }
        }


        string _JumpLink = "";
        /// <summary>
        /// 跳转模版
        /// </summary>
        public string JumpLink
        {
            get
            {
                if (_JumpLink.Length == 0)
                {
                    if (IsPost)
                    {
                        //如果是post
                        _JumpLink = "href='#' onclick=\"return AjaxToPage('" + FormId + "',{0})\"";
                    }
                    else
                    {

                    }
                }
                return _JumpLink;
            }

        }
        #endregion

        #region 可配置参数

        string _CurrentpageClass = "class='active'";
        /// <summary>
        /// 当前页样式
        /// </summary>
        public string CurrentpageClass
        {
            get { return _CurrentpageClass; }
            set { _CurrentpageClass = value; }
        }


        string _PageClass = "paginate_button";
        /// <summary>
        /// 非当前页样式
        /// </summary>
        public string PageClass
        {
            get { return _PageClass; }
            set { _PageClass = value; }
        }


        string _DivClass = "class='pagination'";
        /// <summary>
        /// 最外层div样式名称
        /// </summary>
        public string DivClass
        {
            get { return _DivClass; }
            set { _DivClass = value; }
        }


        string _LabelClass = "class=''";
        /// <summary>
        ///  首页 上一页  下一页  尾页 可用时候样式
        /// </summary>
        public string LabelClass
        {
            get { return _LabelClass; }
            set { _LabelClass = value; }
        }


        string _NotLabelClass = "class=''";
        /// <summary>
        /// 首页 上一页  下一页  尾页 不可用时样式
        /// </summary>
        public string NotLabelClass
        {
            get { return _NotLabelClass; }
            set { _NotLabelClass = value; }
        }


        bool _OnlyPage = true;
        /// <summary>
        /// 只有一页是否显示
        /// </summary>
        public bool OnlyPage
        {
            get { return _OnlyPage; }
            set { _OnlyPage = value; }
        }


        string _FormId = "from0";
        /// <summary>
        /// 表单ID
        /// </summary>
        public string FormId
        {
            get { return _FormId; }
            set { _FormId = value; }
        }


        bool _IsPost = true;
        /// <summary>
        /// 提交方式
        /// true==Post   false==get
        /// </summary>
        public bool IsPost
        {
            get { return _IsPost; }
            set { _IsPost = value; }
        }


        int _PageNum = 5;
        /// <summary>
        /// 页码显示数量
        /// </summary>
        public int PageNum
        {
            get { return _PageNum; }
            set { _PageNum = value; }
        }
        #endregion
    }
}
