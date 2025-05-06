using System;

namespace AIText.Models.PromptTemplate
{
    public class PromptTemplateDto
    {
        public System.String Id { get; set; }

        public System.String Name { get; set; }

        public System.String Prompt { get; set; }

        public bool? IsEnable { get; set; }

        public DateTime? CreateTime { get; set; }

        public DateTime? UpdateTime { get; set; }
    }
}
