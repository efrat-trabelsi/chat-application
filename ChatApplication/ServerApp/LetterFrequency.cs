using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ServerApp
{
    internal class LetterFrequency
    {
        private readonly Dictionary<char, int> _letterCounts = new();
        private readonly HashSet<char> _seenLetters = new();
        private readonly object _lock = new();

        public void AddMessage(string message)
        {
            lock (_lock)
            {
                foreach (char c in message)
                {
                    if (char.IsLetter(c))
                    {
                        char lower = char.ToLower(c);

                        if (_letterCounts.ContainsKey(lower))
                            _letterCounts[lower]++;
                        else
                            _letterCounts[lower] = 1;

                        _seenLetters.Add(lower);
                    }
                }
            }
        }

        public void PrintSummary()
        {
            lock (_lock)
            {
                Console.WriteLine("-- Letters summary:");
                foreach (var kvp in _letterCounts)
                {
                    if (_seenLetters.Contains(kvp.Key))
                        Console.WriteLine($"{kvp.Key}: {kvp.Value}");
                }
                Console.WriteLine();
            }
        }
    }
}
