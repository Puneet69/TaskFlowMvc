using System.ComponentModel.DataAnnotations;

namespace TaskFlowMvc.ViewModels;

public class TeamCreateInputModel
{
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [StringLength(500)]
    public string Description { get; set; } = string.Empty;
}
