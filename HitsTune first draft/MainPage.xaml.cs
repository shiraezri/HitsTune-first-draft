using System;
using HitsTune_first_draft.Models;
using HitsTune_first_draft.Services;
using Microsoft.Maui.Controls;

namespace HitsTune_first_draft
{
    public partial class MainPage : ContentPage
    {
        private int _playerTrophies = 350;
        private readonly FirebaseService _firebaseService;

        public MainPage()
        {
            InitializeComponent();
            _firebaseService = new FirebaseService();
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            string playerName = AuthService.CurrentPlayerName;
            PlayerNameLabel.Text = !string.IsNullOrEmpty(playerName) ? playerName : "שחקן";
            TrophiesLabel.Text = _playerTrophies.ToString("N0");
        }

        private async void OnCreateRoomClicked(object sender, EventArgs e)
        {
            // 1. יצירת קוד חדר אקראי בן 4 ספרות/אותיות
            string newRoomCode = Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
            string currentPlayerName = AuthService.CurrentPlayerName ?? "מנהל החדר";

            // 2. יצירת אובייקט חדר חדש ושמירתו ב-Firebase
            var hostPlayer = new Player { Id = "P1", Name = currentPlayerName, Score = 0 };
            var songService = new SongService();
            hostPlayer.Timeline.Add(songService.GetRandomSong());

            var newRoom = new GameRoom
            {
                RoomId = newRoomCode,
                Status = "Waiting",
                Player1 = hostPlayer,
                CurrentTurnPlayerId = "P1",
                CurrentSong = songService.GetRandomSong(),
                TimerSeconds = 30
            };

            // שמירת החדר במסד הנתונים
            await _firebaseService.CreateRoomAsync(newRoom);

            // 3. מעבר ללובי ההמתנה בתור host
            await Navigation.PushAsync(new WaitingLobbyPage(newRoomCode, true));
        }

        private async void OnJoinRoomClicked(object sender, EventArgs e)
        {
            string roomCode = RoomCodeEntry.Text?.Trim().ToUpper();

            if (string.IsNullOrEmpty(roomCode) || roomCode.Length < 4)
            {
                await DisplayAlert("שגיאה", "נא להזין קוד חדר תקין בן 4 תווים לפחות", "אישור");
                return;
            }

            // בדיקה שהחדר אכן קיים ב-Firebase לפני מעבר
            var room = await _firebaseService.GetRoomAsync(roomCode);
            if (room == null)
            {
                await DisplayAlert("שגיאה", "החדר לא נמצא, ודא שהקוד נכון", "אישור");
                return;
            }

            // מעבר ללובי ההמתנה בתור שחקן 2
            await Navigation.PushAsync(new WaitingLobbyPage(roomCode, false));
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            AuthService.CurrentPlayerName = null;
            await Shell.Current.GoToAsync("//LoginPage");
        }
    }
}