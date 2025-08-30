using CET96_ProjetoFinal.web.Entities;

namespace CET96_ProjetoFinal.web.Models
{
    public class LinkManagerToCondominiumViewModel
    {
        public string UserId { get; set; }
        public string FullName { get; set; }
        public string? AssociatedCOndominiumName { get; set; }
        public List<Condominium> CondominiumsList { get; set; }
    }
}
