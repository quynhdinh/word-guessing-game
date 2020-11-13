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
        static Dictionary<string, Socket> allClientSockets = null;
        static List<string> clientsQueue;
        static MainForm form = null;

        static Dictionary<string, int> scoreboard = null;
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            allClientSockets = new Dictionary<string, Socket>();
            scoreboard = new Dictionary<string, int>();
            clientsQueue = new List<string>();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new MainForm(bListenClick, bSendClick, bLoadQuestionClick);
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

                    allClientSockets.Add(clientPoint, clientSocket);
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

        static void Receive(object so)
        {
            Socket clientSocket = so as Socket;
            string clientPoint = clientSocket.RemoteEndPoint.ToString();
            Debug.WriteLine("Received from: " + clientPoint);
            while (true) {
                try {
                    //Get the sent message container
                    byte[] buf = new byte[1024 * 1024 * 2];
                    int len = clientSocket.Receive(buf);
                    //If valid byte is 0, skip
                    if (len == 0) break;

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
                            byte[] sendeee = Encoding.UTF8.GetBytes("GESSYou guessed it right. You recieved 1 point");
                            allClientSockets[clientPoint].Send(sendeee);
                            foreach (var item in allClientSockets)
                            {
                                if(item.Key != clientPoint)
                                {
                                    byte[] ss = Encoding.UTF8.GetBytes("ANNOPlayer" + ip.ToString() + "guessed the word right. Now moving to the next question");
                                    allClientSockets[item.Key].Send(ss);
                                }
                            }
                        }
                        else
                        {
                            byte[] sendeee = Encoding.UTF8.GetBytes("ANNOYou guessed it wrong! You have been disqualified!");
                            allClientSockets[clientPoint].Send(sendeee);
                            allClientSockets.Remove(clientPoint);
                            form.Println("The player " + ip.ToString() + " has been disqualified");

                        }
                    }
                    else if (flag == "ONE:")
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
                        if(bingo == false) // does not match
                        {
                            byte[] ss = Encoding.UTF8.GetBytes("FLS:");
                            allClientSockets[clientPoint].Send(ss);
                        }
                        else // does match update everything and send vietn*m back
                        {
                            listQuestions[indexQuestion].updateGuesses(s);
                            string sendee = listQuestions[indexQuestion].updateShowed(s);
                            Debug.WriteLine("The keyword sent back to client: " + sendee);
                            byte[] ss = Encoding.UTF8.GetBytes("COR:" + sendee);
                            allClientSockets[clientPoint].Send(ss);
                        }
                    }
                    else if(flag == "MSG:")
                    {
                        Debug.WriteLine("MSG received");
                        form.Println($"{clientPoint}: {s}");
                    }
                    else return;

                    foreach (Socket t in allClientSockets.Values) {
                        byte[] sendee = Encoding.UTF8.GetBytes($"{clientPoint}: {s}");
                        t.Send(sendee);
                    }

                    //byte[] sendee = Encoding.UTF8.GetBytes("Server returns information");
                    //clientSocket.Send(sendee);
                }
                catch (SocketException e) {
                    allClientSockets.Remove(clientPoint);
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
            foreach(Socket s in allClientSockets.Values)
                s.Send(sendee);
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
            indexQuestion++;
            foreach (String s in allClientSockets.Keys)
            {
                clientsQueue.Add(s);
            }
            foreach(var x in allClientSockets.Select((entry, index)=> new { entry, index }))
            {
                //Debug.WriteLine("{0}: {1} = {2}", x.index, x.entry.Key, x.entry.Value);
                if(indexPlayer == x.index)
                {
                    byte[] sendeee = Encoding.UTF8.GetBytes("InTurn?" + 1.ToString());
                    x.entry.Value.Send(sendeee);
                }
                else
                {
                    byte[] sendeee = Encoding.UTF8.GetBytes("InTurn?" + 0.ToString());
                    x.entry.Value.Send(sendeee);
                }
            }
            loadData();
            Debug.WriteLine("We are at: " + indexQuestion.ToString());
            form.loadQuestion(listQuestions[indexQuestion].Keyword.ToString(), listQuestions[indexQuestion].Hint.ToString());
            string msgQuestion = "QQQ" + listQuestions[indexQuestion].Keyword.ToString() + ' ' + listQuestions[indexQuestion].Hint.ToString();
            byte[] sendee = Encoding.UTF8.GetBytes(msgQuestion);
            foreach (Socket s in allClientSockets.Values)
                s.Send(sendee);
            form.Println("Question has been loaded and sent to the clients!");
            if(listQuestions.Count == indexQuestion)
            {
                form.disableLoadQuestionButton();
            }
        }

    }
}