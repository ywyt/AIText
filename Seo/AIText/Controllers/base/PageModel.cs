using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace commons.util
{
    /// <summary>
    /// 分页方法
    /// </summary>
    public class PageModel
    {
        private int _PageIndex = 1;

        public int PageIndex
        {
            get { return _PageIndex; }
            set { _PageIndex = value; }
        }

        private int _PageSize = 36;

        /// <summary>
        /// 每页数量
        /// </summary>
        public int PageSize
        {
            get { return _PageSize; }
            set { _PageSize = value; }
        }


        /// <summary>
        /// 页数
        /// </summary>
        public int PageCount
        {
            get
            {
                if (Count == 0)
                {
                    return 0;
                }
                return Count / PageSize + (Count % PageSize == 0 ? 0 : 1);
            }
        }



        private int _Count = 0;

        /// <summary>
        /// 总记录
        /// </summary>
        public int Count
        {
            get { return _Count; }
            set { _Count = value; }
        }

    }
}
