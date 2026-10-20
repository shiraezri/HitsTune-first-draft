using System;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using HitsTune_first_draft.Models;

namespace HitsTune_first_draft.Services
{
    public class FirebaseService
    {
        private const string FirebaseBaseUrl = "https://hitstune-7bca7-default-rtdb.firebaseio.com/";
        private readonly FirebaseClient _firebaseClient;

        public FirebaseService()
        {
            _firebaseClient = new FirebaseClient(FirebaseBaseUrl);
        }

        // יצירת חדר משחק חדש
        public async Task CreateRoomAsync(GameRoom room)
        {
            await _firebaseClient
                .Child("GameRooms")
                .Child(room.RoomId)
                .PutAsync(room);
        }

        // עדכון מצב חדר המשחק בזמן אמת
        public async Task UpdateRoomAsync(GameRoom room)
        {
            await _firebaseClient
                .Child("GameRooms")
                .Child(room.RoomId)
                .PutAsync(room);
        }

        // קבלת נתוני חדר לפי קוד חדר
        public async Task<GameRoom> GetRoomAsync(string roomId)
        {
            return await _firebaseClient
                .Child("GameRooms")
                .Child(roomId)
                .OnceSingleAsync<GameRoom>();
        }

        // האזנה לשינויים בחדר משחק בזמן אמת (Realtime Listener)
        public IObservable<FirebaseObject<GameRoom>> ListenToRoom(string roomId)
        {
            return _firebaseClient
                .Child("GameRooms")
                .Child(roomId)
                .AsObservable<GameRoom>();
        }
    }
}