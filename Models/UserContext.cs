using Microsoft.EntityFrameworkCore;

namespace Aton.Models
{
    public class UserContext : DbContext
    {
        public UserContext(DbContextOptions<UserContext> options)
            : base(options)
        {
        }

        public DbSet<User> UserItems { get; set; } = null!;
        public static void SeedAdminUser(UserContext context)
        {
            // Check if the admin user already exists in the database
            var adminUser = context.UserItems.FirstOrDefault(u => u.Login == "admin");

            if (adminUser == null)
            {
                // Create a new admin user
                var admin = new User
                {
                    Login = "admin",
                    Password = "admin",
                    Birthday = DateTime.Now,
                    Name = "Administrator"
                };

                context.UserItems.Add(admin);
                context.SaveChanges();
            }
        }

    }
}
