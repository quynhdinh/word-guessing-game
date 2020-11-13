using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameClient.DTO
{
    public class Player
    {
        private string nickname;
        private int point;
        private int no_turn; // the number of time that this user has been guessed
        private bool disqualified; // this user has been disqualified for wrong guessing or not
        public Player(string nickname, int point, int no_turn, bool disqualified = false)
        {
            this.nickname = nickname;
            this.point = point;
            this.no_turn = no_turn;
            this.disqualified = disqualified;
        }

        public string Nickname { get => nickname; set => nickname = value; }
        public int Point { get => point; set => point = value; }
        public int No_turn { get => no_turn; set => no_turn = value; }
        public bool Disqualified { get => disqualified; set => disqualified = value; }
    }
}
