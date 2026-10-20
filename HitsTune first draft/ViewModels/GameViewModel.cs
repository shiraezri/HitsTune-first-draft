using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using HitsTune_first_draft.Models;
using HitsTune_first_draft.Services;

namespace HitsTune_first_draft.ViewModels
{
    public class GameViewModel : INotifyPropertyChanged
    {
        private readonly FirebaseService _firebaseService;
        private readonly SongService _songService;
        private GameRoom _currentRoom;
        private string _inputRoomId;
        private string _guessedYear;
        private string _feedbackMessage;
        private string _myPlayerId;
        private IDispatcherTimer _timer;

        public GameRoom CurrentRoom
        {
            get => _currentRoom;
            set
            {
                _currentRoom = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMyTurn));
                OnPropertyChanged(nameof(TurnStatusMessage));
            }
        }

        public string InputRoomId
        {
            get => _inputRoomId;
            set
            {
                _inputRoomId = value;
                OnPropertyChanged();
            }
        }

        public string GuessedYear
        {
            get => _guessedYear;
            set
            {
                _guessedYear = value;
                OnPropertyChanged();
            }
        }

        public string FeedbackMessage
        {
            get => _feedbackMessage;
            set
            {
                _feedbackMessage = value;
                OnPropertyChanged();
            }
        }

        public string MyPlayerId
        {
            get => _myPlayerId;
            set
            {
                _myPlayerId = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsMyTurn));
                OnPropertyChanged(nameof(TurnStatusMessage));
            }
        }

        public bool IsMyTurn => CurrentRoom != null && CurrentRoom.CurrentTurnPlayerId == MyPlayerId;

        public string TurnStatusMessage
        {
            get
            {
                if (CurrentRoom?.Player2 == null)
                    return "ממתין להצטרפות שחקן 2...";

                return IsMyTurn ? "זה התור שלך!" : "ממתין לתור של השחקן השני...";
            }
        }

        public ICommand CreateRoomCommand { get; }
        public ICommand JoinRoomCommand { get; }
        public ICommand SubmitGuessCommand { get; }

        public GameViewModel()
        {
            _firebaseService = new FirebaseService();
            _songService = new SongService();

            CreateRoomCommand = new Command(async () => await CreateNewRoomAsync());
            JoinRoomCommand = new Command(async () => await JoinRoomAsync());
            SubmitGuessCommand = new Command(async () => await SubmitGuessAsync());
        }

        private async Task CreateNewRoomAsync()
        {
            MyPlayerId = "P1";
            var hostPlayer = new Player { Id = MyPlayerId, Name = "Player 1", Score = 0 };

            // הוספת שיר ראשון רנדומלי לציר הזמן של שחקן 1
            hostPlayer.Timeline.Add(_songService.GetRandomSong());

            var firstSong = _songService.GetRandomSong();

            var newRoom = new GameRoom
            {
                RoomId = new Random().Next(1000, 9999).ToString(),
                Status = "Waiting",
                Player1 = hostPlayer,
                CurrentTurnPlayerId = hostPlayer.Id,
                CurrentSong = firstSong,
                TimerSeconds = 30
            };

            await _firebaseService.CreateRoomAsync(newRoom);
            CurrentRoom = newRoom;
            StartListeningToRoomUpdates(newRoom.RoomId);
            StartTimer();
        }

        private async Task JoinRoomAsync()
        {
            if (string.IsNullOrWhiteSpace(InputRoomId)) return;

            var existingRoom = await _firebaseService.GetRoomAsync(InputRoomId);
            if (existingRoom != null)
            {
                MyPlayerId = "P2";
                var guestPlayer = new Player { Id = MyPlayerId, Name = "Player 2", Score = 0 };

                // הוספת שיר ראשון רנדומלי לציר הזמן של שחקן 2
                guestPlayer.Timeline.Add(_songService.GetRandomSong());

                existingRoom.Player2 = guestPlayer;
                existingRoom.Status = "Playing";

                await _firebaseService.UpdateRoomAsync(existingRoom);
                CurrentRoom = existingRoom;
                StartListeningToRoomUpdates(existingRoom.RoomId);
            }
        }

        private async Task SubmitGuessAsync()
        {
            if (!IsMyTurn || CurrentRoom?.CurrentSong == null || string.IsNullOrWhiteSpace(GuessedYear)) return;

            if (int.TryParse(GuessedYear, out int yearGuess))
            {
                var currentPlayer = MyPlayerId == "P1" ? CurrentRoom.Player1 : CurrentRoom.Player2;

                if (yearGuess == CurrentRoom.CurrentSong.ReleaseYear)
                {
                    FeedbackMessage = "תשובה נכונה! השיר התווסף לציר הזמן 🎉";

                    if (currentPlayer != null)
                    {
                        currentPlayer.Score += 100;
                        // הוספת השיר לציר הזמן של השחקן
                        currentPlayer.Timeline.Add(CurrentRoom.CurrentSong);
                    }
                }
                else
                {
                    FeedbackMessage = $"טעות! השיר יצא בשנת {CurrentRoom.CurrentSong.ReleaseYear}";
                }

                SwitchTurn();
                CurrentRoom.CurrentSong = _songService.GetRandomSong();
                CurrentRoom.TimerSeconds = 30;
                GuessedYear = string.Empty;

                // עדכון המסך המקומי ושל השרת
                OnPropertyChanged(nameof(CurrentRoom));
                await _firebaseService.UpdateRoomAsync(CurrentRoom);
            }
        }

        private void SwitchTurn()
        {
            if (CurrentRoom == null) return;

            // התור יעבור ל-P2 רק אם שחקן 2 כבר הצטרף לחדר
            if (CurrentRoom.Player2 != null)
            {
                CurrentRoom.CurrentTurnPlayerId = CurrentRoom.CurrentTurnPlayerId == "P1" ? "P2" : "P1";
            }
        }

        private void StartTimer()
        {
            _timer?.Stop();
            _timer = Application.Current.Dispatcher.CreateTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += async (s, e) =>
            {
                if (MyPlayerId == "P1" && CurrentRoom != null && CurrentRoom.TimerSeconds > 0)
                {
                    CurrentRoom.TimerSeconds--;
                    OnPropertyChanged(nameof(CurrentRoom));

                    if (CurrentRoom.TimerSeconds == 0)
                    {
                        FeedbackMessage = "נגמר הזמן! השיר מתחלף.";
                        SwitchTurn();
                        CurrentRoom.CurrentSong = _songService.GetRandomSong();
                        CurrentRoom.TimerSeconds = 30;
                    }

                    await _firebaseService.UpdateRoomAsync(CurrentRoom);
                }
            };
            _timer.Start();
        }

        private void StartListeningToRoomUpdates(string roomId)
        {
            _firebaseService.ListenToRoom(roomId).Subscribe(updatedRoom =>
            {
                if (updatedRoom?.Object != null)
                {
                    CurrentRoom = updatedRoom.Object;
                }
            });
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}