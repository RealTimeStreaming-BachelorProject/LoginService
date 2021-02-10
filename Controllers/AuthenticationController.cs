using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LoginService.Contracts;
using LoginService.Data.DTOs.InputDTOs;
using LoginService.Data.DTOs.OutputDTOs;
using LoginService.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace LoginService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        private static readonly SigningCredentials SigningCreds =
            new(Startup.SecurityKey, SecurityAlgorithms.HmacSha256);

        public AuthenticationController(UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginDTO input)
        {
            var signInResult = await _signInManager.PasswordSignInAsync(input.Username, input.Password, false, false);
            if (!signInResult.Succeeded)
            {
                return StatusCode(StatusCodes.Status401Unauthorized, new GenericReturnMessageDTO
                {
                    StatusCode = 401,
                    Message = ErrorMessages.IncorrectCredentials
                });
            }

            var user = await _userManager.FindByNameAsync(input.Username);
            var claims = new[]
            {
                new Claim("username", user.UserName),
                new Claim("driverID", user.Id)
            };

            var jwtIssuerAuthority = Environment.GetEnvironmentVariable("LOGINSERVICE_JWT_ISSUER");
            var token = new JwtSecurityToken(jwtIssuerAuthority, jwtIssuerAuthority, claims,
                signingCredentials: SigningCreds, expires: DateTime.Now.AddDays(30));
            return StatusCode(StatusCodes.Status200OK, new TokenResponseDTO
            {
                StatusCode = 200,
                Message = SuccessMessages.DriverLoggedIn,
                Token = new JwtSecurityTokenHandler().WriteToken(token)
            });
        }

        [HttpPost("register")]
        public async Task<ActionResult> RegisterUser(RegisterDTO input)
        {
            var user = await _userManager.FindByNameAsync(input.Username);
            if (user != null) // User already exists
                return StatusCode(StatusCodes.Status400BadRequest, new GenericReturnMessageDTO
                {
                    StatusCode = 400,
                    Message = ErrorMessages.DriverAlreadyExists
                });

            // create a new user
            var newUser = new ApplicationUser {UserName = input.Username};
            var result = await _userManager.CreateAsync(newUser, input.Password);

            if (!result.Succeeded)
                return StatusCode(StatusCodes.Status400BadRequest, new GenericReturnMessageDTO
                {
                    StatusCode = 400,
                    Message = result.Errors.Select(e => e.Description)
                });

            return StatusCode(StatusCodes.Status201Created, new GenericReturnMessageDTO
            {
                StatusCode = 201,
                Message = SuccessMessages.DriverCreated,
            });
        }
    }
}