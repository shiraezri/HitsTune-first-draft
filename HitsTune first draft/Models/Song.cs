namespace HitsTune_first_draft.Models
{
    public class Song
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Artist { get; set; }
        public int ReleaseYear { get; set; }
        public string AudioUrl { get; set; }
    }
}