using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace SmtpMoq.Model
{
    public class SmtpServerSettings
    {
        public Int32 Port
        {
            get;
            set;
        }

        public IPAddress Endpoint
        {
            get;
            set;
        }

        public String ServiceDomain
        {
            get;
            set;
        }

        public SmtpServerSettings()
        {
            this.Endpoint = IPAddress.Parse("127.0.0.1");
            this.Port = 25;
            this.ServiceDomain = "localhost";
        }
    }
}
