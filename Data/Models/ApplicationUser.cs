using Microsoft.AspNetCore.Identity;

namespace LoginService.Data.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string PasswordUpdateThing { get; set; }
    }
}