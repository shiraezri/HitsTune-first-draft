using System;
using System.Collections.Generic;
using HitsTune_first_draft.Models;

namespace HitsTune_first_draft.Services
{
    public class SongService
    {
        private readonly List<Song> _songs = new List<Song>
        {
            new Song { Id = "1", Title = "Bohemian Rhapsody", Artist = "Queen", ReleaseYear = 1975, AudioUrl = "" },
            new Song { Id = "2", Title = "Billie Jean", Artist = "Michael Jackson", ReleaseYear = 1982, AudioUrl = "" },
            new Song { Id = "3", Title = "Smells Like Teen Spirit", Artist = "Nirvana", ReleaseYear = 1991, AudioUrl = "" },
            new Song { Id = "4", Title = "Shape of You", Artist = "Ed Sheeran", ReleaseYear = 2017, AudioUrl = "" }
        };

        public Song GetRandomSong()
        {
            var random = new Random();
            int index = random.Next(_songs.Count);
            return _songs[index];
        }
    }
}