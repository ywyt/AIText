using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AIText.Controllers
{
    public class ReturnValue<T>
    {

        string _ErrorDetailed;

        /// <summary>
        /// 错误详细消息
        /// </summary>
        public string errordetailed
        {
            get { return _ErrorDetailed; }
            set { _ErrorDetailed = value; }
        }

        string _ErrorSimple;
        /// <summary>
        /// 错误消息
        /// </summary>
        public string errorsimple
        {
            get { return _ErrorSimple; }
            set { _ErrorSimple = value; }
        }

        T _Value;
        /// <summary>
        /// 返回数据
        /// </summary>
        public T value
        {
            get { return _Value; }
            set { _Value = value; }
        }

        bool _Status = false;
        /// <summary>
        /// 返回值状态
        /// </summary>
        public bool status
        {
            get { return _Status; }
            set { _Status = value; }
        }

        /// <summary>
        /// 正确
        /// </summary>
        /// <param name="_Value"></param>
        public void True(T _Value)
        {
            status = true;
            value = _Value;
        }

        /// <summary>
        /// 错误
        /// </summary>
        /// <param name="_ErrorSimple">简单说明</param>
        /// <param name="_ErrorDetailed">详细说明（一般异常用）</param>
        public void False(string _ErrorSimple = "", string _ErrorDetailed = "")
        {
            status = false;
            errorsimple = _ErrorSimple;
            errordetailed = _ErrorDetailed;
            if (errordetailed.Length == 0)
            {
                errordetailed = errorsimple;
            }
        }

    }
}
