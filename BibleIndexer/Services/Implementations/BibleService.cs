﻿using BibleIndexer.Data;
using BibleIndexer.Models.DTOs.Request;
using BibleIndexer.Models.DTOs.Response;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace BibleIndexer.Services.Implementations
{
    public class BibleService 
    {
        const int first = 1;
        const int bibleBookCount = 66;
        private static IEnumerable<dynamic>? _bibleBlob;

        ///<Summary>Get the chapters in a book of the bible using the full name of the boo or via the book abbreviation. This will also return a cascading dropdown for all chapters in specified book</Summary>        
        public static async Task<ChaptersResponse?> GetChaptersInABookOfTheBible(string name)
        {
            BlobResponse? bibleResult = await GetBookOfTheBible(name);
            if (bibleResult is null) return null;
            int count = 0;

            List<List<string>>? bookChapters = JsonConvert.DeserializeObject<List<List<string>>>(Convert.ToString(bibleResult.Chapters));
            IEnumerable<dynamic>? chaptersDropdown = bookChapters?.Select(x => new { Id = count += 1 });

            return new ChaptersResponse()
            {
                DropDown = chaptersDropdown ?? Enumerable.Empty<dynamic>(),
                BookName = bibleResult?.Name ?? string.Empty,
                Resource = bookChapters ?? Enumerable.Empty<List<string>>(),
            };
        }

        ///<Summary>Generate a random bible verse</Summary>
        public static async Task<BibleVerseResponse?> GenerateRandomBibleVerse()
        {
            int randomBibleBookIndex = RandomNumberGenerator.GetInt32(bibleBookCount);
            
            IEnumerable<dynamic>? bibleBlob = await GetBlob();
            if (bibleBlob is null || !bibleBlob.Any()) return null;

            var book = bibleBlob.ElementAt(randomBibleBookIndex);
            var bookName = book.Name;
            var chapterCount = book.Chapters.Count;
            
            int randomBibleChapterIndex = RandomNumberGenerator.GetInt32(chapterCount);            
            var chapter = JsonConvert.DeserializeObject<List<List<string>>>(Convert.ToString(book.Chapters));

            var verseCount = chapter.Count;
            var randomVerseIndex = RandomNumberGenerator.GetInt32(verseCount);
            return await GetBibleVerse(new GetBibleVerseRequest() { BookNameInFull = bookName, ChapterNumber = randomBibleChapterIndex, VerseNumber = randomVerseIndex });
        }

        ///<Summary>Gets all books of the bible together with their abbreviations</Summary>
        public static async Task<object> GetAllBooksOfTheBible()
        {
            IEnumerable<dynamic>? bibleBlob = await GetBlob();

            if (bibleBlob is null) return bibleBlob;
            return bibleBlob.Select(bibleBlob => new
            {
                Id = bibleBlob.Abbreviation,
                Book = bibleBlob.Name,
            });
        }

        ///<Summary>Get all verses in a chapter of the bible. This will also return a cascading dropdown for all verses in specified chapter</Summary>
        public static async Task<VersesResponse?> GetAllVersesInAChapterOFTheBible(GetBibleVerseRequest request)
        {
            BlobResponse? bookResponse = await GetBookOfTheBible(request.BookNameInFull);
            if (bookResponse is null) return null;
            int count = 0;
            IEnumerable<string> verses = bookResponse.Chapters.ElementAt(request.ChapterNumber - first);
            IEnumerable<dynamic> versesDropdown = verses.Any() ? verses.Select(x => new { Id = count += 1 }) : Enumerable.Empty<dynamic>();

            return new VersesResponse()
            {
                DropDown = versesDropdown,
                BookName = bookResponse?.Name,
                Resource = verses,
            };
        }

        ///<Summary>Get a book of bible</Summary>
        public static async Task<BlobResponse?> GetBookOfTheBible(string bookName)
        {
            IEnumerable<dynamic>? bibleBlob = await GetBlob();

            if (bibleBlob is null || !bibleBlob.Any()) return null;
            dynamic? result = bibleBlob.FirstOrDefault(x => Convert.ToString(x.Name).ToLower() == bookName.ToLower().Trim()
            || Convert.ToString(x.Abbreviation).ToLower() == bookName.ToLower().Trim());

            if (result is null) return null;
            return JsonConvert.DeserializeObject<BlobResponse>(Convert.ToString(result));
        }

        ///<Summary>Get a bible verse</Summary>
        public static async Task<BibleVerseResponse?> GetBibleVerse(GetBibleVerseRequest request)
        {
            BlobResponse? bookResponse = await GetBookOfTheBible(request.BookNameInFull);
            if (bookResponse is null) return null;

            return new BibleVerseResponse()
            {
                BookName = request.BookNameInFull,
                ChapterNumber = request.ChapterNumber,
                VerseNumber = request.VerseNumber,
                VerseContent = bookResponse.Chapters[request.ChapterNumber - first][request.VerseNumber - first]
            };
        }

        /// <Summary>Get all occurences of a word in the bible</Summary>
        public static async Task<IEnumerable<BibleVerseResponse>> SearchBible(string query)
        {
            query = string.IsNullOrEmpty(query) ? throw new InvalidOperationException("Invalid query") : query.ToLower().Trim();

            IEnumerable<dynamic> bibleBlob = await GetBlob();

            if (!bibleBlob.Any()) return Enumerable.Empty<BibleVerseResponse>();

            List<BibleVerseResponse> result = new List<BibleVerseResponse>();

            Parallel.ForEach(bibleBlob, (blob, state, index) =>
            {
                BlobResponse deserializedBlob = JsonConvert.DeserializeObject<BlobResponse>(Convert.ToString(blob));

                Parallel.ForEach(deserializedBlob.Chapters, (chapter, state, chapterIndex) =>
                {
                    Parallel.ForEach(chapter, (verse, state, verseIndex) =>
                    {
                        if (verse.ToLower().Contains(query))
                        {
                            result.Add(new BibleVerseResponse()
                            {
                                BookName = deserializedBlob.Name,
                                BookNumber = (int)index + first,
                                ChapterNumber = (int)chapterIndex + first,
                                VerseNumber = (int)verseIndex + first,
                                VerseContent = verse
                            });
                        }
                    });
                });

            });

            return result;

        }
        private static async Task<IEnumerable<dynamic>?> GetBlob() => _bibleBlob = _bibleBlob == null ? await Api.GetBibleBlob() : _bibleBlob ?? null;
    }
}
