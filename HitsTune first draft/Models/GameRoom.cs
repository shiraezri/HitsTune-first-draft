using System.Collections.Generic;

namespace HitsTune_first_draft.Models
{
    public class GameRoom
    {
        public string RoomId { get; set; }
        public Player Player1 { get; set; }
        public Player Player2 { get; set; }
        public string CurrentTurnPlayerId { get; set; }
        public Song CurrentSong { get; set; }
        public string Status { get; set; } = "Waiting"; // Waiting, Playing, Finished
        public int TimerSeconds { get; set; } = 30;
    }
}