using System.Collections.ObjectModel;

namespace HitsTune_first_draft.Models
{
    public class Player
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
        public int Coins { get; set; }
        public ObservableCollection<Song> Timeline { get; set; } = new ObservableCollection<Song>();
    }
}