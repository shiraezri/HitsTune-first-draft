using System;
using System.Collections.ObjectModel;
using HitsTune_first_draft.Models;
using HitsTune_first_draft.Services;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;

namespace HitsTune_first_draft
{
    public class PlayerItem
    {
        public string Name { get; set; }
        public string RoleText { get; set; }
    }

    public partial class WaitingLobbyPage : ContentPage
    {
        public ObservableCollection<PlayerItem> Players { get; set; } = new ObservableCollection<PlayerItem>();
        private string _roomCode;
        private bool _isHost;
        private readonly FirebaseService _firebaseService;
        private IDisposable _roomSubscription;

        public WaitingLobbyPage(string roomCode, bool isHost)
        {
            InitializeComponent();
            _roomCode = roomCode;
            _isHost = isHost;
            _firebaseService = new FirebaseService();

            RoomCodeLabel.Text = _roomCode;
            PlayersCollectionView.ItemsSource = Players;

            StartGameButton.IsVisible = _isHost;

            // אתחול החדר ב-Firebase והפעלת האזנה בזמן אמת
            InitRoomAndListen();
        }

        private async void InitRoomAndListen()
        {
            string currentPlayerName = AuthService.CurrentPlayerName ?? "שחקן";

            if (_isHost)
            {
                // יצירת חדר חדש ב-Firebase במידה והוא עדיין לא קיים
                var hostPlayer = new Player { Id = "P1", Name = currentPlayerName, Score = 0 };
                var songService = new SongService();
                hostPlayer.Timeline.Add(songService.GetRandomSong());

                var newRoom = new GameRoom
                {
                    RoomId = _roomCode,
                    Status = "Waiting",
                    Player1 = hostPlayer,
                    CurrentTurnPlayerId = "P1",
                    CurrentSong = songService.GetRandomSong(),
                    TimerSeconds = 30
                };

                await _firebaseService.CreateRoomAsync(newRoom);
            }
            else
            {
                // הצטרפות שחקן 2 לחדר קיים
                var existingRoom = await _firebaseService.GetRoomAsync(_roomCode);
                if (existingRoom != null)
                {
                    var guestPlayer = new Player { Id = "P2", Name = currentPlayerName, Score = 0 };
                    var songService = new SongService();
                    guestPlayer.Timeline.Add(songService.GetRandomSong());

                    existingRoom.Player2 = guestPlayer;
                    await _firebaseService.UpdateRoomAsync(existingRoom);
                }
            }

            // האזנה בזמן אמת לשינויים בחדר (סנכרון שחקנים ומעבר למשחק)
            _roomSubscription = _firebaseService.ListenToRoom(_roomCode).Subscribe(roomObj =>
            {
                if (roomObj?.Object == null) return;

                var room = roomObj.Object;

                MainThread.BeginInvokeOnMainThread(async () =>
                {
                    // 1. עדכון רשימת השחקנים ב-UI
                    Players.Clear();
                    if (room.Player1 != null)
                        Players.Add(new PlayerItem { Name = room.Player1.Name, RoleText = "👑 מנהל החדר" });
                    if (room.Player2 != null)
                        Players.Add(new PlayerItem { Name = room.Player2.Name, RoleText = "שחקן" });

                    UpdatePlayerCount();

                    // 2. מעבר אוטומטי למסך המשחק כשהסטטוס משתנה ל-Playing
                    if (room.Status == "Playing")
                    {
                        _roomSubscription?.Dispose();
                        await Shell.Current.GoToAsync($"//GamePage?roomId={_roomCode}&playerId={(_isHost ? "P1" : "P2")}");
                    }
                });
            });
        }

        private void UpdatePlayerCount()
        {
            PlayerCountLabel.Text = $"{Players.Count}/8";
        }

        private async void OnCopyCodeClicked(object sender, EventArgs e)
        {
            await Clipboard.Default.SetTextAsync(_roomCode);
            await DisplayAlert("הועתק!", "קוד החדר הועתק ללוח", "אישור");
        }

        private async void OnStartGameClicked(object sender, EventArgs e)
        {
            try
            {
                StartGameButton.IsEnabled = false; // מניעת לחיצות כפולות

                var room = await _firebaseService.GetRoomAsync(_roomCode);
                if (room != null)
                {
                    room.Status = "Playing";
                    await _firebaseService.UpdateRoomAsync(room);
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("שגיאה", "לא ניתן להתחיל את המשחק: " + ex.Message, "אישור");
                StartGameButton.IsEnabled = true;
            }
        }

        private async void OnLeaveRoomClicked(object sender, EventArgs e)
        {
            _roomSubscription?.Dispose();
            await Shell.Current.GoToAsync("//MainPage");
        }
    }
}