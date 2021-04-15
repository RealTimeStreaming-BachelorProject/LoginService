using System.Threading.Tasks;
using LoginService.Data.DTOs.InputDTOs;
using LoginService.Data.Models;

namespace LoginService.Data
{
    public interface IAuthRepo
    {
        Task<Driver> RegisterUser(RegisterDTO registerDto);
        Task<Driver> Login(LoginDTO loginDto);
        Task<bool> UpdatePasswordEmulation(string username, string newPassword);
    }
}