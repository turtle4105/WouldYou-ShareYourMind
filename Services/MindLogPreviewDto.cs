using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WouldYou_ShareMind.Services
{
    public sealed class MindLogPreviewDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = "";
        public string? AiReply { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
