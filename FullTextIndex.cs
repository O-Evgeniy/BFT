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
            Console.WriteLine("Запрос: " + query);
            numberOfDocuments = documentDictionary.Count;
            query = NormalizeQuery(query);
            string[] textout = new string[2];
            //for (int i = 0; i < 2; i++)
            //{
            //    textout[i] = Regex.Matches(query, "\"(.*?)\"", RegexOptions.IgnoreCase)[i].Value.Trim('"');
            //    Console.WriteLine(textout[i]);
            //}
            //Console.WriteLine(textout);
            NormalizeText();

            string [] keywords = DeleteStopWords(query);

            keywords = NotHandler(keywords);

            SearchWordsInDoc(keywords);

            numberOfDocWithWord = DocumentKey.Count;

            MatchedDocument[] matchedDocuments = CreateMatchedDocuments();

            ClearTempListsAfterReq();
            //Console.WriteLine("ID документа попавшего в поиск: " +matchedDocuments[0].Id);
            return matchedDocuments;   
        }

        //private string[] FindSubquery(string str)
        //{
        //    string[] subguery = Regex.Matches(str, "\"(.*?)\"").Cast<Match>().Select(m=>m.Value.Trim('"')).ToArray();
        //    string[] query = Regex.Replace(str, "\"(.*?)\"", string.Empty).Trim(' ').Split(' ', ',', '-');
        //    if (query[0] != "")
        //    {
        //        query = string.Join(" ", query.Where(word => !"".Contains(word))).Split(',', '-', ' ');
        //        string[] finalquery = subguery.Concat(query).ToArray();
        //        return finalquery;
        //    }
        //    else
        //    {
        //        return subguery;
        //    }
                
            
        //    //return finalquery;
        //}

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
                Console.WriteLine("ТЕКСТ: " + item.Value);
                //string kkk = string.Join(" ", item.Value.Where(word => !stopwords.Contains(word)));
                string[] docStrings = item.Value.Split(' ', ',', '-');
                string[] textWithoutStopWords = string.Join(" ", docStrings.Where(word => !stopwords.Contains(word))).Split(' ', ',', '-');
                string textWithoutStopWordss = string.Join(" ", textWithoutStopWords.Where(word => !"  ".Contains(word)));

                //string[] docStringss = textWithoutStopWords.Split(' ', ',', '-');
                //string textWithoutStopWordss = string.Join(" ", docStringss.Where(word => !stopwords.Contains(word)));
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
                            numberOfEntry[i] = Regex.Matches(textWithoutStopWordss, pattern, RegexOptions.IgnoreCase).Count;
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
            string[] subquery = FindSubquery(query);
            string[] query1 = Regex.Replace(query, "\"(.*?)\"", string.Empty).Trim(' ').ToLower().Split(' ', ',', '-');
            string[] query11 = string.Join(" ", query1.Where(word => !stopwords.Contains(word))).Split(',', '-', ' ','\"');
            if (query1[0] != "")
            {
                query11 = string.Join(" ", query11.Where(word => (!("").Contains(word) && !"\"".Contains(word)))).Split(',', '-', ' ','\"');
                string[] finalquery1 = subquery.Concat(query11).ToArray();
                return finalquery1;
            }
            else
            {
                //subquery = string.Join(" ", subquery.Where(word => !("  ").Contains(word))).Split(',', '-', ' ', '\"');
                return subquery;
            }

            //query = query.ToLower();
            //string[] stringsWithoutSep = query.Split(',', '-', ' ');
            //string[] keywords = string.Join(" ", stringsWithoutSep.Where(word => !stopwords.Contains(word))).Split(',', '-', ' ');
            //string str = string.Join(" ",keywords );
            //string[] finalquery= FindSubquery(str);
            //foreach (var i in finalquery)
            //    Console.WriteLine(i);
            //return finalquery;
        }

        private string[] FindSubquery(string query)
        {
            string[] stringsWithoutSep = query.Split(',', '-', ' ');
            string keywords = string.Join(" ", stringsWithoutSep.Where(word => (!stopwords.Contains(word) && !"    ".Contains(word))));
            string[] subguery = Regex.Matches(keywords, "\"(.*?)\"").Cast<Match>().Select(m => m.Value.Trim('"')).ToArray();
            return subguery;
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