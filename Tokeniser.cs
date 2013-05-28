using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PanGu;
using Lucene;
using PanGu.HighLight;
using Lucene.Net.Analysis.PanGu;
using Lucene.Net.Store;
using Lucene.Net.Index;
using Lucene.Net.Documents;
using Lucene.Net.Search;

namespace Keywords
{
    /// <summary>
    /// Summary description for Tokeniser.
    /// Partition string into SUBwords
    /// </summary>
    internal class Tokeniser : ITokeniser
    {
        ICollection<WordInfo> _words;
        List<string> _filteredwords;
        /// <summary>
        /// 以空白字符进行简单分词，并忽略大小写，
        /// 实际情况中可以用其它中文分词算法
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public IList<string> Partition(string input)
        {
            PanGu.Segment.Init();
            Segment segment = new Segment();
            ICollection<WordInfo> words1 = segment.DoSegment(input.ToLower());

            _words = PatternMatch(words1);
            
            foreach (WordInfo word in _words)
            {

                //Int32 x = (Int32)word.Pos | 0x419090f8;
                //if (x == 0x419090f8)
                int i = word.Word.Length;
                if(i>=2)
                    _filteredwords.Add(word.Word);
            }

            return _filteredwords.ToArray();


            //Regex r = new Regex("([ \\t{}():;. \n])");
            //input = input.ToLower();

            //String[] tokens = r.Split(input);

            //List<string> filter = new List<string>();

            //for (int i = 0; i < tokens.Length; i++)
            //{
            //    MatchCollection mc = r.Matches(tokens[i]);
            //    if (mc.Count <= 0 && tokens[i].Trim().Length > 0
            //        && !StopWordsHandler.IsStopword(tokens[i]))
            //        filter.Add(tokens[i]);
            //}

            //return filter.ToArray();
        }



        /// <summary>
        /// 匹配名词短语的模式，考虑组成名词短语的词性组合，
        /// 只考虑连续两个词的组合,返回WordInfo类型的短语集合。
        /// </summary>
        /// <param name="words"></param>
        /// <returns></returns>
        public ICollection<WordInfo> PatternMatch(ICollection<WordInfo> words)
        {
            List<WordInfo> phrase = new List<WordInfo>();
            IEnumerator<WordInfo> enums = words.GetEnumerator();
            Stack<WordInfo> s = new Stack<WordInfo>();
            s.Clear();
            while (enums.MoveNext())
            {
                WordInfo temp = enums.Current;
                Int32 x = (Int32)temp.Pos;
                switch (x)
                {
                    case 0:
                    case 8:
                    case 16:
                    case 32:
                    case 64:
                    case 128:
                    case 8388608:
                    case 16777216:  //专有名词，外文词，未知词性直接加入
                        if (s.Count == 1)
                        {
                            phrase.Add(s.Pop());
                        }
                        phrase.Add(temp);
                        s.Clear();
                        break;
                    case 4096:
                    case 1048576:
                    case 1073741824://名词、动词、形容词
                        if (s.Count == 1)
                        {
                            WordInfo t = s.Pop();
                            if (t.Pos.Equals(POS.POS_D_A) && temp.Pos.Equals(POS.POS_D_A))//两个都是形容词
                            {
                                s.Clear();
                                s.Push(temp);
                            }
                            else
                            {   //把两个词连接起来
                                //t.Pos = POS.POS_D_N;
                                s.Push(temp);
                                t.Word += temp.Word;
                                phrase.Add(t);
                                //phrase.Add(temp);
                            }
                        }
                        else
                        {
                            s.Push(temp);
                        }
                    break;

                }
            }
            

            return phrase;
             
        }

        //private void GenerateTermFrequency()
        //{
        //    for(int i=0; i < _numDocs  ; i++)
        //    {								
        //        string curDoc=_docs[i];
        //        IDictionary freq=GetWordFrequency(curDoc);
                
        //        IDictionaryEnumerator enums=freq.GetEnumerator() ;
        //        _maxTermFreq[i]=int.MinValue ;
        //        while (enums.MoveNext())
        //        {
        //            string word=(string)enums.Key;
        //            int wordFreq=(int)enums.Value ;
        //            int termIndex=GetTermIndex(word);
        //            if(termIndex == -1)
        //                continue;
        //            _termFreq [termIndex][i]=wordFreq;
        //            _docFreq[termIndex] ++;

        //            if (wordFreq > _maxTermFreq[i]) _maxTermFreq[i]=wordFreq;					
        //        }
        //    }
        //}

        private IDictionary GetWordFrequency()
		{
								
            string[] words= _filteredwords.ToArray();		
	        
			Array.Sort(words);
			
			String[] distinctWords=GetDistinctWords(words);
						
			IDictionary result=new Hashtable();
			for (int i=0; i < distinctWords.Length; i++)
			{
				object tmp;
				tmp=CountWords(distinctWords[i], words);
				result[distinctWords[i]]=tmp;
				
			}
			
			return result;
		}				
				
		private static string[] GetDistinctWords(String[] input)
		{				
			if (input == null)			
				return new string[0];			
			else
			{
                List<string> list = new List<string>();
				
				for (int i=0; i < input.Length; i++)
					if (!list.Contains(input[i])) // N-GRAM SIMILARITY?				
						list.Add(input[i]);
				
				return list.ToArray();
			}
		}
		

		
		private int CountWords(string word, string[] words)
		{
			int itemIdx=Array.BinarySearch(words, word);
			
			if (itemIdx > 0)			
				while (itemIdx > 0 && words[itemIdx].Equals(word))				
					itemIdx--;				
						
			int count=0;
			while (itemIdx < words.Length && itemIdx >= 0)
			{
				if (words[itemIdx].Equals(word)) count++;				
				
				itemIdx++;
				if (itemIdx < words.Length)				
					if (!words[itemIdx].Equals(word)) break;					
				
			}
			
			return count;
		}


