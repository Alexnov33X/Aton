using Aton.Controllers;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json.Linq;
using System.Security.Claims;

namespace Aton.Models
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options)
            : base(options)
        {
        }

        public DbSet<User> UserItems { get; set; } = null!;
        public static async Task SeedAdminUser(UserContext context, IHttpContextAccessor httpContextAccessor)
        {
            // Check if the admin user already exists in the database
            var adminUser = context.UserItems.FirstOrDefault(u => u.Login == "admin");

            if (adminUser == null)
            {
                // Create a new admin user
                adminUser = new User
                {
                    Login = "admin",
                    Password = "admin",
                    Birthday = DateTime.Now,
                    Name = "Administrator",
                    CreatedBy = "admin",
                    CreatedOn = DateTime.Now,
                    Admin = true
                };
                var token = UsersController.GenerateNewToken(adminUser);
                adminUser.JwtToken = token;
                context.UserItems.Add(adminUser);
                context.SaveChanges();
            }
            var httpContext = httpContextAccessor.HttpContext;
            //if (httpContext != null)
            //{
                // Authenticate the host as the admin user
                var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, adminUser.Guid.ToString()),
        new Claim(ClaimTypes.Name, adminUser.Login),
        new Claim(ClaimTypes.Role, "Admin")

    };
    

                var identity = new ClaimsIdentity(claims, JwtBearerDefaults.AuthenticationScheme);
                httpContext.User = new ClaimsPrincipal(identity);
            //}
        }

    }
}
