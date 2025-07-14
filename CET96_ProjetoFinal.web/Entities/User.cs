using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Entities
{
    public class User : IdentityUser
    {
        [Required]
        [StringLength(100, ErrorMessage = "The name must be at most 100 characters long.")]
        [Display(Name = "First Name")]
        public string FirstName { get; set; } = null!;

        [Required]
        [StringLength(100, ErrorMessage = "The name must be at most 100 characters long.")]
        [Display(Name = "Last Name")]
        public string LastName { get; set; } = null!;

        public string? ProfilePicturePath { get; set; } // For optional user profile photos
    }
}
