using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Text;
using System.Diagnostics;
using System.Linq;

namespace GameClient
{
    static class Program
    {
        static Socket clientSocket = null;
        static IPAddress ip = null;
        static IPEndPoint point = null;

        static MainForm form = null;

        static int _turn = 0; // the number of turns this player has been played
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            form = new MainForm(bConnectClick, bSendClick, bGuessClick);
            Application.Run(form);

        }

        static EventHandler bConnectClick = SetConnection;
        static EventHandler bSendClick = SendMsg;
        static EventHandler bGuessClick = SendMsg;
        static void SetConnection(object sender, EventArgs e)
        {
            ip = IPAddress.Parse(form.GetIPText());
            point = new IPEndPoint(ip, form.GetPort());

            clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try {
                //Make a connection
                clientSocket.Connect(point);
                form.SetConnectionStatusLabel(true, point.ToString());
                form.SetButtonSendEnabled(true);
                form.Println($"Connected to the {point} server.");

                // Keep receiving messages from the server
                Thread thread = new Thread(Receive);
                thread.IsBackground = true;
                thread.Start(clientSocket);

            }
            catch (Exception ex) {
                form.Println("Error：" + ex.Message);
            }
        }
        static void Receive(object so)
        {
            Socket send = so as Socket;
            while (true)
            {
                try {
                    //Get the message sent
                    byte[] buf = new byte[1024 * 1024 * 2];
                    int len = send.Receive(buf);
                    if (len == 0) break;
                    string s = Encoding.UTF8.GetString(buf, 0, len);
                    if (s.StartsWith("QQQ"))
                    {
                        System.Diagnostics.Debug.WriteLine("This is QQQ: ");
                        s = s.Substring(3);
                        form.updateQuestion(s);
                        form.Println("A new question has been loaded!");
                    }
                    else if (s.StartsWith("InTurn?"))
                    {
                        Debug.WriteLine("This is InTurn?: ");
                        int turn = s.Last() - '0';
                        form.isItMyTurn(turn);
                        if(turn == 1)
                        {
                            form.Println("IT IS YOUR TURN!");
                        }
                    }
                    else if (s.StartsWith("COR:"))
                    {
                        form.Println("You guessed it right!");
                        s = s.Substring(4);
                        Debug.WriteLine("after " + s.ToString());
                        form.modifyKeyword(s);
                    }
                    else if (s.StartsWith("FLS:"))
                    {
                        form.Println("You guessed it correctly");
                    }
                    else
                    {
                        Debug.WriteLine("CAME TO MESSAGE BOX");
                        form.Println(s);
                    }
                }
                catch (Exception e) {
                    form.SetConnectionStatusLabel(false);
                    form.SetButtonSendEnabled(false);
                    form.Println($"Server disconnected1：{ e.Message}");
                    break;
                }
            }
        }

        static void SendMsg(object sender, EventArgs e)
        {
            form.Println("Ip = " + ip.ToString());
            Debug.WriteLine("SendMsg invoked");
            string msg = form.GetMsgText();
            string ch = form.GetCharacter();
            string guessAll = form.GetStringGuess();
            Debug.WriteLine(ch.ToString() + '-' + guessAll.ToString());
            if (msg != "")
            {
                form.Println(msg);
                msg = "MSG:" + msg;
                byte[] sendee = Encoding.UTF8.GetBytes(msg);
                clientSocket.Send(sendee);
                form.ClearMsgText();
            }
            else if (ch.Length > 1)
            {
                form.Println("Error: Guess for ONE character only");
            }
            else if(ch.Length != 0 && guessAll.Length != 0)
            {
                Debug.WriteLine("Error: You can EITHER guess all OR guess one character (leave one text box blank)");
                form.Println("Error: You can EITHER guess all OR guess one character (leave one text box blank)");
            }
            else if(ch.Length == 1)
            {
                Debug.WriteLine(ch.ToString() + " a character");
                byte[] sendee = Encoding.UTF8.GetBytes("ONE:" + ch.ToLower().ToString());
                clientSocket.Send(sendee);
                form.Println("You have submit your guess");
            }
            else
            {
                Debug.WriteLine(guessAll.ToString() + " all");
                byte[] sendee = Encoding.UTF8.GetBytes("ALL:"+ guessAll);
                clientSocket.Send(sendee);
                form.Println("You have submit your guess");
            }
        }
    }
}
