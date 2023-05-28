using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace Aton.Models
{
    [PrimaryKey(nameof(Guid))]
    public class User
    {
        static bool IsLatinAndNumbers(string input)
        {
            string pattern = "^[a-zA-Z0-9]+$";
            Regex regex = new Regex(pattern);
            return regex.IsMatch(input);
        }
        public Guid Guid { get; set; }
        string login, password, name;
        int gender;

        [Required]
        public string Login { get { return login; } set
            {
                if (IsLatinAndNumbers(value))
                    login = value;
                else
                    throw new Exception();
            }
        }
       
        public string Password
        {
            get { return password; } set
            {
                if (IsLatinAndNumbers(value))
                    password = value;
                else
                    throw new Exception();
            }
        }
        public string Name
        {
            get { return name; }
            set
            {
                string pattern = @"^[a-zA-Zа-яА-Я]+$";

                // Check if the input matches the pattern
                if (Regex.IsMatch(value, pattern))
                    name = value;
                else
                    throw new Exception();
            }
        }
        public int Gender { get { return gender; } set
            {
                if (value >= 0 && value < 3)
                    gender = value;
                else
                    throw new Exception();
            }
        }
        public bool Admin { get; set; }
        public DateTime? Birthday { get; set; }
        public DateTime CreatedOn { get; set; }
        public string CreatedBy { get; set; } 
        public DateTime? ModifiedOn { get; set; }
        public string? ModifiedBy { get; set; }
        public DateTime? RevokedOn { get; set; } 
        public string? RevokedBy { get; set; }
        public string JwtToken { get; set; }
    }
}
