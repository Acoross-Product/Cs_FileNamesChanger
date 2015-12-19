using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;

/* 숫자를 *로 해서 검색하는 것은 성공. 
 * TODO: 길이가 정해지지 않은 임의의 문자열 지정 기능 추가.
 */
namespace FileNamesChanger
{
    public partial class Form1 : Form
    {
        string      path;
        string frontPattern, backPattern;                

        public Form1()
        {
            InitializeComponent();
            path = "";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if(openFileDialog1.ShowDialog() != DialogResult.OK)
            {
                return;
            }

            if (openFileDialog1.FileNames.Length  > 0)
            {
                path = openFileDialog1.FileName.TrimEnd(openFileDialog1.SafeFileName.ToCharArray());
                textBox1.Text = path;

                textBox2.Text = openFileDialog1.SafeFileName;

                System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(path);
                System.IO.FileInfo[] fis = di.GetFiles();

                string[] srcFilenames = new string[fis.Length];
                int i = 0;
                foreach (System.IO.FileInfo fi in fis)
                {
                    srcFilenames[i++] = fi.Name;
                }
                listBox1.DataSource = srcFilenames;
            }            
        }

        private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox1.SelectedIndex < 0 || listBox1.SelectedIndex > listBox1.Items.Count)
                return;

            textBox2.Text = (string)listBox1.Items[listBox1.SelectedIndex];
        }

        private string PatternStringToREX(string srcPattern)
        {
            char[] escapeChars = { '[', ']', '+', '.', '(', ')'};

            foreach (char ch in escapeChars)
            {
                srcPattern = InsertEscapeToString(srcPattern, ch);
            }

            return srcPattern;
        }

        private string InsertEscapeToString(string src, char ch)
        {
            int idx1 = 0;
            int idx = src.IndexOf(ch, idx1);
            while (idx != -1)
            {
                src = src.Insert(idx, @"\");
                idx1 = idx + 2;
                idx = src.IndexOf(ch, idx1);
            }

            return src;
        }

        /* 방법: 숫자위치를 *로  표시.  검색-> 파일목록 가져오고,  각각을 번호와함께 저장.
         * 변환문자열 *과 함께 기록. (*은 숫자 위치)
         * '변환' 버튼을 누르면 변환됨. */
        private void btSearch_Click(object sender, EventArgs e)        
        {
            SearchAndBind();    
        }

        void SearchAndBind()
        {
            string pattern = StringToREX2(textBox2.Text);

            if (pattern.TrimEnd("\0".ToArray<char>()) == "")
            {
                return;
            }

            if (path == "")
            {
                return;
            }

            SearchResult sr = FindMatchingList(pattern, path);

            if (sr != null)
                listBox1.DataSource = sr.filenames;    
        }

        SearchResult FindMatchingList(string aPattern, string aPath)
        {
            System.IO.DirectoryInfo di = new System.IO.DirectoryInfo(aPath);
            System.IO.FileInfo[] fis = di.GetFiles();

            string[] filenames = new string[fis.Length];            
            string[] numbers = new string[fis.Length];

            int i = 0;
            try
            {
                foreach (System.IO.FileInfo fi in fis)
                {
                    System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(aPattern);
                    Match match = regex.Match(fi.Name);

                    if (match.Success)
                    {                        
                        filenames[i] = match.Captures[0].Value;
                        numbers[i] = match.Groups[1].Value;
                        ++i;
                    }
                }

                SearchResult sr = new SearchResult(filenames, numbers, null, i);

                return sr;
            }
            catch
            {
                Console.WriteLine("Error!!!");

                return null;
            }
        }
        
        private bool PatternStringDiscriminate(string src)
        {
            int idx = src.IndexOf('*');

            if (src.IndexOf('*', idx + 1) > -1)
            {
                return false;
            }

            if (idx != -1)
            {
                frontPattern = src.Substring(0, idx);
                backPattern = src.Substring(idx + 1);

                return true;
            }
            else
            {
                return false;
            }
        }
                
        private string StringToREX2(string src)
        {
            src =  PatternStringToREX(src);

            if (src.Contains(@"***"))
            {
                src = src.Replace(@"***", @"([0-9]{3})");
            }
            else if (src.Contains(@"**"))
            {
                src = src.Replace(@"**", @"([0-9]{2})");
            }
            else if (src.Contains(@"*"))
            {
                src = src.Replace(@"*", @"([0-9])");
            }
            else
            {
                return src;
            }

            while (src.Contains(@"$"))
            {
                src = src.Replace(@"$", @"(.+)");
            }

            return src;
        }

        private void btStringTrans_Click(object sender, EventArgs e)
        {
            textBox2.Text = StringToREX2(textBox2.Text);
        }

        private void btRun_Click(object sender, EventArgs e)
        {
            if (tbNewName.Text.Trim() == "")
            {
                return;
            }

            string pattern = StringToREX2(textBox2.Text);

            if (pattern.TrimEnd("\0".ToArray<char>()) == "")
            {
                return;
            }

            if (path == "")
            {
                return;
            }

            SearchResult sr =  FindMatchingList(pattern, path);

            string[] newnames = new string[sr.found];
                       
            for (int i = 0; i < sr.found; ++i)
            {
                newnames[i] = GetNewName(sr.nums[i]);
                if(newnames[i] == "null")
                {
                    MessageBox.Show("Dest file name error.");

                    return;
                }                
            }

            for (int i = 0; i < sr.found; ++i)
            {
                System.IO.File.Move(path + sr.filenames[i], path + newnames[i]);
            }

            SearchAndBind();
        }

        private string GetNewName(string num)
        {
            if (tbNewName.Text.Trim() == "")
            {
                return "null";
            }

            string tplt = tbNewName.Text;

            if (tplt.Contains(@"***"))
            {
                tplt = tplt.Replace(@"***", num);
            }
            else if (tplt.Contains(@"**"))
            {
                tplt = tplt.Replace(@"**", num);
            }
            else if (tplt.Contains(@"*"))
            {
                tplt = tplt.Replace(@"*", num);
            }
            else
            {
                return "null";
            }

            return tplt;
        }
    }

    public class SearchResult
    {
        public string[] filenames;
        public string[] nums;
        public string[] var;
        public int found;

        private SearchResult()
        {
        }

        public SearchResult(string[] fn, string[] n, string[] v, int f)
        {
            if (fn != null)
                filenames = (string[])fn.Clone();

            if (n != null)
                nums = (string[])n.Clone();

            if (v != null)
                var = (string[])v.Clone();

            found = f;
        }
    }

    
}