         /// <summary>
        /// 把一个集合按重复次数排序
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inputList"></param>
        /// <returns></returns>
        public static Dictionary<T, int> SortByDuplicateCount<T>(IList<T> inputList)
        {
            //用于计算每个元素出现的次数，key是元素，value是出现次数
            Dictionary<T, int> distinctDict = new Dictionary<T, int>();
            for (int i = 0; i < inputList.Count; i++)
            {
                //这里没用trygetvalue，会计算两次hash
                if (distinctDict.ContainsKey(inputList[i]))
                    distinctDict[inputList[i]]++;
                else
                    distinctDict.Add(inputList[i], 1);
            }

            Dictionary<T, int> sortByValueDict = GetSortByValueDict(distinctDict);
            return sortByValueDict;
        }

        public Dictionary<string,float> WeightComputing()
        {
            IDictionary d1 = GetWordFrequency();
            Dictionary<string,float> weight = new Dictionary<string,float>(d1.Count);
            string[] words = new string[d1.Count];
            d1.Keys.CopyTo(words, 0);
            foreach (String word in words)
            {
                float locWeight = locWeightComputing(word);
                float spanWeight = spanWeightComputing(word);
                int freq = (int)d1[word];
                float freqWeight = (float)freq / (1 + freq);
                weight.Add(word,freqWeight *locWeight*spanWeight);

            }

            return weight;
        }

        private float spanWeightComputing(string word)
        {
            WordInfo lastone = _words.Last();
            int sum = lastone.Position + lastone.Word.Length;
            int first = 0;
            int last = 0;

            foreach (WordInfo w in _words)
            {
                if (word.Equals(w.Word))
                {
                    if (first == 0)
                        first = w.Position;
                    else
                        last = w.Position;
                }
            }

            return last==0?(float)1/sum:(float)(last - first + 1) / sum;
        }

        private float locWeightComputing(String word)
        {
            
            WordInfo lastone = _words.Last();
            int sum = lastone.Position + lastone.Word.Length;
            int head = (int)(sum * 0.1);
            int tail = sum - head;
            int loc = 0;
            
            foreach (WordInfo w in _words)
            {
                int temp = 0;
                if (word.Equals(w.Word))
                {
                    if (w.Position < head)
                        temp = 50;
                    else if (w.Position > tail)
                        temp = 10;
                    else
                        temp = 30;
                }
                if (loc < temp)
                    loc = temp;
            }

            return (float)(loc - 1) / (loc + 1);
            
        }

        public void OutputKeywords(int n)
        {
            Dictionary<string, float> all = WeightComputing();
            Dictionary<string, float> dict = GetSortByValueDict(all);
            IDictionaryEnumerator pointer = dict.GetEnumerator();
            for (int i = 0; i < n; i++)
            {
                pointer.MoveNext();
                Console.WriteLine(pointer.Key + ": " + pointer.Value);
            }
        }

        public void OutputTermFreqDirectly(int n)
        {
            IDictionary d1 = GetWordFrequency();
            Dictionary<string,int> all = new Dictionary<string,int>(d1.Count);
            string[] words = new string[d1.Count];
            d1.Keys.CopyTo(words,0);
            foreach (String word in words)
            {
                all.Add(word, (int)d1[word]);
                    
            }
            Dictionary<string, int> dict = GetSortByValueDict(all);
            IDictionaryEnumerator pointer = dict.GetEnumerator();
            for (int i = 0; i < n; i++)
            {
                pointer.MoveNext();
                Console.WriteLine(pointer.Key+": "+pointer.Value);
            }


        }
        /// <summary>
        /// 把一个字典俺value的顺序排序
        /// </summary>
        /// <typeparam name="K"></typeparam>
        /// <typeparam name="V"></typeparam>
        /// <param name="distinctDict"></param>
        /// <returns></returns>
        public static Dictionary<K, V> GetSortByValueDict<K, V>(IDictionary<K, V> distinctDict)
        {
            //用于给tempDict.Values排序的临时数组
            V[] tempSortList = new V[distinctDict.Count];
            distinctDict.Values.CopyTo(tempSortList, 0);
            Array.Sort(tempSortList); //给数据排序
            Array.Reverse(tempSortList);//反转

            //用于保存按value排序的字典
            Dictionary<K, V> sortByValueDict =
                new Dictionary<K, V>(distinctDict.Count);
            for (int i = 0; i < tempSortList.Length; i++)
            {
                foreach (KeyValuePair<K, V> pair in distinctDict)
                {
                    //比较两个泛型是否相当要用Equals，不能用==操作符
                    if (pair.Value.Equals(tempSortList[i]) && !sortByValueDict.ContainsKey(pair.Key))
                        sortByValueDict.Add(pair.Key, pair.Value);
                }
            }
            return sortByValueDict;
        }

        /// <summary>
        /// 对一个数组进行排重
        /// </summary>
        /// <param name="scanKeys"></param>
        /// <returns></returns>
        public static IEnumerable<T> GetDistinctWords<T>(IEnumerable<T> scanKeys)
        {
            T temp = default(T);
            if (scanKeys.Equals(temp))
                return new T[0];
            else
            {
                Dictionary<T, T> fixKeys = new Dictionary<T, T>();
                foreach (T key in scanKeys)
                {
                    fixKeys[key] = key;
                }
                T[] result = new T[fixKeys.Count];
                fixKeys.Values.CopyTo(result, 0);
                return result;
            }
        }

        public Tokeniser()
        {
            _filteredwords = new List<string>();
        }

    }
}
