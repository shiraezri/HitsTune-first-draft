using System;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using HitsTune_first_draft.Models;
using HitsTune_first_draft.Services;

namespace HitsTune_first_draft.ViewModels
{
    [QueryProperty(nameof(RoomIdParam), "roomId")]
    [QueryProperty(nameof(PlayerIdParam), "playerId")]
    public class GameViewModel : INotifyPropertyChanged
    {
        private readonly FirebaseService _firebaseService;
        private readonly SongService _songService;
        private GameRoom _currentRoom;
        private string _guessedYear;
        private string _feedbackMessage;
        private string _myPlayerId;
        private IDispatcherTimer _timer;
        private IDisposable _roomSubscription;

        public string RoomIdParam
        {
            set
            {
                if (!string.IsNullOrEmpty(value))
                {
                    LoadAndListenToRoom(value);
                }
            }
        }

        public string PlayerIdParam
        {
            set => MyPlayerId = value;
        }

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

        public string GuessedYear
        {
            get => _guessedYear;
            set { _guessedYear = value; OnPropertyChanged(); }
        }

        public string FeedbackMessage
        {
            get => _feedbackMessage;
            set { _feedbackMessage = value; OnPropertyChanged(); }
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

        public ICommand SubmitGuessCommand { get; }

        public GameViewModel()
        {
            _firebaseService = new FirebaseService();
            _songService = new SongService();
            SubmitGuessCommand = new Command(async () => await SubmitGuessAsync());
        }

        private async void LoadAndListenToRoom(string roomId)
        {
            CurrentRoom = await _firebaseService.GetRoomAsync(roomId);
            StartListeningToRoomUpdates(roomId);
            if (MyPlayerId == "P1")
            {
                StartTimer();
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

                OnPropertyChanged(nameof(CurrentRoom));
                await _firebaseService.UpdateRoomAsync(CurrentRoom);
            }
        }

        private void SwitchTurn()
        {
            if (CurrentRoom?.Player2 != null)
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

                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        OnPropertyChanged(nameof(CurrentRoom));
                    });

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
            _roomSubscription?.Dispose();
            _roomSubscription = _firebaseService.ListenToRoom(roomId).Subscribe(updatedRoom =>
            {
                if (updatedRoom?.Object != null)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        CurrentRoom = updatedRoom.Object;
                    });
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