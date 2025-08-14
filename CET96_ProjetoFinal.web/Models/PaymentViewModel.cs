using System.ComponentModel.DataAnnotations;

namespace CET96_ProjetoFinal.web.Models
{
    // This model holds all the data needed for the payment simulation page.
    public class PaymentViewModel
    {
        // --- Company Details (to be displayed) ---
        [Required]
        public string CompanyName { get; set; }
        public string? CompanyDescription { get; set; }

        [Required]
        [Display(Name = "Tax ID")]
        public string CompanyTaxId { get; set; }

        // --- Credit Card Form Fields ---
        [Required(ErrorMessage = "Card holder name is required.")]
        [Display(Name = "Card Holder Name")]
        public string CardHolderName { get; set; }

        [Required(ErrorMessage = "Card number is required.")]
        [CreditCard] // This provides basic format validation
        [Display(Name = "Card Number")]
        public string CardNumber { get; set; }

        [Required(ErrorMessage = "Expiration date is required.")]
        [Display(Name = "Expiration Date (MM/YY)")]
        public string ExpirationDate { get; set; }

        [Required(ErrorMessage = "CVV is required.")]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "Invalid CVV.")]
        [Display(Name = "CVV")]
        public string Cvv { get; set; }
    }
}