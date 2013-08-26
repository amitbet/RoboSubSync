using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace SyncSubsByComparison
{
    public class LineInfo
    {
        public string Line { get; set; }
        public string TranslatedLine { get; set; }
        public TimeStamp TimeStamp { get; set; }
        //public int LineNumber { get; set; }
        Dictionary<string, int> _triGrams = null;
        public Dictionary<string, int> TriGrams
        {
            get
            {
                if (_triGrams == null)
                    _triGrams = GetStringTrigrams(Line);
                return _triGrams;
            }
        }


        public int LineNumberInSub { get { return TimeStamp.Lines.IndexOf(this); } }

        public override string ToString()
        {
            return Line;
        }

        public bool CalcIsTrigramMatch(LineInfo other)
        {
            return (CheckTrigramContainment(TriGrams, other.TriGrams) ||
            CheckTrigramContainment(other.TriGrams, TriGrams));
        }

        public bool CalcIsWordMatch(LineInfo other, int minimumLettersForContainment, double orderedLetterSimilarityTreshold)
        {
            string myLine = (TranslatedLine != null) ? TranslatedLine : Line;
            string otherLine = (other.TranslatedLine != null) ? other.TranslatedLine : other.Line;
            return (CheckWordLettersContainment(myLine, otherLine, minimumLettersForContainment, orderedLetterSimilarityTreshold) ||
            CheckWordLettersContainment(otherLine, myLine, minimumLettersForContainment, orderedLetterSimilarityTreshold));
        }

        bool CheckWordLettersContainment(string contained, string container, int minimumLettersForContainment, double orderedLetterSimilarityTreshold)
        {
            int lettersContained = 0;
            int totalLetters = 0;
            string[] containedWords = contained.ToLower().Split(" .,';?!#$%^&*()@><{}[]".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            //string[] containerWords = container.ToLower().Split(" .,';?!#$%^&*()@><{}[]".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            HashSet<string> containerWordsHash = new HashSet<string>();
            //foreach (string word in containerWords)
            //    containerWordsHash.Add(word);

            foreach (string word in containedWords)
            {
                if (word.Length < 2)
                    continue;

                totalLetters += word.Length;
                if (container.ToLower().Contains(word))
                    lettersContained += word.Length;
            }
            if (totalLetters < minimumLettersForContainment) return false;

            double precentage = (double)lettersContained / (double)totalLetters;
            return (precentage > orderedLetterSimilarityTreshold);
        }

        bool CheckTrigramContainment(Dictionary<string, int> contained, Dictionary<string, int> container)
        {
            int numContained = 0;
            foreach (string feature in contained.Keys)
            {
                if (container.ContainsKey(feature))
                {
                    ++numContained;
                }
            }
            double precentage = (double)numContained / (double)contained.Keys.Count();
            return (precentage > Contants.TrigramSimilarityTreshold);
        }


        //Dictionary<string, int> TriGrams = null;
        public static Dictionary<string, int> GetStringTrigrams(string str)
        {
            Dictionary<string, int> trigrams = new Dictionary<string, int>();
            for (int i = 0; i < str.Length - 3; ++i)
            {
                string trigram = str.Substring(i, 3);
                if (trigrams.ContainsKey(trigram))
                    ++trigrams[trigram];
                else
                    trigrams.Add(trigram, 1);
            }

            //cutoff the best 100
            List<KeyValuePair<string, int>> order = new List<KeyValuePair<string, int>>();
            foreach (string trig in trigrams.Keys)
            {
                order.Add(new KeyValuePair<string, int>(trig, trigrams[trig]));
            }
            order.Sort((Comparison<KeyValuePair<string, int>>)delegate(KeyValuePair<string, int> a, KeyValuePair<string, int> b)
            {
                return a.Value.CompareTo(b.Value);
            });

            if (trigrams.Count > 100)
                for (int i = 0; i < order.Count; ++i)
                {
                    trigrams.Remove(order[i].Key);
                    if (trigrams.Count <= 100)
                        break;
                }
            return trigrams;
        }

    }
}
