using System;

namespace AIText.Models.SendReview
{
    public class AiReviewDto
    {
        public int Id { get; set; }

        public int SiteProductId { get; set; }
        public string Site { get; set; }
        public string ProductName { get; set; }
        public string Permalink { get; set; }

        public System.String ErrMsg { get; set; }

        public DateTime CreateTime { get; set; }
    }
}
