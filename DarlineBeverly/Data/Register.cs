using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace DarlineBeverly.Data
{
    public class Register : BaseModel
    {

        [Required]
        [Compare("Password", ErrorMessage = "Password and Confirm Password do not match.")]
        public string ConfirmPassword { get; set; }
    }

    public class BaseModel
    {
        [EmailAddress]
        [Required]
        public string Email { get; set; }

        public string Password { get; set; }


    }
    public class Login : BaseModel
    {
        public bool RememberMe { get; set; } 
    }   
}