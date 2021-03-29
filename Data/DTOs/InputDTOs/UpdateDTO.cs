using System.ComponentModel.DataAnnotations;
using LoginService.Contracts;

namespace LoginService.Data.DTOs.InputDTOs
{
    public class UpdateDTO
    {
        public string Username { get; set; }
        public string NewPassword { get; set; } 
    }
}