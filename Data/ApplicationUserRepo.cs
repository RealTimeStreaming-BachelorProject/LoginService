using System.Threading.Tasks;
using LoginService.Data.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace LoginService.Data
{
    public class ApplicationUserRepo
    {
        private readonly DataContext _dataContext;
        private readonly UserManager<ApplicationUser> _userManager;

        
        public ApplicationUserRepo(DataContext dataContext, UserManager<ApplicationUser> userManager)
        {
            _dataContext = dataContext;
            _userManager = userManager;
        }

        public async Task<string> UpdatePasswordEmulation(string username, string newPassword)
        {
            var user = await _userManager.FindByNameAsync(username);
            if (user == null)
                return null;
            user.PasswordUpdateThing = newPassword;
            await _dataContext.SaveChangesAsync();
            return user.Id;
        }
    }
}