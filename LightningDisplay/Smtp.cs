using System;
using Microsoft.SPOT;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Microsoft.SPOT.Net.NetworkInformation;
using System.Threading;
using System.Collections;

namespace LightningDisplay
{
    public class SmtpClient
    {
        private string _SmtpServerName = null;
        private int _Port = 0;
        private enum SmtpState
        {
            NotConnected,
            DomainAccepting,
            MailFromAccepting,
            RecipientAccepting,
            DataCommandAccepting,
            MessageAccepting,
            ConnectionClosing
        };

        SmtpState state;

        public SmtpClient(string SmtpServerName, Int32 Port)
        {
            if (Port < 0)
                throw new ArgumentOutOfRangeException();
            _SmtpServerName = SmtpServerName;
            _Port = Port;
        }

        public void Send(string from, string recipient, string subject, string body,
            bool authenticate = false, string username = "", string password = "")
        {
            if (username != "" && password != "")
            {
                System.Text.Encoding encoding = new System.Text.UTF8Encoding();
                Convert.UseRFC4648Encoding = true;
                username = Convert.ToBase64String(encoding.GetBytes(username));
                password = Convert.ToBase64String(encoding.GetBytes(password));
            }
            state = SmtpState.NotConnected;
            /* Connect to the Server*/
            // Figure out the Server IP Address
            IPHostEntry SmtpServerHostEntry = Dns.GetHostEntry(_SmtpServerName);
            IPEndPoint SmtpServerEndPoint = new IPEndPoint(SmtpServerHostEntry.AddressList[0],
            _Port);
            // Establish the connection with SMTP Server
            Socket SmtpConnection = new Socket(AddressFamily.InterNetwork, SocketType.Stream,
            ProtocolType.Tcp);
            SmtpConnection.Connect(SmtpServerEndPoint);
            String ResponseStr = null;
            while (SmtpConnection.Poll(-1, SelectMode.SelectRead))
            {
                byte[] ReceiveBuffer = new byte[SmtpConnection.Available];
                SmtpConnection.Receive(ReceiveBuffer, ReceiveBuffer.Length, SocketFlags.None);
                // The first 3 bytes hold the message number which is enough for us.
                ResponseStr = new String(Encoding.UTF8.GetChars(ReceiveBuffer), 0, 3);
                int Response = Int16.Parse(ResponseStr);
                switch (state)
                {
                    case SmtpState.NotConnected:
                        if (Response == 220) // Domain service ready.
                        {
                            SmtpConnection.Send(Encoding.UTF8.GetBytes("HELO " +
                                from.Split(new char[] { '@' })[1] + "\r\n"));
                            if (authenticate && username != "" && password != "")
                            {
                                Thread.Sleep(300);
                                SmtpConnection.Send(Encoding.UTF8.GetBytes("AUTH LOGIN\r\n"));
                                Thread.Sleep(300);
                                SmtpConnection.Send(Encoding.UTF8.GetBytes(username + "\r\n"));
                                Thread.Sleep(300);
                                SmtpConnection.Send(Encoding.UTF8.GetBytes(password + "\r\n"));
                                Thread.Sleep(300);
                            }
                            state = SmtpState.DomainAccepting;
                        }
                        break;
                    case SmtpState.DomainAccepting:
                        if (Response == 250) // auth successful (if authentication enabled).
                        {
                            SmtpConnection.Send(Encoding.UTF8.GetBytes("MAIL FROM:<" + from +
                            ">\r\n"));
                            state = SmtpState.MailFromAccepting;
                        }
                        break;
                    case SmtpState.MailFromAccepting:
                        if (Response == 250) // Requested mail action okay, completed.
                        {
                            SmtpConnection.Send(Encoding.UTF8.GetBytes("RCPT TO:<" + recipient
                            + ">\r\n"));
                            state = SmtpState.RecipientAccepting;
                        }
                        break;
                    case SmtpState.RecipientAccepting:
                        if (Response == 250) // Requested mail action okay, completed.
                        {
                            SmtpConnection.Send(Encoding.UTF8.GetBytes("DATA\r\n"));
                            state = SmtpState.DataCommandAccepting;
                        }
                        break;
                    case SmtpState.DataCommandAccepting:
                        if (Response == 354) //Start mail input; end with <CRLF>.<CRLF>.
                        {
                            SmtpConnection.Send(Encoding.UTF8.GetBytes("Subject: " + subject +
                            "\r\n"));
                            SmtpConnection.Send(Encoding.UTF8.GetBytes("From: " + from +
                            "\r\n"));
                            SmtpConnection.Send(Encoding.UTF8.GetBytes("To: " + recipient +
                            "\r\n\r\n"));
                            SmtpConnection.Send(Encoding.UTF8.GetBytes(body + "\r\n"));
                            SmtpConnection.Send(Encoding.UTF8.GetBytes(".\r\n"));
                            state = SmtpState.MessageAccepting;
                        }
                        break;
                    case SmtpState.MessageAccepting:
                        if (Response == 250) // Requested mail action okay, completed.
                        {
                            SmtpConnection.Send(Encoding.UTF8.GetBytes("QUIT\r\n"));
                            state = SmtpState.ConnectionClosing;
                        }
                        break;
                    case SmtpState.ConnectionClosing:
                        if (Response == 221) // Requested mail action okay, completed.
                        {
                            SmtpConnection.Close();
                            return;
                        }
                        break;
                }
            }
            SmtpConnection.Close();
            throw new Exception("SMTP connection Timeout");
        }
    }
}
