using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace SMTP
{
    class SMTPStart
    {
        static void Main(string[] args)
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, 1025);
            TcpListener listener = new TcpListener(endPoint);
            listener.Start();
            LinkedList<SMTPServer> servers = new LinkedList<SMTPServer>();

            int file = 0;
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                SMTPServer handler = new SMTPServer(client, file);
                file += 1;
                servers.AddFirst(handler);
                Thread thread = new System.Threading.Thread(new ThreadStart(handler.Run));
                thread.Start();
            }
        }
    }


    class SMTPServer
    {
        TcpClient client;
        String from = "";
        LinkedList<String> to = new LinkedList<String>();
        int number;
        int file = 1;
        String username = "";

        public SMTPServer(TcpClient client, int i)
        {
            this.client = client;
            Write("Hello user! Welcome!");
            this.number = i;
        }

        public void Run()
        {
            string strMessage = String.Empty;
            while (true)
            {
                try
                {
                    strMessage = Read();
                }
                catch (Exception e)
                {
                    //a socket error has occured
                    break;
                }

                if (strMessage.Length > 0)
                {
                    if (strMessage.StartsWith("QUIT"))
                    {
                        Write("221: Bye");
                        client.Close();
                        break;//exit while
                    }
                    //message has successfully been received
                    if (strMessage.StartsWith("EHLO"))
                    {
                        username = strMessage[5..];
                        Write("250 OK");
                    }

                    if (strMessage.StartsWith("RCPT TO"))
                    {
                        strMessage = strMessage.Replace("<", "");
                        strMessage = strMessage.Replace(">", "");
                        to.AddLast(strMessage[8..]);
                        Write("Your mail now will be sent to: " + strMessage[8..].Remove(strMessage[8..].Length - 1));
                        Write("250 OK");
                    }

                    if (strMessage.StartsWith("MAIL FROM"))
                    {
                        from = strMessage[10..];
                        from = from.Replace("<", "");
                        from = from.Replace(">", "");
                        Write("You are sending from: " + from.Remove(from.Length - 1));
                        Write("250 OK");
                    }

                    if (strMessage.StartsWith("DATA"))
                    {
                        bool IsEmpty = false;
                        if (to.Count == 0)
                        {
                            Write("Specify email to send!");
                            IsEmpty = true;
                        }

                        if (from.Length == 0)
                        {
                            Write("Specify your email!");
                            IsEmpty = true;
                        }

                        if (IsEmpty)
                        {
                            continue;
                        }

                        Write("354 Start mail input; end with '.' as a first symbol on line");
                        StringBuilder endMessage = new StringBuilder();
                        endMessage.AppendLine("From:" + from.Remove(from.Length - 1));

                        endMessage.AppendLine("To: " + to.ElementAt(0).Remove(to.ElementAt(0).Length - 1));
                        endMessage.Append("Cc: ");
                        for (int i = 1; i < to.Count; i++)
                        {
                            endMessage.Append(to.ElementAt(i).Remove(to.ElementAt(i).Length - 1) + " ");
                        }
                        string date = DateTime.UtcNow.ToString("MM-dd-yyyy");
                        endMessage.Append("\nDate: " + date + "\n");
                        while (true)
                        {
                            strMessage = Read();
                            if (strMessage.StartsWith("."))
                            {
                                break;
                            } else
                            {
                                endMessage.AppendLine(strMessage.Remove(strMessage.Length - 1));
                            }
                        }

                        Write("Email:\n" + endMessage.ToString());
                        string env = System.AppContext.BaseDirectory;
                        if (!Directory.Exists(env + @"\Mails\"))
                        {
                            Directory.CreateDirectory(env + @"\Mails\");
                        }

                        string path = env + @"\Mails\" + number.ToString() + "_" + file.ToString() + "_" + "mail.txt";
                        file += 1;
                        using (StreamWriter sw = File.CreateText(path))
                        {
                            sw.Write(endMessage.ToString());
                        }
                        Write("Email stored!");
                        Write("250 OK");
                    }
                }
            }
        }

        private void Write(String strMessage)
        {
            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();
            byte[] buffer = encoder.GetBytes(strMessage + "\r\n");

            clientStream.Write(buffer, 0, buffer.Length);
            clientStream.Flush();
        }

        private String Read()
        {
            byte[] messageBytes = new byte[8192];
            int bytesRead = 0;
            NetworkStream clientStream = client.GetStream();
            ASCIIEncoding encoder = new ASCIIEncoding();
            bytesRead = clientStream.Read(messageBytes, 0, 8192);
            string strMessage = encoder.GetString(messageBytes, 0, bytesRead);
            return strMessage;
        }
    }
}
