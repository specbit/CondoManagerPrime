using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    public class EditAccountViewModel
    {
        // We include the Id to ensure we're updating the correct user,
        // but it won't be shown on the form.
        public string Id { get; set; }

        [Required]
        [Display(Name = "First Name")]
        [MaxLength(50)]
        public string FirstName { get; set; }

        [Required]
        [Display(Name = "Last Name")]
        [MaxLength(50)]
        public string LastName { get; set; }

        [MaxLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
    }
}