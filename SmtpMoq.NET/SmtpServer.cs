using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using SmtpMoq.Model;
using SmtpMoq.Repository;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SmtpMoq
{
    public class SmtpServer
    {
        private volatile bool isShutdownInitiated;
        private ILogger logger;
        private IEmailRepository emailRepository;

        private TcpListener smtpListener;
        private readonly SynchronizedCollection<TcpClient> connectionsToDispose;

        public SmtpServerSettings Settings
        {
            get;
            private set;
        }

        public SmtpServer()
            : this(new SmtpServerSettings(), new InMemoryEmailRepository(), NullLogger.Instance)
        {
        }

        public SmtpServer(ILogger logger)
            : this(new SmtpServerSettings(), new InMemoryEmailRepository(), logger)
        {
        }

        public SmtpServer(SmtpServerSettings settings, IEmailRepository emailRepository, ILogger logger)
        {

            this.Settings = settings;
            this.emailRepository = emailRepository;
            this.logger = logger;
            this.smtpListener = null;
            this.connectionsToDispose = new SynchronizedCollection<TcpClient>();
        }

        public async Task StartAsync()
        {
            this.isShutdownInitiated = false;

            this.smtpListener = new TcpListener(this.Settings.Endpoint, this.Settings.Port);
            this.smtpListener.Start();
            try
            {
                this.logger.LogInformation("SmtpMoq server started listening on " + this.Settings.Endpoint + ":" + this.Settings.Port);
                while (this.isShutdownInitiated == false)
                {
                    TcpClient tcpConnection = null;
                    try
                    {
                        tcpConnection = await this.smtpListener.AcceptTcpClientAsync();
                    }
                    catch (ObjectDisposedException)
                    {
                        //when TcpListener is stopped. Do nothing
                    }

                    if (tcpConnection != null)
                    {
                        this.connectionsToDispose.Add(tcpConnection);
                        SmtpConnection smtpConnection = new SmtpConnection(tcpConnection, this, this.emailRepository);
                        _ = smtpConnection.Process();

                        this.logger.LogInformation("SmtpMoq client connected: " + tcpConnection.Client.RemoteEndPoint.ToString() +
                            " (Total: " + this.connectionsToDispose.Count + ")");
                    }
                }
            }
            catch
            {
                Stop();
                throw;
            }
        }

        public void Stop()
        {
            if (this.isShutdownInitiated == false)
            {
                if (this.smtpListener == null)
                {
                    throw new SmtpServerException("Server not started but shutdown requested");
                }

                this.isShutdownInitiated = true;
                this.smtpListener.Stop();
                foreach (var connection in this.connectionsToDispose)
                {
                    CloseConnection(connection);
                }
                this.logger.LogInformation($"SmtpMoq server stopped listening on {this.Settings.Endpoint}:{this.Settings.Port}");
            }
        }

        internal void CloseConnection(TcpClient connection)
        {
            string endpoint = connection.Client.RemoteEndPoint.ToString();
            int activeConnections = this.connectionsToDispose.Count;

            this.connectionsToDispose.Remove(connection);
            connection.Close();

            this.logger.LogInformation($"SmtpMoq client disconnected: {endpoint} (Active remaingin connections: {activeConnections - 1})");
        }

        public IEmailRepository ReceivedMessages
        {
            get
            {
                return this.emailRepository;
            }
        }
    }
}
