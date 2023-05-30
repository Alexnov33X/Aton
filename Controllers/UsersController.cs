using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Aton.Models;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.Text;
using NuGet.Protocol.Plugins;
using Microsoft.AspNetCore.Authorization;
using Swashbuckle.AspNetCore.Annotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient.Server;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Reflection;
using System.Xml.Linq;

namespace Aton.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly UserContext _context;

        public UsersController(UserContext context)
        {
            _context = context;
        }
       
        public static string GenerateNewToken(User user)
        {
            // Create a JWT token for the user
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("seeeecret");
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new Claim[]
                {
            new Claim(ClaimTypes.NameIdentifier, user.Guid.ToString()),
            new Claim(ClaimTypes.Name, user.Login)}),
                Expires = DateTime.UtcNow.AddHours(24),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);

            // Store the JWT token in the user object
            return tokenHandler.WriteToken(token);

        }

        // POST: api/UsersControllerEntity 1) Создание пользователя по логину, паролю, имени, полу и дате рождения + указание будет ли
        //пользователь админом(Доступно Админам)
        [HttpPost("Create user")]
        [ProducesResponseType(typeof(User), 201)]
        public async Task<ActionResult<User>> CreateUser( string userLogin, string userPassword, string userName, int userGender, DateTime userBirthday, bool userIsAdmin, string yourLogin = "Admin", string yourPassword = "Admin")
        {
            if (_context.UserItems == null)
            {
                return Problem("Entity set 'UserContext.UserItems' is null.");
            }
            var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

            if (admin == null)
                return NotFound("User not found");

            if (admin.Password != yourPassword)
                return BadRequest("Incorrect password");

            if (!admin.Admin)
                return Unauthorized("You do not have access to this method");
            //var userClaims = User.Claims;
            //var loginClaim = userClaims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value;
            //var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == loginClaim);
            try
            {
                User user = new User
                {
                    Login = userLogin,
                    Password = userPassword,
                    Name = userName,
                    Gender = userGender,
                    Birthday = userBirthday,
                    Admin = userIsAdmin,
                    CreatedOn = DateTime.Now,
                    CreatedBy = yourLogin,
                    ModifiedBy = "",
                    ModifiedOn = null,
                    RevokedBy = "",
                    RevokedOn = null
                };
                //user.JwtToken = GenerateNewToken(user);
                _context.UserItems.Add(user);

                await _context.SaveChangesAsync();

                //var response = new
                //{
                //    user.Login,
                //    user.Password,
                //    //Token = user.JwtToken
                //};

                return CreatedAtAction(nameof(CreateUser), user);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

//Изменение имени, пола или даты рождения пользователя(Может менять Администратор, либо
//лично пользователь, если он активен (отсутствует RevokedOn))
        [HttpPut("Update profile")]

        public async Task<IActionResult> UpdateOneProfile(string? userLogin, string? name = null, int? gender = null, DateTime? birthday = null, string yourLogin = "Admin", string yourPassword = "Admin")
        {

            var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

            if (admin == null)
                return NotFound("User not found");

            if (admin.Password != yourPassword)
                return BadRequest("Incorrect password");

            if (!admin.Admin && admin.RevokedOn != null)
                return Unauthorized("You do not have access to this method. Your account is revoked");

            if (admin.Admin)
            {
                try
                {
                    var user = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == userLogin);
                    if (user == null)
                        return BadRequest("User to be changed not found");
                    if (name != null)
                        user.Name = name;
                    if (gender != null)
                        user.Gender = (int)gender;
                    if (birthday != null)
                        user.Birthday = birthday;
                    user.ModifiedBy = admin.Login;
                    user.ModifiedOn = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Ok("User has been updated");
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }
            else
            {
                try
                {
                    var user = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                    if (name != null)
                        user.Name = name;
                    if (gender != null)
                        user.Gender = (int)gender;
                    if (birthday != null)
                        user.Birthday = birthday;
                    user.ModifiedBy = yourLogin;
                    user.ModifiedOn = DateTime.Now;
                    await _context.SaveChangesAsync();
                    return Ok("You have updated your profile");
                }
                catch (ArgumentException ex)
                {
                    return BadRequest(ex.Message);
                }
            }
        }

//        Изменение пароля(Пароль может менять либо Администратор, либо лично пользователь, если
//он активен (отсутствует RevokedOn))
        [HttpPut("Update password")]
        public async Task<IActionResult> UpdateOnePassword(string newPassword, string yourLogin = "Admin", string yourPassword = "Admin")
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return NotFound("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin && admin.RevokedOn != null)
                    return Unauthorized("You do not have access to this method. Your account is revoked");

                admin.Password = newPassword;

                await _context.SaveChangesAsync();
                return Ok("You have updated your password");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

//        Изменение логина(Логин может менять либо Администратор, либо лично пользователь, если
//он активен (отсутствует RevokedOn), логин должен оставаться уникальным)
        [HttpPut("Update login")]
        public async Task<IActionResult> UpdateOneLogin(string newLogin, string yourLogin = "Admin", string yourPassword = "Admin")
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return NotFound("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin && admin.RevokedOn != null)
                    return Unauthorized("You do not have access to this method. Your account is revoked");

                admin.Login = newLogin;

                await _context.SaveChangesAsync();
                return Ok("You have updated your login");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

//        Запрос списка всех активных(отсутствует RevokedOn) пользователей, список отсортирован по
//CreatedOn(Доступно Админам)
        [HttpGet("Get active users")]
        [SwaggerOperation(Summary = "Get all active users", Description = "Retrieves a list of all active users.")]

        public async Task<ActionResult<IEnumerable<User>>> GetAllActiveUsers(string yourLogin = "Admin", string yourPassword = "Admin")
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return NotFound("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin)
                    return Unauthorized("You do not have access to this method. You are not an admin");
          
            if (_context.UserItems == null)
            {
                return NotFound();
            }
            var users = await _context.UserItems.Where(u => u.RevokedOn == null).OrderBy(u => u.CreatedOn).ToListAsync();
            return users;
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

//        Запрос пользователя по логину, в списке долны быть имя, пол и дата рождения статус активный
//или нет(Доступно Админам)
        [HttpGet("Get user by login")]
        //[Authorize]
        public async Task<ActionResult<object>> GetUser(string userLogin, string yourLogin = "Admin", string yourPassword = "Admin")
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return NotFound("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin)
                    return Unauthorized("You do not have access to this method. You are not an admin");

                if (_context.UserItems == null)             
                    return NotFound();
                
            var user = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == userLogin);

            if (user == null)          
                return NotFound("User not found");
            

            var userResponse = new
            {
                user.Name,
                user.Gender,
                user.Birthday,
                RevokedStatus = user.RevokedOn != null ? "Status: Revoked" : "Status: Active"
            };

            return userResponse;
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //        Запрос пользователя по логину и паролю(Доступно только самому пользователю, если он
        //активен (отсутствует RevokedOn))
        [HttpGet("Get self information")]
        public async Task<ActionResult<object>> GetSelfInformation(string yourLogin = "Admin", string yourPassword = "Admin")
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return NotFound("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (admin.RevokedOn!=null)
                    return Unauthorized("You do not have access to this method. You have been revoked");

                if (_context.UserItems == null)
                    return NotFound();

                return admin;
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //Запрос всех пользователей старше определённого возраста(Доступно Админам)
        [HttpGet("Get users above age")]
        //[Authorize]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsersAboveAge(int age, string yourLogin = "Admin", string yourPassword = "Admin")
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return NotFound("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin)
                    return Unauthorized("You do not have access to this method. You are not an admin");

                if (_context.UserItems == null)
                    return NotFound();


            var users = await _context.UserItems.Where(u => (DateTime.Now - u.Birthday).Value.Days*365>age).ToListAsync();
            return users;
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

//        Удаление пользователя по логину мягкое(При мягком удалении должна
//происходить простановка RevokedOn и RevokedBy) (Доступно Админам)
        [HttpPut("Soft delete of user")]
        //[Authorize]
        public async Task<IActionResult> DeleteUserSoft(string userLogin, string yourLogin = "Admin", string yourPassword = "Admin")
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return NotFound("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin)
                    return Unauthorized("You do not have access to this method. You are not an admin");

                if (_context.UserItems == null)
                    return NotFound();
                var user = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == userLogin);
                if (user == null)
                    return BadRequest("User to be changed not found");
               
                user.ModifiedBy = yourLogin;
                user.ModifiedOn = DateTime.Now;
                user.RevokedOn = DateTime.Now;
                user.RevokedBy = yourLogin;
                await _context.SaveChangesAsync();
                return Ok("User has been revoked");

            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //        Удаление пользователя по логину полное (Доступно Админам)
        [HttpDelete("Hard delete of user")]
        //[Authorize]
        public async Task<IActionResult> DeleteUserHard(string userLogin, string yourLogin = "Admin", string yourPassword = "Admin")
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return NotFound("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin)
                    return Unauthorized("You do not have access to this method. You are not an admin");

                if (_context.UserItems == null)
                    return NotFound();
                var user = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == userLogin);
                if (user == null)
                    return BadRequest("User to be deleted not found");

                _context.UserItems.Remove(user);
                await _context.SaveChangesAsync();
                return Ok("User has been deleted");
            }
                 catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        //[Authorize]
        //Восстановление пользователя - Очистка полей(RevokedOn, RevokedBy) (Доступно Админам)
        [HttpPut("Restore user by login")]
        public async Task<IActionResult> RestoreUser(string userLogin, string yourLogin = "Admin", string yourPassword = "Admin")
        {
            try
            {


                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return NotFound("Your login is not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin)
                    return Unauthorized("You do not have access to this method. You are not an admin");

                var user = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == userLogin);
                if (user == null)
                    return NotFound("User not found");
                user.RevokedOn = null;
                user.RevokedBy = null;
                user.ModifiedOn = DateTime.Now;
                user.ModifiedBy = yourLogin;
                await _context.SaveChangesAsync();

                return Ok("User has been restored");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }
       
        private bool UserExists(Guid id)
        {
            return (_context.UserItems?.Any(e => e.Guid == id)).GetValueOrDefault();
        }
    }
}
