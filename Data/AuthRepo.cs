using System;
using System.Linq;
using System.Threading.Tasks;
using Cassandra;
using LoginService.Data.DTOs.InputDTOs;
using LoginService.Data.Models;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace LoginService.Data
{
    public class AuthRepo : IAuthRepo
    {
        private readonly Cluster _cluster;
        private static string _keyspace = "loginservice";

        public AuthRepo(Cluster dataContext)
        {
            _cluster = dataContext;
        }

        public async Task<bool> UpdatePasswordEmulation(string username, string newPassword)
        {
            ISession session = null;
            try
            {
                session = await _cluster.ConnectAsync(_keyspace);
                var driver = await FindDriver(username);

                var preparedStatement =
                    await session.PrepareAsync("UPDATE drivers SET passwordUpdateEmulation = ? WHERE id = ?");
                var statement = preparedStatement.Bind(newPassword, driver.Id);
                
                await session.ExecuteAsync(statement);

                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
            finally
            {
                session?.Dispose();
            }
        }

        public async Task<Driver> RegisterUser(RegisterDTO registerDto)
        {
            ISession session = null;
            try
            {
                session = await _cluster.ConnectAsync(_keyspace);
                var userId = Guid.NewGuid();
                var hashedPassword = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

                var preparedStmt = await session.PrepareAsync(
                    "INSERT INTO drivers (id, username, hashedPassword, passwordUpdateEmulation) VALUES (?,?,?,'start')");
                var selectStmt = preparedStmt.Bind(userId, registerDto.Username, hashedPassword);
                await session.ExecuteAsync(selectStmt);

                return new Driver
                {
                    Id = userId,
                    Username = registerDto.Username,
                    HashedPassword = hashedPassword,
                    PasswordUpdateEmulation = "start"
                };
            }
            catch (InvalidQueryException e)
            {
                Console.WriteLine(e);
                return null;
            }
            finally
            {
                session?.Dispose();
            }
        }

        private async Task<Driver> FindDriver(string username)
        {
            ISession session = null;

            try
            {
                session = await _cluster.ConnectAsync(_keyspace);

                var preparedStmt = await session.PrepareAsync(
                    "SELECT * FROM drivers WHERE username = ?");
                var selectStmt = preparedStmt.Bind(username);
                var result = (await session.ExecuteAsync(selectStmt)).GetRows().ToList();
                if (!result.Any())
                    return null;

                var driverRow = result.First();

                return new Driver
                {
                    Id = driverRow.GetValue<Guid>("id".ToLower()),
                    Username = driverRow.GetValue<string>("username".ToLower()),
                    HashedPassword = driverRow.GetValue<string>("hashedPassword".ToLower()),
                    PasswordUpdateEmulation = driverRow.GetValue<string>("passwordUpdateEmulation".ToLower())
                };
            }
            catch (InvalidQueryException e) 
            {
                Console.WriteLine(e);
                return null;
            }

            finally
            {
                session?.Dispose();
            }
        }


        public async Task<Driver> Login(LoginDTO loginDto)
        {
            try
            {
                var loggedInDriver = await FindDriver(loginDto.Username);
                if (loggedInDriver == null)
                    return null;

                return BCrypt.Net.BCrypt.Verify(loginDto.Password, loggedInDriver.HashedPassword) ? loggedInDriver : null;
            }
            catch (InvalidQueryException e)
            {
                Console.WriteLine(e);
                return null;
            }
        }
    }
}