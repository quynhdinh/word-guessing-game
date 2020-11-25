using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Linq;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Policy;
using GameServer.DTO;

namespace GameServer
{
    static class Program
    {

        static List<Question> listQuestions = new List<Question>();
        static int indexQuestion = -1; // the index of number that we are currently in
        static Socket serverSocket = null;
        static IPAddress ip = null;
        static IPEndPoint point = null;
        static int indexPlayer = -1; // the index of the current player is in turn
        static List<Player> listPlayer;
        static MainForm form = null;
        static int turn = 0; // keep track of what turn we are in
        static bool running = false;

        //static Dictionary<string, int> scoreboard = null;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            //scoreboard = new Dictionary<string, int>();
            listPlayer = new List<Player>();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new MainForm(bListenClick, bSendClick, bLoadQuestionClick, EndGame);
            loadData();
            Application.Run(form);
        }

        static EventHandler bListenClick = SetListen;
        static EventHandler bSendClick = SendMsg;
        static EventHandler bLoadQuestionClick = LoadQuestions;
        static EventHandler bEndGameClick = EndGame;
        static void SetListen(object sender, EventArgs e)
        {
            ip = IPAddress.Parse(form.GetIPText());
            point = new IPEndPoint(ip, form.GetPort());

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                serverSocket.Bind(point);
                serverSocket.Listen(20);
                form.Println($"Server starts at {point}.");

                Thread thread = new Thread(Listen);
                thread.IsBackground = true;
                thread.Start(serverSocket);
            }
            catch (Exception ex)
            {
                form.Println($"Error： {ex.Message}");
            }
        }

        static void Listen(object so)
        {
            Socket serverSocket = so as Socket;
            while (true)
            {
                try
                {
                    //Wait for connection and create a communication socket
                    Socket clientSocket = serverSocket.Accept();
                    //Get the IP address of the link
                    string clientPoint = clientSocket.RemoteEndPoint.ToString();
                    form.Println($"Client {clientPoint}'s request connection accepted.");


                    form.ComboBoxAddItem(clientPoint);

                    //Start a new thread to keep receiving messages
                    if (running)// if the game is running do not accept new client
                    {
                        byte[] msg = Encoding.UTF8.GetBytes("Game is running please wait for next game./");
                        clientSocket.Send(msg);
                        clientSocket.Close();
                        return;

                    }

                    Thread thread = new Thread(Receive);
                    thread.IsBackground = true;
                    thread.Start(clientSocket);
                }
                catch (Exception e)
                {
                    form.Println($"Error： {e.Message}");
                    break;
                }
            }
        }

        /// <summary>
        /// check whether the listPlayer already has the name s
        /// </summary>
        /// <param name="s"></param>
        /// <returns></returns>
        static bool containThisName(string s)
        {
            foreach (var item in listPlayer)
                if (item.Nickname == s)
                    return true;
            return false;
        }

        /// <summary>
        /// update for player that at Socket 'socket' with 'point' points
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="point"></param>
        static void updateScore(Socket socket, int point)
        {
            foreach (var item in listPlayer)
                if (item.Socket == socket)
                {
                    item.Point += point;
                    break;
                }
        }


        /// <summary>
        /// set disqualify status for the player that is at Socket 'socket' as 'kick'
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="kick"></param>
        static void setDisqualifyPlayer(Socket socket, bool kick)
        {
            foreach (var item in listPlayer)
                if (item.Socket == socket)
                {
                    item.Disqualified = kick;
                    break;
                }
        }

        /// <summary>
        /// if the player at index 'now' is the only one that has not been disqualified
        /// </summary>
        /// <param name="now"></param>
        /// <returns></returns>
        static bool allDisqualiedExceptMe(int now)
        {
            for (int i = 0; i < (int)listPlayer.Count(); i++)
                if (i != now && !listPlayer[i].Disqualified) return false;
            return true;
        }
        static void sendScoreboard()
        {
            string s = "ScoreBoard: /";
            List<Player> sorted = listPlayer.OrderByDescending(x => x.Point).ToList();

            foreach (Player player in sorted)
            {
                s += player.Nickname + " has " + player.Point + " points./";
            }
            byte[] msg = Encoding.UTF8.GetBytes(s);
            foreach (Player player in listPlayer)
            {
                player.Socket.Send(msg);
            }
        }
        /// <summary>
        /// update the index to the next player that has not been disqualified
        /// </summary>
        /// <param name="now"></param>
        static void nextPlayer(ref int _now)
        {
            if (!running) return;
            int now = _now;
            while (true)
            {
                now++;
                if (now >= listPlayer.Count()) now = 0;
                if (!listPlayer[now].Disqualified) break;
            }
            _now = now;
            turn++;

            if (turn >= 5)
            {
                running = false;
                byte[] msg = Encoding.UTF8.GetBytes("The fifth turn has ended! Game over!/");
                foreach (Player player in listPlayer)
                {
                    player.Socket.Send(msg);
                }
                sendScoreboard();
            }
            else
            {
                byte[] msg = Encoding.UTF8.GetBytes("This is turn number: " + (turn + 1) + "/");
                foreach (Player player in listPlayer)
                {
                    player.Socket.Send(msg);
                }
            }

        }
        /// <summary>
        /// send signal that the player has index as who is in turn all others are not in turn(enable button Guess)
        /// </summary>
        /// <param name="who"></param>
        static void activatePlayer(int who)
        {
            if (!running) return;
            for (int index = 0; index < listPlayer.Count(); index++)
            {
                byte[] sendeee = null;
                string str = "InTurn?" + (index == who ? '1' : '0').ToString() + "/";
                Debug.WriteLine(str);
                sendeee = Encoding.UTF8.GetBytes(str);
                listPlayer[index].Socket.Send(sendeee);
            }
        }
        static void Receive(object so)
        {
            Socket clientSocket = so as Socket;
            string clientPoint = clientSocket.RemoteEndPoint.ToString();
            bool setname = false;
            byte[] send1 = Encoding.UTF8.GetBytes("Write in the chat what your username is:");
            clientSocket.Send(send1);
            while (!setname)
            {
                byte[] bufname = new byte[1024];
                int len1 = clientSocket.Receive(bufname);
                if (len1 != 0)
                {


                    string name = Encoding.UTF8.GetString(bufname, 0, len1);
                    name = name.Substring(4);
                    if (!name.All(x => char.IsLetterOrDigit(x) || x == '_'))
                    {
                        byte[] send = Encoding.UTF8.GetBytes("ERR: name not in right format(must be alphanumeric or underscore please choose another/");
                        clientSocket.Send(send);
                        continue;
                    }
                    if (containThisName(name))
                    {
                        byte[] send = Encoding.UTF8.GetBytes("ERR: name already exist please choose anotherf/");
                        clientSocket.Send(send);
                        continue;
                    }
                    clientPoint = name;
                    listPlayer.Add(new Player(name, 0, 0, false, clientSocket));
                    setname = true;
                    form.Println("The player " + name + " has joined the game.");


                }
            }

            while (true)
            {
                try
                {
                    //Get the sent message container
                    byte[] buf = new byte[1024 * 1024 * 2];
                    int len = clientSocket.Receive(buf);
                    //If valid byte is 0, skip
                    if (len == 0) continue;

                    string s = Encoding.UTF8.GetString(buf, 0, len);
                    string flag = s.Substring(0, 4); // capture the flag
                    s = s.Substring(4); // delete the flag 
                    if (flag == "ONE:" || flag == "ALL:") // one character sent
                    {

                        if (clientSocket != listPlayer[indexPlayer].Socket)
                        {
                            byte[] send = Encoding.UTF8.GetBytes("It is not your turn!/");
                            clientSocket.Send(send);
                            continue;
                        }
                        if (flag == "ONE:") guessOneCharacter(clientSocket, s[0]);
                        else if (turn > 1) guessTheKeyword(clientSocket, s);
                        else
                        {
                            byte[] send = Encoding.UTF8.GetBytes("You can only guess the key word after 2 turns./");
                            clientSocket.Send(send);
                            continue;
                        }
                        Debug.WriteLine("On player: " + indexPlayer.ToString());
                        nextPlayer(ref indexPlayer);

                        activatePlayer(indexPlayer);
                    }
                    else if (flag == "MSG:") // just a chit-chat message, normal print out on form
                    {
                        form.Println($"{clientPoint}: {s}");

                        //send the chat message to other players(or to server only ???)
                        foreach (var item in listPlayer)
                        {
                            byte[] sendee = Encoding.UTF8.GetBytes($"{clientPoint}: {s}");
                            item.Socket.Send(sendee);
                        }
                    }
                    else return;
                }
                catch (SocketException e)
                {
                    form.ComboBoxRemoveItem(clientPoint);

                    form.Println($"Client {clientSocket.RemoteEndPoint} Disconnect： {e.Message}");

                    clientSocket.Close();
                    break;
                }
                catch (Exception e)
                {
                    form.Println($"Error： {e.Message}");
                }
            }
        }

        /// <summary>
        /// process when received the string 's' and the player at 'clientSocket' choose to guess they whole keyword
        /// </summary>
        /// <param name="clientSocket"></param>
        /// <param name="s"></param>
        static void guessTheKeyword(Socket clientSocket, string s)
        {
            if (s == listQuestions[indexQuestion].Keyword) // guess the whole keyword right
            {
                running = false;
                foreach (var item in listPlayer)
                {
                    if (item.Socket == clientSocket)
                    {
                        byte[] sendeee = Encoding.UTF8.GetBytes("GESSYou guessed it right. You recieved 5 point/");
                        updateScore(clientSocket, 5);
                        item.Socket.Send(sendeee);
                    }
                    else
                    {
                        byte[] ss = Encoding.UTF8.GetBytes("ANNOPlayer " + item.Nickname + " guessed the word right. Now moving to the next question/");
                        item.Socket.Send(ss);
                    }
                }
                sendScoreboard();

                for (int index = 0; index < listPlayer.Count(); index++)
                {
                    byte[] sendeee = null;
                    string str = "InTurn?" + '0' + "/";

                    sendeee = Encoding.UTF8.GetBytes(str);
                    listPlayer[index].Socket.Send(sendeee);

                }
                indexPlayer = -1;

            }
            else // guess it wrong => disqualify this player
            {
                byte[] sendeee = Encoding.UTF8.GetBytes("ANNOYou guessed it wrong! You have been disqualified!/");
                clientSocket.Send(sendeee);
                setDisqualifyPlayer(clientSocket, true); // set status of be disqualified as true
                form.Println("The player " + ip.ToString() + " has been disqualified/");
            }
        }

        /// <summary>
        /// process when received the string 's' and the player at 'clientSocket' choose to guess only one character
        /// </summary>
        /// <param name="client"></param>
        /// <param name=""></param>
        /// <param name="s"></param>
        static void guessOneCharacter(Socket clientSocket, char ch)
        {
            bool bingo = false; // match any character?
            for (int i = 0; i < listQuestions[indexQuestion].Keyword.Length; i++)
                if (listQuestions[indexQuestion].Keyword[i] == ch)
                {
                    bingo = true;
                    break;
                }
            if (bingo == false) // DOESN'T match annouce and move on
            {
                byte[] ss = Encoding.UTF8.GetBytes("FLS:/");
                clientSocket.Send(ss);
            }
            else // DOES match, then update everything and send vietn*m back
            {
                listQuestions[indexQuestion].updateGuesses(ch);
                string sendee = listQuestions[indexQuestion].updateShowed();
                byte[] ss = Encoding.UTF8.GetBytes("COR:" + sendee + "/"); // send to the one who guessed it right
                clientSocket.Send(ss);
                ss = Encoding.UTF8.GetBytes("UDT:" + sendee + "/"); // send to the one who isn't in turn
                foreach (var item in listPlayer)
                {
                    if (item.Socket == clientSocket)
                    {
                        updateScore(clientSocket, 1);
                    }
                    item.Socket.Send(ss);
                }
            }
        }
        static void SendMsg(object sender, EventArgs e)
        {
            string msg = form.GetMsgText();
            if (msg == "") return;
            byte[] sendee = Encoding.UTF8.GetBytes($"Server：{msg}" + "/");
            foreach (var item in listPlayer)
            {
                item.Socket.Send(sendee);
            }
            form.Println(msg);
            form.ClearMsgText();
        }

        /// <summary>
        /// Load data from file
        /// </summary>
        static void loadData()
        {
            string[] lines = System.IO.File.ReadAllLines(@"../../data.txt");
            int n = int.Parse(lines[0]);
            lines = lines.Skip(1).ToArray();
            for (int i = 0; i < n * 2; i += 2)
            {
                listQuestions.Add(new Question(lines[i], lines[i + 1], lines[i].Length, new List<int>(), new List<char>()));
                Debug.WriteLine(lines[i] + '-' + lines[i + 1] + " Size = " + lines[i].Length);
            }
        }
        static void LoadQuestions(object sender, EventArgs e)
        {
            turn = 0;
            if (listPlayer.Count < 1)
            {
                form.Println("Can't start because there aren't enough player!");
                return;
            }
            running = true;
            if (indexPlayer < 0)
            {
                indexPlayer = 0;
                activatePlayer(indexPlayer);

            }
            indexQuestion++;
            if (listQuestions.Count == indexQuestion) // if we have been go through all questions
            {
                form.disableLoadQuestionButton();
                MessageBox.Show("End game! Check out the scoreboard");
                form.updateScoreboard(listPlayer);
                return;
            }
            if (listPlayer.Count() != 1 && allDisqualiedExceptMe(indexPlayer))
            {

            }
            form.loadQuestion(listQuestions[indexQuestion].Keyword.ToString(), listQuestions[indexQuestion].Hint.ToString());
            string msgQuestion = "QQQ" + listQuestions[indexQuestion].updateShowed() + ' ' + listQuestions[indexQuestion].Hint.ToString();
            byte[] sendee = Encoding.UTF8.GetBytes(msgQuestion);

            // if that player hasn't been disqualified, send question to him
            foreach (var item in listPlayer)
            {
                if (!item.Disqualified) item.Socket.Send(sendee);
            }
            form.Println("Question has been loaded and sent to the clients!");

            foreach (var item in listPlayer)
            {
                Debug.WriteLine("player: " + item.Nickname);
            }
        }

        static void EndGame(object sender, EventArgs e)
        {
            form.updateScoreboard(listPlayer);
        }
    }
}