using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SmtpMoq.Example.Blazor.Data
{
    public class Email
    {
        [Required]
        [EmailAddress]
        [Display(Name = "From")]
        public string From { get; set; }

        [Required]
        [EmailAddress]
        [Display(Name = "To")]
        public string To { get; set; }

        [Required]
        [Display(Name = "Subject")]
        public string Subject { get; set; }
        public string Body { get; set; }
    }
}
