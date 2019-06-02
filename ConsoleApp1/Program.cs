using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ConsoleApp1
{
    class Program
    {
        static async void SortVoc()
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string sortDir = Path.Combine(dir, "sort\\");
            Directory.CreateDirectory(sortDir);
            while (true)
            {
                StreamReader fin = File.OpenText(sortDir + "in.txt");
                var eng_fout = new StreamWriter(sortDir + "eng_out.txt");
                var rus_fout = new StreamWriter(sortDir + "rus_out.txt");
                List<string> end_voc = new List<string>();
                List<string> rus_voc = new List<string>();
                int maxLength = 0;
                while (!fin.EndOfStream)
                {
                    var str = fin.ReadLine();
                    maxLength = (maxLength > str.Length) ? maxLength : str.Length;
                    end_voc.Add(str);
                }
                end_voc.Sort();

                char last_symbol = '\0';

                foreach (var line in end_voc)
                {
                    StringBuilder tmpLine = new StringBuilder(line);
                    tmpLine.Append(' ', maxLength - tmpLine.Length);
                    var tmpVoc = tmpLine.ToString().Split(new string[] { " - " }, StringSplitOptions.None);
                    if (last_symbol < line[0].ToString().ToLower()[0])
                    {
                        last_symbol = line[0].ToString().ToLower()[0];
                        eng_fout.WriteLine("");
                        eng_fout.WriteLine(last_symbol.ToString().ToUpper());
                        eng_fout.WriteLine("");
                    }
                    eng_fout.WriteLine(tmpLine);
                    rus_voc.Add(tmpVoc[1] + " - " + tmpVoc[0]);
                    Console.WriteLine(tmpLine);
                }
                rus_voc.Sort();
                last_symbol = '\0';
                foreach (var line in rus_voc)
                {
                    if (last_symbol < line[0].ToString().ToLower()[0])
                    {
                        last_symbol = line[0].ToString().ToLower()[0];
                        rus_fout.WriteLine("");
                        rus_fout.WriteLine(last_symbol.ToString().ToUpper());
                        rus_fout.WriteLine("");
                    }
                    rus_fout.WriteLine(line);
                }
                fin.Close();
                eng_fout.Close();
                rus_fout.Close();
                Console.WriteLine("\nSorted\n===============\n");
                await Task.Delay(10000);
            }
        }

        public static async Task<string> TranslateTextWithModel(string text)
        {
            string yandexAPIkey = "";
            string url = "https://translate.yandex.net/api/v1.5/tr.json/translate?key=" + yandexAPIkey + "&text=" + text + "&lang=en-ru&format=plain";
            HttpClient client = new HttpClient();
            string result = "";
            try
            {
                while (true)
                {
                    var response = await client.GetAsync(url);
                    var json = await response.Content.ReadAsStringAsync();
                    dynamic jsonObj = JsonConvert.DeserializeObject(json);
                    Console.WriteLine(text + "\t- OK");
                    result = jsonObj.text[0];
                    break;
                }
            }
            catch(Exception)
            {
                Console.WriteLine(text + "\t- FAIL. Try again after 5 seconds ...");
                await Task.Delay(5000);
            }
            return result;
        }

        static async void SplitText()
        {
            string dir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string sortDir = Path.Combine(dir, "sort\\");
            Directory.CreateDirectory(sortDir);
            StreamReader fin = File.OpenText(sortDir + "text.txt");
            SortedSet<string> voc = new SortedSet<string>();
            int maxLength = 0;

            Regex rgx = new Regex("[^a-zA-Z ]");

            var splitVoc = new char[] { ' ' };

            Console.WriteLine("\nRead and split...\n");

            while (!fin.EndOfStream)
            {
                var line = fin.ReadLine();
                line = rgx.Replace(line, "");
                foreach (var str in line.Split(splitVoc))
                {
                    if (str.Length > 1)
                        voc.Add(str.ToLower());
                    maxLength = (maxLength > str.Length) ? maxLength : str.Length;
                }
            }
            var fout = new StreamWriter(sortDir + "text_out.txt");

            List<KeyValuePair<string, Task<string>>> translatedStringTasks = new List<KeyValuePair<string, Task<string>>>();

            Console.WriteLine("\nMake tasks...\n");

            foreach (var str in voc)
            {
                var result = TranslateTextWithModel(str);
                translatedStringTasks.Add(new KeyValuePair<string, Task<string>> (str, result));

                await Task.Delay(5);
            }

            Console.WriteLine("\nTranslate...\n");

            foreach (var task in translatedStringTasks)
            {
                await task.Value;
            }

            Console.WriteLine("\nMake voc...\n");

            char last_symbol = '\0';

            foreach (var str in translatedStringTasks)
            {
                StringBuilder tmpLine = new StringBuilder(str.Key);
                tmpLine.Append(' ', maxLength - tmpLine.Length);
                tmpLine.Append(" - ");
                tmpLine.Append(str.Value.Result);
                if (last_symbol < tmpLine[0].ToString().ToLower()[0])
                {
                    last_symbol = tmpLine[0].ToString().ToLower()[0];
                    fout.WriteLine("");
                    fout.WriteLine(last_symbol.ToString().ToUpper());
                    fout.WriteLine("");
                    Console.WriteLine("");
                    Console.WriteLine(last_symbol.ToString().ToUpper());
                    Console.WriteLine("");
                }
                fout.WriteLine(tmpLine);
                Console.WriteLine(tmpLine);
            }
            fout.Close();
            Console.WriteLine("\nText end\n===============\n");
        }

        static void Main(string[] args)
        {
            SplitText();
            Console.ReadKey();
        }
    }
}
