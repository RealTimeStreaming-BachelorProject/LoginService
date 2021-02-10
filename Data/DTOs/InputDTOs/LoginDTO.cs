using System.ComponentModel.DataAnnotations;
using LoginService.Contracts;

namespace LoginService.Data.DTOs.InputDTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = ErrorMessages.UsernameRequired)]  
        public string Username { get; set; }  
  
        [Required(ErrorMessage = ErrorMessages.PasswordRequired)]  
        public string Password { get; set; } 
    }
}