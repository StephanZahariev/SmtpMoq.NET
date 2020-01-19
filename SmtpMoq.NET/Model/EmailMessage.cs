using System;
using System.Collections.Generic;
using System.Text;

namespace SmtpMoq.Model
{
    public class EmailMessage
    {
        public EmailMessage()
        {
            Recipients = new String[] { };
        }

        public Guid Guid { get; set; }
        public String From { get; set; }
        public String[] Recipients { get; set; }
        public String Data { get; set; }

        public String RecipientsCommaSeparated
        {
            get
            {
                return String.Join(", ", Recipients);
            }
        }
    }
}
