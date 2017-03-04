namespace HabitatBot.Models
{
    internal class HabitatLuis
    {
        public string Query { get; set; }
        public LuisIntent[] Intents { get; set; }
        public LuisEntity[] Entities { get; set; }
        public LuisTopScoringIntent TopScoringIntent { get; set; }
    }
}