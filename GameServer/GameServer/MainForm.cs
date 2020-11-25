using GameServer.DTO;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Resources;
using System.Runtime.ExceptionServices;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace GameServer
{
    public partial class MainForm : Form
    {

        public MainForm(EventHandler bListenClick, EventHandler bSendClick, EventHandler bLoadQuestionClick, EventHandler bEndGameClick, EventHandler timerTick)
        {
            InitializeComponent();
            //loadData();
            this.buttonListen.Click += bListenClick;
            this.buttonSend.Click += bSendClick;
            this.btnLoadQuestion.Click += bLoadQuestionClick;
            this.btnEndGame.Click += bEndGameClick;
            this.timer.Tick += timerTick;
        }
        public string GetIPText()
        {
            return this.textBoxIP.Text;
        }
        
        public int GetPort()
        {
            return (int)this.numericUpDownPort.Value;
        }

        public string GetMsgText()
        {
            return this.textBoxSendee.Text.Trim();
        }
        #region Timer business

        public void startTimer()
        {
            this.timer.Start();
        }
        public void stopTimer()
        {
            this.timer.Stop();
        }
        #endregion
        public void ClearMsgText()
        {
            this.textBoxSendee.Clear();
        }
        public void disableLoadQuestionButton()
        {
            this.btnLoadQuestion.Enabled = false;
        }
        delegate void VoidString(string s);
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

        delegate void VoidDTGV(List<Player> list);
        public void updateScoreboard(List<Player> list)
        {
            if(this.dgvScoreboard.Columns.Count != 0) { return;}
            List<Player> sortedPlayers = list.OrderByDescending(o => o.Point).ToList();
            if (this.dgvScoreboard.InvokeRequired)
            {
                VoidDTGV voidDTGV = updateScoreboard;
                this.dgvScoreboard.Invoke(voidDTGV, sortedPlayers);
            }
            else
            {
                this.dgvScoreboard.Columns.Add("rank", "Rank");
                this.dgvScoreboard.Columns.Add("name", "Player name");
                this.dgvScoreboard.Columns.Add("score", "Score earned");
                for (int i = 0; i < sortedPlayers.Count(); i++)
                {
                    this.dgvScoreboard.Rows.Add((i + 1).ToString(), sortedPlayers[i].Nickname, sortedPlayers[i].Point);
                }
            }
        }
        public void loadQuestion(string keyword, string hint)
        {
            if (this.txbKeyword.InvokeRequired && this.txbHint.InvokeRequired)
            {
                VoidString println = Println;
                this.txbKeyword.Invoke(println, keyword);
                this.txbHint.Invoke(println, keyword);
            }
            else
            {
                this.txbKeyword.Text = keyword;
                this.txbHint.Text = hint;
            }
        }

        public void ComboBoxAddItem(string s)
        {
            if (this.comboBoxAllClients.InvokeRequired) {
                VoidString cbAddItem = ComboBoxAddItem;
                this.textBoxMsg.Invoke(cbAddItem, s);
            }
            else {
                this.comboBoxAllClients.Items.Add(s);
            }
        }
        public void ComboBoxRemoveItem(string s)
        {
            if (this.comboBoxAllClients.InvokeRequired) {
                VoidString cbRmItem = ComboBoxRemoveItem;
                this.textBoxMsg.Invoke(cbRmItem, s);
            }
            else {
                this.comboBoxAllClients.Items.Remove(s);
            }
        }

        private void btnClearStatus_Click(object sender, EventArgs e)
        {
            this.textBoxMsg.Clear();
        }
    }
}
