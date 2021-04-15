using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using LoginService.Contracts;
using LoginService.Data;
using LoginService.Data.DTOs.InputDTOs;
using LoginService.Data.DTOs.OutputDTOs;
using LoginService.Data.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace LoginService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class AuthenticationController : ControllerBase
    {
        private readonly AuthRepo _authRepo;

        private static readonly SigningCredentials SigningCreds =
            new(Startup.SecurityKey, SecurityAlgorithms.HmacSha256);

        public AuthenticationController(AuthRepo authRepo)
        {
            _authRepo = authRepo;
        }

        private static string CreateJwToken(string username, Guid driverId)
        {
            var claims = new[]
            {
                new Claim("username", username),
                new Claim("driverID", driverId.ToString())
            };

            var jwtIssuerAuthority = Startup.environmentVariables.JwtIssuerAuthorithy;
            var token = new JwtSecurityToken(jwtIssuerAuthority, jwtIssuerAuthority, claims,
                signingCredentials: SigningCreds, expires: DateTime.Now.AddDays(30));
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // TODO: Create request which can validate a JWT. It is important to validate users last password change up against the token creation time

        [HttpPost("login")]
        public async Task<ActionResult> Login(LoginDTO input)
        {
            try
            {
                var loggedInDriver = await _authRepo.Login(input);
                if (loggedInDriver == null)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, new GenericReturnMessageDTO
                    {
                        StatusCode = 401,
                        Message = ErrorMessages.IncorrectCredentials
                    });
                }

                return StatusCode(StatusCodes.Status200OK, new TokenResponseDTO
                {
                    StatusCode = 200,
                    Message = SuccessMessages.DriverLoggedIn,
                    Token = CreateJwToken(input.Username, loggedInDriver.Id)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPost("register")]
        public async Task<ActionResult> RegisterUser(RegisterDTO input)
        {
            try
            {
                var registeredDriver = await _authRepo.RegisterUser(input);
                if (registeredDriver == null)
                    return StatusCode(StatusCodes.Status400BadRequest, new GenericReturnMessageDTO
                    {
                        StatusCode = 400,
                        Message = "Could not register user"
                    });

                return StatusCode(StatusCodes.Status201Created, new TokenResponseDTO
                {
                    StatusCode = 201,
                    Message = SuccessMessages.DriverCreated,
                    Token = CreateJwToken(input.Username, registeredDriver.Id)
                });
            }
            catch (System.Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }

        [HttpPatch]
        public async Task<ActionResult> ChangePassword(UpdateDTO input)
        {
            try
            {
                var passwordUpdatedSuccessfully =
                    await _authRepo.UpdatePasswordEmulation(input.Username, input.NewPassword);

                if (!passwordUpdatedSuccessfully)
                {
                    return StatusCode(StatusCodes.Status401Unauthorized, new GenericReturnMessageDTO
                    {
                        StatusCode = 401,
                        Message = ErrorMessages.IncorrectCredentials
                    });
                }

                return StatusCode(StatusCodes.Status200OK, new GenericReturnMessageDTO
                {
                    StatusCode = 200,
                    Message = SuccessMessages.PasswordUpdated,
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(StatusCodes.Status500InternalServerError);
            }
        }
    }
}