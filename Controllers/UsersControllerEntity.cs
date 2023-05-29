﻿using System;
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
    public class UsersControllerEntity : ControllerBase
    {
        private readonly UserContext _context;

        public UsersControllerEntity(UserContext context)
        {
            _context = context;
        }
       
        DateTime dtPlaceholder = new DateTime(2000, 1, 1);
        public static string GenerateNewToken(User user)
        {
            // Create a JWT token for the user
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes("YOUR_SECRET_KEY_GOES_HERE");
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
        //[Authorize]
        public async Task<ActionResult<User>> CreateUser(string yourLogin, string yourPassword, string login, string pass, string name, int gender, DateTime birthday, bool isAdmin)
        {
            if (_context.UserItems == null)
            {
                return Problem("Entity set 'UserContext.UserItems'  is null.");
            }
            var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

            if (admin == null)
                return BadRequest("User not found");

            if (admin.Password != yourPassword)
                return BadRequest("Incorrect password");

            if (!admin.Admin)
                return BadRequest("You do not have access to this method");
            //var userClaims = User.Claims;
            //var loginClaim = userClaims.FirstOrDefault(c => c.Type == ClaimTypes.Name).Value;
            //var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == loginClaim);
            try
            {
                User user = new User
                {
                    Login = login,
                    Password = pass,
                    Name = name,
                    Gender = gender,
                    Birthday = birthday,
                    Admin = isAdmin,
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

                var response = new
                {
                    user.Login,
                    user.Password,
                    //Token = user.JwtToken
                };

                return CreatedAtAction(nameof(CreateUser), response);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("Update profile")]
        public async Task<IActionResult> UpdateOneProfile(string yourLogin, string yourPassword, string? targetedUser, string? name = null, int? gender = null, DateTime? birthday = null)
        {

            var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

            if (admin == null)
                return BadRequest("User not found");

            if (admin.Password != yourPassword)
                return BadRequest("Incorrect password");

            if (!admin.Admin && admin.RevokedOn != null)
                return BadRequest("You do not have access to this method. Your account is revoked");

            if (admin.Admin)
            {
                try
                {
                    var user = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == targetedUser);
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

        [HttpPut("Update password")]
        public async Task<IActionResult> UpdateOnePassword(string yourLogin, string yourPassword, string pass)
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return BadRequest("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin && admin.RevokedOn != null)
                    return BadRequest("You do not have access to this method. Your account is revoked");

                admin.Password = pass;

                await _context.SaveChangesAsync();
                return Ok("You have updated your password");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpPut("Update login")]
        public async Task<IActionResult> UpdateOneLogin(string yourLogin, string yourPassword, string login)
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return BadRequest("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin && admin.RevokedOn != null)
                    return BadRequest("You do not have access to this method. Your account is revoked");

                admin.Login = login;

                await _context.SaveChangesAsync();
                return Ok("You have updated your login");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }
        }

        // GET: api/UsersControllerEntity
        [HttpGet("Get active users")]
        [SwaggerOperation(Summary = "Get all active users", Description = "Retrieves a list of all active users.")]

        public async Task<ActionResult<IEnumerable<User>>> GetAllActiveUsers(string yourLogin, string yourPassword)
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return BadRequest("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin)
                    return BadRequest("You do not have access to this method. You are not an admin");
          
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
        [HttpGet("Get user by login")]
        [Authorize]
        public async Task<ActionResult<object>> GetUser(string yourLogin, string yourPassword, string login)
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return NotFound("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin)
                    return BadRequest("You do not have access to this method. You are not an admin");

                if (_context.UserItems == null)             
                    return NotFound();
                
            var user = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == login);

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

        [HttpGet("Get users above age")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsersAboveAge(string yourLogin, string yourPassword, int age)
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return NotFound("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin)
                    return BadRequest("You do not have access to this method. You are not an admin");

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

        // DELETE: api/UsersControllerEntity/5
        [HttpDelete("Soft delete of user")]
        [Authorize]
        public async Task<IActionResult> DeleteUserSoft(string yourLogin, string yourPassword, string targetedUser)
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return NotFound("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin)
                    return BadRequest("You do not have access to this method. You are not an admin");

                if (_context.UserItems == null)
                    return NotFound();
                var user = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == targetedUser);
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

        [HttpDelete("Hard delete of user")]
        [Authorize]
        public async Task<IActionResult> DeleteUserHard(string yourLogin, string yourPassword, string targetedUser)
        {
            try
            {
                var admin = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == yourLogin);

                if (admin == null)
                    return NotFound("User not found");

                if (admin.Password != yourPassword)
                    return BadRequest("Incorrect password");

                if (!admin.Admin)
                    return BadRequest("You do not have access to this method. You are not an admin");

                if (_context.UserItems == null)
                    return NotFound();
                var user = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == targetedUser);
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

        //// GET: api/UsersControllerEntity/5
        //[HttpGet("Get self information")]

        //public async Task<ActionResult<object>> GetUserSelf(string login, string pass)
        //{
        //  if (_context.UserItems == null)
        //  {
        //      return NotFound();
        //  }
        //    var user = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == login);

        //    if (user == null || user.RevokedOn!=null)
        //    {
        //        return NotFound();
        //    }
        //    var userResponse = new
        //    {
        //        user.Name,
        //        user.Gender,
        //        user.Birthday,
        //        user.RevokedOn
        //    };

        //    return userResponse;
        //}



        //// PUT: api/UsersControllerEntity/5
        //// To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        //[HttpPut("Get user by id")]
        //[Authorize]
        //public async Task<IActionResult> PutUser(Guid id, User user)
        //{
        //    if (id != user.Guid)
        //    {
        //        return BadRequest();
        //    }

        //    _context.Entry(user).State = EntityState.Modified;

        //    try
        //    {
        //        await _context.SaveChangesAsync();
        //    }
        //    catch (DbUpdateConcurrencyException)
        //    {
        //        if (!UserExists(id))
        //        {
        //            return NotFound();
        //        }
        //        else
        //        {
        //            throw;
        //        }
        //    }

        //    return NoContent();
        //}

        [Authorize]
        [HttpPost("Restore user by login")]
        public async Task<IActionResult> RestoreUser(string login )
        {
            if (_context.UserItems == null)
            {
                return NotFound();
            }
            var user = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == login);
            if (user == null)
            {
                return NotFound();
            }

            user.RevokedOn = null;
            user.ModifiedOn = DateTime.Now;
            ///!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            await _context.SaveChangesAsync();

            return NoContent();
        }

        

       


       

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<User>> PostUser(User user)
        {
            if (_context.UserItems == null)
            {
                return Problem("Entity set 'UserContext.UserItems'  is null.");
            }
            user.JwtToken = GenerateNewToken(user);
            _context.UserItems.Add(user);
            foreach (var item in HttpContext.User.Claims)
            {
                System.Console.WriteLine(item);
            }
           
            await _context.SaveChangesAsync();

            return CreatedAtAction("Created user:", new { user.Login, user.Password, user.JwtToken });
        }

       

        private bool UserExists(Guid id)
        {
            return (_context.UserItems?.Any(e => e.Guid == id)).GetValueOrDefault();
        }
    }
}
