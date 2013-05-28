using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Keywords
{
    class Program
    {
        static void Main(string[] args)
        {
            //1、获取文档输入
            string[] docs = getInputDocs("test5.txt");
            if (docs.Length < 1)
            {
                Console.WriteLine("没有文档输入");
                Console.Read();
                return;
            }

            //2、初始化TFIDF测量器，用来生产每个文档的TFIDF权重
            
            string txt = "";
            for (int i = 0; i < docs.Length; i++)
            {
                txt += docs[i];
            }
            Tokeniser tk = new Tokeniser();
            tk.Partition(txt);
            tk.OutputKeywords(10);

            Console.Read();
        }

        /// <summary>
        /// 获取文档输入
        /// </summary>
        /// <returns></returns>
        private static string[] getInputDocs(string file)
        {
            List<string> ret = new List<string>();
            try
            {
                using (StreamReader sr = new StreamReader(file, Encoding.Default))
                {
                    string temp;
                    while ((temp = sr.ReadLine()) != null)
                    {
                        ret.Add(temp);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return ret.ToArray();
        }
    }
}
