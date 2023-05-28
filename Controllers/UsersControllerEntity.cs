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
       
       
        static string GenerateNewToken(User user)
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
        // GET: api/UsersControllerEntity
        [HttpGet("Get active users")]
        [SwaggerOperation(Summary = "Get all active users", Description = "Retrieves a list of all active users.")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<User>>> GetAllActiveUsers()
        {
            if (_context.UserItems == null)
            {
                return NotFound();
            }
            var users = await _context.UserItems.Where(u => u.RevokedOn == null).OrderBy(u => u.CreatedOn).ToListAsync();
            return users;
        }
        [HttpGet("Get users above age")]
        [Authorize]
        public async Task<ActionResult<IEnumerable<User>>> GetAllUsersAboveAge(int age)
        {
            if (_context.UserItems == null)
            {
                return NotFound();
            }
            var users = await _context.UserItems.Where(u => (DateTime.Now - u.Birthday).Value.Days*365>age).ToListAsync();
            return users;
        }



        // GET: api/UsersControllerEntity/5
        [HttpGet("Get self information")]
        [Authorize]
        public async Task<ActionResult<object>> GetUserSelf(string login, string pass)
        {
          if (_context.UserItems == null)
          {
              return NotFound();
          }
            var user = await _context.UserItems.FirstOrDefaultAsync(u => u.Login == login);

            if (user == null || user.RevokedOn!=null)
            {
                return NotFound();
            }
            var userResponse = new
            {
                user.Name,
                user.Gender,
                user.Birthday,
                user.RevokedOn
            };

            return userResponse;
        }

        [HttpGet("Get user by login")]
        [Authorize]
        public async Task<ActionResult<object>> GetUser(string login)
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

            var userResponse = new
            {
                user.Name,
                user.Gender,
                user.Birthday,
                user.RevokedOn
            };

            return userResponse;
        }

        // PUT: api/UsersControllerEntity/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("Get user by id")]
        [Authorize]
        public async Task<IActionResult> PutUser(Guid id, User user)
        {
            if (id != user.Guid)
            {
                return BadRequest();
            }

            _context.Entry(user).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }
        [Authorize]
        [HttpPost("Restore user by login")]
        public async Task<IActionResult> RestoreUser(string login )
        {
            if (_context.UserItems == null)
            {
                return NotFound();
            }
            var user = await _context.UserItems.FindAsync(login);
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

        // POST: api/UsersControllerEntity 1) Создание пользователя по логину, паролю, имени, полу и дате рождения + указание будет ли
        //пользователь админом(Доступно Админам)
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost("Create user")]
        [Authorize]
        public async Task<ActionResult<User>> CreateUser(string login, string pass,string name, int gender, DateTime birthday, bool isAdmin)
        {
          if (_context.UserItems == null)
          {
              return Problem("Entity set 'UserContext.UserItems'  is null.");
          }
            //user.JwtToken = GenerateNewToken(user);
            //_context.UserItems.Add(user);
            System.Console.WriteLine(login);
            System.Console.WriteLine(birthday);
            await _context.SaveChangesAsync();

            return CreatedAtAction("Created user:", new { login, pass});
        }

        [HttpPut("Update profile")]
        public async Task<IActionResult> UpdateOneProfile(Guid id, string name, int gender, DateTime birthday)
        {
            try
            {
                var user = await _context.UserItems.FindAsync(id);

            if (user == null)
            {
                return NotFound();
            }

            user.Name = name;
            user.Gender = gender;
            user.Birthday = birthday;

          
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }


        [HttpPut("Update password")]
        public async Task<IActionResult> UpdateOnePassword(Guid id, string pass)
        {
            try
            {
                var user = await _context.UserItems.FindAsync(id);

                if (user == null)
                {
                    return NotFound();
                }

                user.Password = pass;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPut("Update login")]
        public async Task<IActionResult> UpdateOneLogin(Guid id, string login)
        {
            try
            {
                var user = await _context.UserItems.FindAsync(id);

                if (user == null)
                {
                    return NotFound();
                }

                user.Login = login;

                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!UserExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

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
            System.Console.WriteLine(HttpContext.User.Claims);
            await _context.SaveChangesAsync();

            return CreatedAtAction("Created user:", new { user.Login, user.Password, user.JwtToken });
        }

        // DELETE: api/UsersControllerEntity/5
        [HttpDelete("Soft delete of user")]
        [Authorize]
        public async Task<IActionResult> DeleteUserSoft(string login)
        {
            if (_context.UserItems == null)
            {
                return NotFound();
            }
            var user = await _context.UserItems.FindAsync(login);
            if (user == null)
            {
                return NotFound();
            }

            user.RevokedOn = DateTime.Now;
            ///!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("Hard delete of user")]
        [Authorize]
        public async Task<IActionResult> DeleteUserHard(string login)
        {
            if (_context.UserItems == null)
            {
                return NotFound();
            }
            var user = await _context.UserItems.FindAsync(login);
            if (user == null)
            {
                return NotFound();
            }

            _context.UserItems.Remove(user);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool UserExists(Guid id)
        {
            return (_context.UserItems?.Any(e => e.Guid == id)).GetValueOrDefault();
        }
    }
}
