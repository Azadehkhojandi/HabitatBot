using System.Collections.Generic;

namespace HabitatBot.Models
{
    public class BatchKeyPhrasesResul
    {
        public List<DocumentKeyPhrasesResult> Documents { get; set; }
        public List<object> Errors { get; set; }
    }
}