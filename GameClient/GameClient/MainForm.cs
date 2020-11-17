using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace GameClient
{
    public partial class MainForm : Form
    {
        public MainForm(EventHandler b1Click, EventHandler b2Click, EventHandler b3Click)
        {
            InitializeComponent();
            this.buttonConnect.Click += b1Click;
            this.buttonSend.Click += b2Click;
            this.btnGuess.Click += b3Click;
        }
        
        public string GetIPText()
        {
            return this.textBoxIP.Text;
        }
        
        public int GetPort()
        {
            return (int)this.numericUpDownPort.Value;
        }
        public List<char> getShownCharacters()
        {
            string s = txbKeyword.Text.ToString();
            List<char> res = new List<char>();
            foreach (char item in s)
                if(item != '*')
                    res.Add(item);
            return res;
        }
        public string GetMsgText()
        {
            return this.textBoxSendee.Text.Trim();
        }
        public string GetCharacter()
        {
            return this.txbGuessOne.Text;
        }
        public string GetStringGuess()
        {
            return this.txbGuessAll.Text;
        }
        public void ClearMsgText()
        {
            this.textBoxSendee.Clear();
        }

        delegate void VoidInt(int n);
           
        /// <summary>
        /// Enable button guess
        /// </summary>
        /// <param name="turn"></param>
        public void isItMyTurn(int turn)
        {
            if(this.btnGuess.InvokeRequired)
            {
                VoidInt voidInt = isItMyTurn;
                this.btnGuess.Invoke(voidInt, turn);
            }
            else
            {
                this.btnGuess.Enabled = (turn == 1 ? true : false);
            }
        }
        delegate void VoidString(string s);

        /// <summary>
        /// Write chit chat on textbox
        /// </summary>
        /// <param name="s"></param>
        public void Println(string s)
        {
            if (this.textBoxMsg.InvokeRequired) {
                VoidString println = Println;
                this.textBoxMsg.Invoke(println, s);
            }
            else {
                this.textBoxMsg.AppendText(s + Environment.NewLine);
            }
        }

        /// <summary>
        /// update the keyword (replacing with '*') *******->*****a*
        /// </summary>
        /// <param name="s"></param>
        public void modifyKeyword(string s)
        {
            if (this.txbKeyword.InvokeRequired)
            {
                VoidString println = modifyKeyword;
                this.txbKeyword.Invoke(println, s);
            }
            else
            {
                this.txbKeyword.Text = s;
            }
        }
        delegate void VoidString2(string s1, string s2);
        public void PrintlnQuestion(string keyword, string hint)
        {
            if (this.txbKeyword.InvokeRequired && this.txbHint.InvokeRequired)
            {
                VoidString2 println = PrintlnQuestion;
                this.txbKeyword.Invoke(println, keyword);
                this.txbHint.Invoke(println, hint);
            }
            else
            {
                this.txbKeyword.Text = keyword;
                this.txbHint.Text = hint;
            }
        }
        /// <summary>
        /// update the keyword and hint on textboxes
        /// </summary>
        /// <param name="s"></param>
        public void updateQuestion(string s)
        {
            Debug.WriteLine("question and hint: " + s);
            string keyword="";
            while(s[0] != ' ')
            {
                keyword += '*';
                s = s.Substring(1);
            }
            s = s.Substring(1);
            if(this.txbKeyword.InvokeRequired && this.txbHint.InvokeRequired)
            {
                VoidString2 printlnQuestion = PrintlnQuestion;
                this.txbKeyword.Invoke(printlnQuestion, keyword, s);
                this.txbHint.Invoke(printlnQuestion, keyword, s);
            }
            else
            {
                this.txbKeyword.Text = keyword;
                this.txbHint.Text = s;
            }
        }
        delegate void VoidBoolString(bool b, string s);
        public void SetConnectionStatusLabel(bool isConnect, string point = null)
        {
            if (this.labelStatus.InvokeRequired) {
                VoidBoolString scsl = SetConnectionStatusLabel;
                this.labelStatus.Invoke(scsl, isConnect, point);
            }
            else {
                if (isConnect) {
                    this.labelStatus.ForeColor = Color.Green;
                    this.labelStatus.Text = point;
                }
                else {
                    this.labelStatus.ForeColor = Color.Red;
                    this.labelStatus.Text = "Not connected yet";
                }
            }
        }
        public void loadAQuestion(string keyword, string hint)
        {
            if (this.txbKeyword.InvokeRequired && this.txbHint.InvokeRequired)
            {
                VoidString println = Println;
                this.txbKeyword.Text= keyword;
                this.txbHint.Text = hint;
            }
            else
            {
                this.txbKeyword.Text = keyword;
                this.txbHint.Text = hint;
            }
        }
        delegate void VoidBool(bool b);
        public void SetButtonSendEnabled(bool enabled)
        {
            if (this.buttonSend.InvokeRequired)
            {
                VoidBool sbse = SetButtonSendEnabled;
                this.textBoxMsg.Invoke(sbse, enabled);
            }
            else
            {
                this.buttonSend.Enabled = enabled;
            }
        }

        public void setButtonGuessEnable(bool enabled)
        {
            if (this.btnGuess.InvokeRequired)
            {
                VoidBool voidBool = setButtonGuessEnable;
                this.btnGuess.Invoke(voidBool, enabled);
            }
            else
            {
                this.btnGuess.Enabled = enabled;
            }
        }
        private void btnClearStatus_Click(object sender, EventArgs e)
        {
            this.textBoxMsg.Clear();
        }
    }
}
