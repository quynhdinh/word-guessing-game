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
        static int indexPlayer = 0; // the index of the current player is in turn
        static List<Player> listPlayer;
//        static Dictionary<string, Socket> allClientSockets = null;
  //      static List<string> clientsQueue;
        static MainForm form = null;

        static Dictionary<string, int> scoreboard = null;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            scoreboard = new Dictionary<string, int>();
            listPlayer = new List<Player>();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new MainForm(bListenClick, bSendClick, bLoadQuestionClick);
            loadData();
            Application.Run(form);
        }

        static EventHandler bListenClick = SetListen;
        static EventHandler bSendClick = SendMsg;
        static EventHandler bLoadQuestionClick = LoadQuestions;
        static void SetListen(object sender, EventArgs e)
        {
            ip = IPAddress.Parse(form.GetIPText());
            point = new IPEndPoint(ip, form.GetPort());

            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try {
                serverSocket.Bind(point);
                serverSocket.Listen(20);
                form.Println($"Server starts at {point}.");

                Thread thread = new Thread(Listen);
                thread.IsBackground = true;
                thread.Start(serverSocket);
            }
            catch (Exception ex) {
                form.Println($"Error： {ex.Message}");
            }
        }

        static void Listen(object so)
        {
            Socket serverSocket = so as Socket;
            while (true)
            {
                try {
                    //Wait for connection and create a communication socket
                    Socket clientSocket = serverSocket.Accept();
                    //Get the IP address of the link
                    string clientPoint = clientSocket.RemoteEndPoint.ToString();
                    form.Println($"Client {clientPoint}'s request connection accepted.");


                    form.ComboBoxAddItem(clientPoint);

                    //Start a new thread to keep receiving messages
                    Thread thread = new Thread(Receive);
                    thread.IsBackground = true;
                    thread.Start(clientSocket);
                }
                catch(Exception e) {
                    form.Println($"Error： {e.Message}");
                    break;
                }
            }
        }
        static bool containThisName(string s)
        {
            foreach (var item in listPlayer)
                if(item.Nickname == s)
                    return true;
            return false;
        }

        static void updateScore(Socket socket, int point)
        {
            foreach (var item in listPlayer)
                if(item.Socket == socket)
                {
                    item.Point += point;
                    break;
                }
        }

        static void disqualifyPlayer(Socket socket, bool kick)
        {
            foreach (var item in listPlayer)
                if(item.Socket == socket)
                {
                    item.Disqualified = kick;
                    break;
                }
        }

        static void nextPlayer(ref int now)
        {
            now++;
            if (now == (int)listPlayer.Count())
                now = 0;
        }
        static void Receive(object so)
        {
            Socket clientSocket = so as Socket;
            string clientPoint = clientSocket.RemoteEndPoint.ToString();
            Debug.WriteLine("Received from: " + clientPoint);
            bool setname = false;
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
                        byte[] send = Encoding.UTF8.GetBytes("ERR: name not in right format(must be alphanumeric or underscore");
                        clientSocket.Send(send);
                        continue;
                    }
                    if (containThisName(name))
                    {
                        byte[] send = Encoding.UTF8.GetBytes("ERR: name already exist");
                        clientSocket.Send(send);
                        continue;
                    }
                    clientPoint = name;
                    //allClientSockets.Add(clientPoint, clientSocket);
                    listPlayer.Add(new Player(name, 0, 0, false, clientSocket));
                    setname = true;
                }
            }

            while (true) {
                try {
                    //Get the sent message container
                    byte[] buf = new byte[1024 * 1024 * 2];
                    int len = clientSocket.Receive(buf);
                    //If valid byte is 0, skip
                    if (len == 0) continue;

                    string s = Encoding.UTF8.GetString(buf, 0, len);
                    string flag = s.Substring(0, 4);
                    s = s.Substring(4);
                    Debug.WriteLine("Message received: " + s);
                    Debug.WriteLine("flag = " + flag);
                    if (flag == "ALL:")
                    {
                        Debug.WriteLine("The player " + clientPoint + " guess all " + s);
                        if(s == listQuestions[indexQuestion].Keyword)
                        {
                            byte[] sendeee = Encoding.UTF8.GetBytes("GESSYou guessed it right. You recieved 3 point");
                            updateScore(clientSocket, 3);
                            foreach (var item in listPlayer)
                            {
                                if(item.Socket == clientSocket)
                                {
                                    item.Socket.Send(sendeee);
                                }
                                else
                                {
                                    byte[] ss = Encoding.UTF8.GetBytes("ANNOPlayer" + item.Nickname + "guessed the word right. Now moving to the next question");
                                    item.Socket.Send(ss);
                                }
                            }
                        }
                        else // guess it wrong => disqualify this player
                        {
                            byte[] sendeee = Encoding.UTF8.GetBytes("ANNOYou guessed it wrong! You have been disqualified!");
                            clientSocket.Send(sendeee);
                            disqualifyPlayer(clientSocket, true);
                            form.Println("The player " + ip.ToString() + " has been disqualified");
                        }
                    }
                    else if (flag == "ONE:") // one character sent
                    {
                        Debug.WriteLine("The current questionIndex is: " + indexQuestion.ToString());
                        Debug.WriteLine("That one character is: " + s);
                        Debug.WriteLine("The current keyword is: " + listQuestions[indexQuestion].Keyword);
                        bool bingo = false; // match any character?
                        for(int i = 0; i < listQuestions[indexQuestion].Keyword.Length; i++)
                        {
                            if(listQuestions[indexQuestion].Keyword[i].ToString() == s)
                            {
                                bingo = true;
                            }
                        }
                        Debug.WriteLine(bingo ? "Match" : "Not match");
                        if(bingo == false) // does not match annouce and move on
                        {
                            byte[] ss = Encoding.UTF8.GetBytes("FLS:");
                            clientSocket.Send(ss);
                        }
                        else // does match, then update everything and send vietn*m back
                        {
                            listQuestions[indexQuestion].updateGuesses(s);
                            string sendee = listQuestions[indexQuestion].updateShowed();
                            Debug.WriteLine("The keyword sent back to client: " + sendee);
                            byte[] ss = Encoding.UTF8.GetBytes("COR:" + sendee);
                            clientSocket.Send(ss);
                        }
                        //TODO: continue next player
                        nextPlayer(ref indexPlayer);
                    }
                    else if(flag == "MSG:") // just a chit-chat message, normal print out on form
                    {
                        Debug.WriteLine("MSG received");
                        form.Println($"{clientPoint}: {s}");
                    }
                    else return;

                    foreach (var item in listPlayer)
                    {
                        byte[] sendee = Encoding.UTF8.GetBytes($"{clientPoint}: {s}");
                        item.Socket.Send(sendee);
                    }

                    //byte[] sendee = Encoding.UTF8.GetBytes("Server returns information");
                    //clientSocket.Send(sendee);
                }
                catch (SocketException e) {
                    //allClientSockets.Remove(clientPoint);
                    form.ComboBoxRemoveItem(clientPoint);

                    form.Println($"Client {clientSocket.RemoteEndPoint} Disconnect： {e.Message}");
                    clientSocket.Close();
                    break;
                }
                catch(Exception e) {
                    form.Println($"Error： {e.Message}");
                }
            }
        }
        static void SendMsg(object sender, EventArgs e)
        {
            string msg = form.GetMsgText();
            if (msg == "") return;
            byte[] sendee = Encoding.UTF8.GetBytes($"Server：{msg}");
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
            Debug.WriteLine("n = " + n.ToString());
            lines = lines.Skip(1).ToArray();
            for (int i = 0; i < n * 2; i += 2)
            {
                listQuestions.Add(new Question(lines[i], lines[i + 1], lines[i].Length,new List<int>(), new List<char>()));
                Debug.WriteLine(lines[i] + '-' + lines[i + 1] + " Size = " + lines[i].Length);
            }
            Debug.WriteLine("The number of question = " + listQuestions.Count.ToString());
        }
        static void LoadQuestions(object sender, EventArgs e)
        {
            Debug.WriteLine("Size = " + listPlayer.Count().ToString());
            foreach (var item in listPlayer)
            {
                Debug.WriteLine(item.Disqualified ? "Kicked" : "In");
            }
            indexQuestion++;
            Debug.WriteLine("Size = " + listPlayer.Count().ToString());
            Debug.WriteLine("indexPlayer = " + indexPlayer.ToString());
            if (!listPlayer[indexPlayer].Disqualified) nextPlayer(ref indexPlayer);
            Debug.WriteLine("Now at " + indexPlayer.ToString());
            for (int index = 0; index < listPlayer.Count(); index++)
            {
                if(index == indexPlayer)
                {
                    byte[] sendeee = Encoding.UTF8.GetBytes("InTurn?" + 1.ToString());
                    listPlayer[index].Socket.Send(sendeee);
                }
                else
                {
                    byte[] sendeee = Encoding.UTF8.GetBytes("InTurn?" + 0.ToString());
                    //Debug.WriteLine("indexPlayer = " + indexPlayer.ToString());
                    //Debug.WriteLine("Total player: " + listPlayer.Count().ToString());
                    listPlayer[indexPlayer].Socket.Send(sendeee);
                }
            }
            Debug.WriteLine("We are at: " + indexQuestion.ToString());
            form.loadQuestion(listQuestions[indexQuestion].Keyword.ToString(), listQuestions[indexQuestion].Hint.ToString());
            string msgQuestion = "QQQ" + listQuestions[indexQuestion].updateShowed() + ' ' + listQuestions[indexQuestion].Hint.ToString();
            byte[] sendee = Encoding.UTF8.GetBytes(msgQuestion);

            // if that player hasn't been disqualified, send question to him
            foreach (var item in listPlayer)
            {
                if(!item.Disqualified) item.Socket.Send(sendee);
            }
            form.Println("Question has been loaded and sent to the clients!");
            if(listQuestions.Count == indexQuestion)
            {
                form.disableLoadQuestionButton();
                finalizeAndShowScoreboard();
            }
        }

        static void finalizeAndShowScoreboard()
        {

        }

    }
}