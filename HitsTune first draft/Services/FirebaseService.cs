using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;
using HitsTune_first_draft.Models;

namespace HitsTune_first_draft.Services
{
    public class FirebaseService
    {
        private readonly FirebaseClient _firebase = new FirebaseClient("https://hitstune-7bca7-default-rtdb.firebaseio.com/");

        public async Task CreateRoomAsync(GameRoom room)
        {
            await _firebase.Child("GameRooms").Child(room.RoomId).PutAsync(room);
        }

        public async Task<GameRoom> GetRoomAsync(string roomId)
        {
            return await _firebase.Child("GameRooms").Child(roomId).OnceSingleAsync<GameRoom>();
        }

        public async Task UpdateRoomAsync(GameRoom room)
        {
            await _firebase.Child("GameRooms").Child(room.RoomId).PutAsync(room);
        }

        public IObservable<FirebaseObject<GameRoom>> ListenToRoom(string roomId)
        {
            return _firebase.Child("GameRooms").Child(roomId).AsObservable<GameRoom>();
        }
    }
}