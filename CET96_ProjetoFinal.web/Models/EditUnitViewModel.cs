using System.ComponentModel.DataAnnotations;

public class EditUnitViewModel
{
    public int Id { get; set; }
    public int CondominiumId { get; set; }

    [Required]
    [MaxLength(50)]
    [Display(Name = "Unit Number or Name")]
    public string UnitNumber { get; set; }
}