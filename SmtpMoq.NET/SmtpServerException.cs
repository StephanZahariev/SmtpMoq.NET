using System;
using System.Collections.Generic;
using System.Text;

namespace SmtpMoq
{
    public class SmtpServerException: ApplicationException
    {
        public SmtpServerException(string message):
            base(message)
        {

        }
    }
}
