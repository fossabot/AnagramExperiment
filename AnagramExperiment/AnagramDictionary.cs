﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnagramExperiment
{
    public class AnagramDictionary
    {
        private readonly ConcurrentDictionary<string, HashSet<string>> _anagrams;
        private readonly TaskFactory _taskFactory;
        private readonly BlockingCollection<string> _words;

        public AnagramDictionary(string path)
        {
            var fileInfo = GetValidatedPath(path);

            _taskFactory = new TaskFactory(TaskCreationOptions.LongRunning, TaskContinuationOptions.None);
            _anagrams = new ConcurrentDictionary<string, HashSet<string>>();
            _words = new BlockingCollection<string>();

            FillDictionary(fileInfo.FullName);
        }

        public void Add(string word)
        {
            var sortedWord = SortByCharacters(word);

            if (_anagrams.ContainsKey(sortedWord))
                _anagrams[sortedWord].Add(word);
            else
                _anagrams[sortedWord] = new HashSet<string> {word};
        }

        public HashSet<string> LookUpWord(string word)
        {
            var sortedWord = SortByCharacters(word);

            _anagrams.TryGetValue(sortedWord, out var result);
            result?.Remove(word);

            return result ?? new HashSet<string>();
        }

        public int GetDictionaryCount()
        {
            return _anagrams.Count;
        }

        public int GetWordCount()
        {
            return _words.Count;
        }

        private static string SortByCharacters(string word)
        {
            var res = word.ToLowerInvariant().ToCharArray();
            Array.Sort(res);
            return new string(res);
        }

        private static FileInfo GetValidatedPath(string path)
        {
            if (!new FileInfo(path).Exists) throw new FileNotFoundException("File not found.");
            return new FileInfo(path);
        }

        private void FillDictionary(string path)
        {
            var readTask = _taskFactory.StartNew(() =>
                Parallel.ForEach(File.ReadLines(path, Encoding.UTF8).AsParallel(), _words.Add));
            Task.WaitAll(readTask);
            _words.CompleteAdding();

            var fillTask = _taskFactory.StartNew(() => Parallel.ForEach(_words, Add));
            Task.WaitAll(fillTask);
        }
    }
}