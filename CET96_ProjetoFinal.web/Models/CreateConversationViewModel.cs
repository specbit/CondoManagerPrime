using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    public class CreateConversationViewModel
    {
        [Required]
        [MaxLength(200)]
        public string Subject { get; set; }

        [Required]
        [Display(Name = "Your Message")]
        public string Message { get; set; }

        // This will hold the ID of the user selected from the dropdown.
        [Required(ErrorMessage = "You must select a recipient.")]
        [Display(Name = "To")]
        public string RecipientId { get; set; }

        // Needed so the POST can know which condo to scope staff/owners to
        public int? CondominiumId { get; set; }

        // This will populate the <select> dropdown list in the view (Server only).
        [ValidateNever] // Prevents model validation on this property
        public IEnumerable<SelectListItem> Recipients { get; set; } = Enumerable.Empty<SelectListItem>();
    }
}