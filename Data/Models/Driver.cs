
using System;

namespace LoginService.Data.Models
{
    public class Driver
    {
        public Guid Id { get; set; }
        public string Username { get; set; }
        public string HashedPassword { get; set; }
        public string PasswordUpdateEmulation { get; set; }
    }
}