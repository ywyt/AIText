using System;

namespace AIText.Models.ImageResource
{
    public class ImageResourceDto
    {
        /// <summary>
        /// ID
        /// </summary>
        public string Id { get; set; }
        /// <summary>
        /// 文件夹 => 款式
        /// </summary>
        public string Style { get; set; }
        /// <summary>
        /// 文件夹 => 按颜色分组
        /// </summary>
        public string Color { get; set; }
        /// <summary>
        /// 图片路径
        /// </summary>
        public string ImagePath { get; set; }
        /// <summary>
        /// 使用次数
        /// </summary>
        public int UseCount { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreateTime { get; set; }

    }
}
