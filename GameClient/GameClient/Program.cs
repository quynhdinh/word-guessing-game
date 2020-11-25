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
        static EventHandler bGuessClick = GuessingKeyword;
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
                form.Println("Choose your nickname: ");
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
                try
                {
                    //Get the message sent
                    byte[] buf = new byte[1024 * 1024 * 2];
                    int len = send.Receive(buf);

                    if (len == 0) break;
                    string[] ss = Encoding.UTF8.GetString(buf, 0, len).Split('/');
                    foreach (string i in ss)
                    {
                        string s = i;
                        if (s.StartsWith("QQQ"))
                        {
                            s = s.Substring(3);
                            form.updateQuestion(s);
                            form.Println("A new question has been loaded!");
                        }
                        else if (s.StartsWith("InTurn?"))
                        {
                            int turn = s.Last() == '0' ? 0 : 1;
                            Debug.WriteLine("Turn = " + turn.ToString());
                            form.isItMyTurn(turn);
                            if (turn == 1)
                            {
                                form.Println("It is your turn");
                            }
                            else
                            {
                                form.Println("Be pateint. Wait for your turn");
                            }
                        }
                        else if (s.StartsWith("COR:"))
                        {
                            form.Println("You guessed it right!");
                            s = s.Substring(4);
                            Debug.WriteLine("after " + s.ToString());
                            form.modifyKeyword(s);
                        }
                        else if (s.StartsWith("UDT:"))
                        {
                            form.Println("The keyword has been updated!");
                            s = s.Substring(4);
                            form.modifyKeyword(s);
                        }
                        else if (s.StartsWith("FLS:"))
                        {
                            form.Println("You guessed it incorrectly");
                        }
                        else if(s.StartsWith("ANNO") || s.StartsWith("GESS"))
                        {
                            s = s.Substring(4);
                            form.Println(s);
                        }
                        else
                        {
                            form.Println(s);
                        }
                    }
                }
                catch (Exception e)
                {
                    form.SetConnectionStatusLabel(false);
                    form.SetButtonSendEnabled(false);
                    form.Println($"Server disconnected1：{ e.Message}");
                    break;
                }
            }
        }

        static void SendMsg(object sender, EventArgs e)
        {
            string msg = form.GetMsgText();
            if (msg != "")
            {
                form.Println(msg);
                msg = "MSG:" + msg;
                byte[] sendee = Encoding.UTF8.GetBytes(msg);
                clientSocket.Send(sendee);
                form.ClearMsgText();
            }
        }
        static bool alreadyShown(List<char> a, char ch)
        {
            foreach (var item in a)
                if(item == ch)
                    return true;
            return false;
        }
        static void GuessingKeyword(object sender, EventArgs e)
        {
            string guess = form.GetStringGuess();
            
            if (guess.Length == 1) // Guessing one character only
            {
                if (alreadyShown(form.getShownCharacters(), guess[0]))
                {
                    form.Println("This character is already shown. Please choose another one");
                    return;
                }
                byte[] sendee = Encoding.UTF8.GetBytes("ONE:" + guess.ToLower().ToString());
                clientSocket.Send(sendee);
                form.Println("You have submit your guess");
            }
            else // Guessing the whole keyword
            {
                byte[] sendee = Encoding.UTF8.GetBytes("ALL:" + guess);
                clientSocket.Send(sendee);
                form.Println("You have submit your guess");
            }
        }
    }
}
