using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Models;

public class Api
{
    public int Id { get; set; }
    [Column(TypeName = "json")]
    public string? RawJson { get; set; } = default!;
    public string Etag { get; set; } = default!;
    public DateTime CreatedAt { get; set; } 
    public DateTime? UpdatedAt { get; set; }
}
