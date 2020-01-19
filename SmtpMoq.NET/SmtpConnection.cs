using SmtpMoq.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq;
using SmtpMoq.Repository;

namespace SmtpMoq
{
    internal class SmtpConnection
    {
        private TcpClient tcpConnection;
        private SmtpServer server;
        private NetworkStream smtpWriter;
        private StreamReader smtpReader;
        IEmailRepository emailRepository;

        private const int timeout = 60 * 1000;
        private const string newLine = "\r\n";

        internal SmtpConnection(TcpClient tcpConnection, SmtpServer server, IEmailRepository emailRepository)
        {
            this.tcpConnection = tcpConnection;
            this.server = server;
            this.emailRepository = emailRepository;
        }

        internal async Task Process()
        {
            async Task Send250Ok(string message = null)
            {
                await SendMessageAsync($"250 Ok" + (message == null ? String.Empty : ": " + message));
            }

            try
            {
                this.smtpWriter = this.tcpConnection.GetStream();
                this.smtpReader = new StreamReader(this.smtpWriter, Encoding.UTF8, false, 4069, true);

                await SendMessageAsync("220 " + this.server.Settings.ServiceDomain);

                EmailMessage receivedEmail = new EmailMessage();

                string message;
                while ((message = await ReceiveMessageAsync()) != null)
                {
                    string command = ParseCommand(message);
                    string payload = ParsePayload(message, command);

                    switch (command)
                    {
                        case "HELO":
                            await SendMessageAsync($"250 {this.server.Settings.ServiceDomain} SmtpMoq server responding");
                            break;

                        case "EHLO":
                            await SendMessageAsync($"250 {this.server.Settings.ServiceDomain} SmtpMoq server responding");
                            await SendMessageAsync($"250-SMTPUTF8");
                            break;

                        case "NOOP":
                            await Send250Ok();
                            break;

                        case "QUIT":
                            await SendMessageAsync("221 It was nice talking to you. Bye.");
                            return;

                        case "RSET":
                            receivedEmail = new EmailMessage();
                            await Send250Ok();
                            break;

                        case "MAIL FROM":
                            receivedEmail.From = ExtractString(payload, "<", ">");
                            await Send250Ok();
                            break;

                        case "RCPT TO":
                            string recipient = ExtractString(payload, "<", ">");
                            receivedEmail.Recipients = receivedEmail.Recipients.Concat(new string[] { recipient }).ToArray();
                            await Send250Ok();
                            break;

                        case "DATA":
                            await SendMessageAsync("354 Start mail input; end with <CRLF>.<CRLF>");

                            string line;
                            StringBuilder receivedData = new StringBuilder();
                            while ((line = await ReceiveMessageAsync()) != ".")
                            {
                                receivedData.Append(line);
                            }
                            receivedEmail.Data = receivedData.ToString();
                            receivedEmail.Guid = Guid.NewGuid();
                            emailRepository.AddEmail(receivedEmail);
                            await Send250Ok("queued as " + receivedEmail.Guid.ToString());
                            break;

                        case "VRFY":
                            await SendMessageAsync("250 " + payload);
                            break;

                        default:
                            await SendMessageAsync("500 Unknow command");
                            break;
                    }
                }
            }
            finally
            {
                this.server.CloseConnection(this.tcpConnection);
            }
        }

        private async Task<String> ReceiveMessageAsync()
        {
            CancellationTokenSource readCancellation = new CancellationTokenSource();

            var readTask = this.smtpReader.ReadLineAsync();
            var cancellationTask = Task.Delay(timeout, readCancellation.Token);

            if (await Task.WhenAny(readTask, cancellationTask) == cancellationTask)
                throw new TimeoutException();

            readCancellation.Cancel();

            return readTask.Result;
        }

        private async Task SendMessageAsync(String line, bool appendNewLine = true)
        {
            var data = Encoding.UTF8.GetBytes(line + (appendNewLine ? newLine : String.Empty));
            await this.smtpWriter.WriteAsync(data, 0, data.Length);
        }

        private string ParseCommand(string message)
        {
            int colonIndex = message.IndexOf(':');
            if (colonIndex > 0)
            {
                return message.Substring(0, colonIndex);
            }

            string uppercaseMessage = message.ToUpperInvariant();
            if (uppercaseMessage.StartsWith("HELLO "))
            {
                return "HELLO";
            }
            else if (uppercaseMessage.StartsWith("EHLO "))
            {
                return "EHLO";
            }
            else if (uppercaseMessage.StartsWith("VRFY "))
            {
                return "VRFY";
            }

            return uppercaseMessage;
        }

        private string ParsePayload(string message, string command)
        {
            if (message.Length == command.Length)
            {
                return String.Empty;
            }

            string payload = message.Substring(command.Length + 1).Trim();

            return payload;
        }

        string ExtractString(string s, string startTag, string endTag)
        {
            int startIndex = s.IndexOf(startTag) + startTag.Length;
            int endIndex = s.IndexOf(endTag, startIndex);
            return s.Substring(startIndex, endIndex - startIndex);
        }
    }
}
