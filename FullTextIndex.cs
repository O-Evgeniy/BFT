using System;
using System.Collections.Generic;
using BFTIndex.Models;
using System.Text.RegularExpressions;
using System.Linq;
using System.Text;


namespace BFTIndex
{
    public class FullTextIndex : IFullTextIndex
    {
        Dictionary<string, string> documentDictionary = new Dictionary<string, string>();
        Dictionary<char, char> normalizationTable = new Dictionary<char, char>();
        List<MatchedDocument> list = new List<MatchedDocument>();
        Dictionary<string, int> listt = new Dictionary<string, int>();

        List<double> numberOfWordList = new List<double>();
        List<double> numberOfAllWordsList = new List<double>();
        List<string> DocumentKey = new List<string>();

        List<string> notWordsList = new List<string>();
        double numberOfDocuments;
        double numberOfDocWithWord;

        string[] stopwords;
        public FullTextIndex(string[] stopWords, Dictionary<char,char> normTable)
        {
            stopwords = stopWords;
            normalizationTable = normTable;
            
        }
        public FullTextIndex() { }
        public void AddOrUpdate(string documentId, string text)
        {
           
            if (documentDictionary.ContainsKey(documentId))
            {
                documentDictionary.Remove(documentId);
                documentDictionary.Add(documentId, text);
            }
            else
            documentDictionary.Add(documentId, text);
        }

        public void Remove(string documentId)
        {
            documentDictionary.Remove(documentId);
        }

        public MatchedDocument[] Search(string query)
        {
            numberOfDocuments = documentDictionary.Count;
            query = NormalizeQuery(query);
            NormalizeText();

            string [] keywords = DeleteStopWords(query);

            keywords = NotHandler(keywords);

            SearchWordsInDoc(keywords);

            numberOfDocWithWord = DocumentKey.Count;

            MatchedDocument[] matchedDocuments = CreateMatchedDocuments();

            ClearTempListsAfterReq();
            return matchedDocuments;   
        }

        private MatchedDocument[] CreateMatchedDocuments()
        {
            MatchedDocument[] matchdoc = new MatchedDocument[numberOfAllWordsList.Count];
            double idf = GetIDF();
            double[] tf_idfArray = new double[numberOfAllWordsList.Count];
            for (int i = 0; i < numberOfAllWordsList.Count; i++)
            {
                tf_idfArray[i] = GetTF_IDF(idf, i);
                matchdoc[i] = new MatchedDocument(DocumentKey[i], tf_idfArray[i]);
            }
            return matchdoc;
        }

        private void ClearTempListsAfterReq()
        {
            notWordsList.Clear();
            numberOfAllWordsList.Clear();
            DocumentKey.Clear();
            numberOfWordList.Clear();
        }

        private void SearchWordsInDoc(string [] keywords)
        {
            foreach (var item in documentDictionary)
            {
                string[] docStrings = item.Value.Split(' ', ',', '-');
                int numberofallwords = docStrings.Length;
                int[] numberOfEntry = new int[keywords.Length];
                for (int i = 0; i < keywords.Length; i++)
                {
                    if (keywords[i] == "")
                    {
                        numberOfEntry[i] = 0;
                    }
                    else
                    {
                        string checkedstr = item.Value;
                        if (!CheckNotWordsInText(checkedstr))
                        {
                            string pattern = @"\b" + keywords[i] + @"\b";
                            numberOfEntry[i] = Regex.Matches(item.Value, pattern, RegexOptions.IgnoreCase).Count;
                        }
                        else
                            numberOfEntry[i] = 0;
                    }

                }
                if (Array.TrueForAll<int>(numberOfEntry, y => y > 0))
                {
                    int numberOfWord = numberOfEntry.Sum();
                    if (numberOfWord > 0)
                    {

                        numberOfAllWordsList.Add(numberofallwords);
                        DocumentKey.Add(item.Key);
                        numberOfWordList.Add(numberOfWord);
                    }
                }

            }
        }

        private bool CheckNotWordsInText(string item)
        {
            for (int i = 0; i < notWordsList.Count; i++)
            {
                if (item.Contains(notWordsList[i]))
                {
                    return true;
                }
            }
            return false;
        }

        private string[] NotHandler(string [] query)
        {
            
            for (int i = 0; i < query.Length; i++)
            {

                if (query[i] == "not" && i == query.Length - 1)
                {
                    query = string.Join(" ", query.Where(word => !"not".Contains(word))).Split(',', '-', ' ');
                }
                else if (query[i] == "not" && query[i + 1] != "not")
                {
                    notWordsList.Add(query[i + 1]);
                    Array.Clear(query, i, 2);
                    query = string.Join(" ", query.Where(word => word != null)).Split(',', '-', ' ');
                    
                }
                else if (query[i] == "not" && query[i + 1] == "not")
                {
                    Array.Clear(query, i, 1);
                    query = string.Join(" ", query.Where(word => word != null)).Split(',', '-', ' ');
                    i--;
                }
            }
            return query;
        }

        private double GetTF_IDF(double idf, int i)
        {
            double tf = (((double)numberOfWordList[i] / (double)numberOfAllWordsList[i]));
            double tf_idf = tf * idf;
            return tf_idf;
        }
        private double GetIDF()
        {
            double idf = Math.Log(Math.Abs((double)numberOfDocuments) / Math.Abs((double)numberOfDocWithWord));
            return idf;
        }

        private string[] DeleteStopWords(string query)
        {
            query = query.ToLower();
            string[] stringsWithoutSep = query.Split(',', '-', ' ');
            string[] keywords = string.Join(" ", stringsWithoutSep.Where(word => !stopwords.Contains(word))).Split(',', '-', ' ');
            return keywords;
        }

        private string NormalizeQuery(string query)
        {
            //string str = "" ;
            foreach (var ch in normalizationTable)
            {
                
                if (query.Contains(ch.Key))
                {
                    query= query.Replace(ch.Key, ch.Value);
                }
            }
            return query;
        }

        private void NormalizeText()
        {
            Dictionary<string, string> tempDictionary = new Dictionary<string, string>();
            foreach (var i in documentDictionary.Keys)
            {
                string dicValue = documentDictionary[i];
                foreach (var ch in normalizationTable)
                {
                    if (dicValue.Contains(ch.Key))
                    {
                        dicValue =dicValue.Replace(ch.Key, ch.Value);
                        
                    }
                }
                tempDictionary.Add(i, dicValue);
            }
            UpdateDocumentDictionaryToTempDict(tempDictionary);
        }

        private void UpdateDocumentDictionaryToTempDict(Dictionary<string,string> dictionary)
        {
            foreach(var item in dictionary)
            {
                AddOrUpdate(item.Key, item.Value);
            } 
        }
    }
    
    public class FullTextIndexFactory : IFullTextIndexFactory
    {
        public IFullTextIndex Create(string[] stopWords, Dictionary<char, char> normalizationTable)
        {
            return new FullTextIndex(stopWords, normalizationTable);
        }
    }
}