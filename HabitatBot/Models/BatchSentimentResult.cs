using System.Collections.Generic;

namespace HabitatBot.Models
{
    public class BatchSentimentResult
    {
        public List<DocumentSentimentResult> Documents { get; set; }
        public List<object> Errors { get; set; }
    }
}