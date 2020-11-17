using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GameServer
{
    public class Question
    {
        private string keyword;
        private string hint;
        private int size;
        public List<int> showed; // the indices of characters that have been shown
        private List<char> alreadyGuessed; // save the characters that have been already guessed
        public Question(string keyword, string hint, int size, List<int> showed, List<char> alreadyGuessed)
        {
            this.keyword = keyword;
            this.hint = hint;
            this.size = size;
            this.showed = showed;
            this.alreadyGuessed = alreadyGuessed;
        }

        /// <summary>
        /// update the character list that has been guessed
        /// </summary>
        /// <param name="ch"></param>
        public void updateGuesses(char ch)
        {
            alreadyGuessed.Add(ch);
        }

        /// <summary>
        /// return the new string to send to clients based on the characters guessed
        /// </summary>
        /// <returns></returns>
        public string updateShowed()
        {
            string res = "";
            for(int i = 0; i < keyword.Length; i++)
            {
                bool exist = false;
                for(int j = 0; j < alreadyGuessed.Count; j++)
                {
                    if (keyword[i] == alreadyGuessed[j])
                    {
                        exist = true;
                        break;
                    }
                }
                if (!exist)
                {
                    res += '*';
                }
                else res += keyword[i];
            }
            return res;
        }
        public string Hint { get => hint; set => hint = value; }
        public string Keyword { get => keyword; set => keyword = value; }
        public int Size { get => size; set => size = value; }
        public List<int> Showed { get => showed; set => showed = value; }
        public List<char> AlreadyGuessed { get => alreadyGuessed; set => alreadyGuessed = value; }

    }
}
