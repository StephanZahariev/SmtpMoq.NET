using System;

namespace SmtpMoq.Example.WebApi.Model
{
    public class RegisterModel
    {
        public string Email { get; set; }
        public String Password { get; set; }
        public String ConfirmPassword { get; set; }
    }
}
