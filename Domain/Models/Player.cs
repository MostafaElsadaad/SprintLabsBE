using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain.Models;
public class Player
{
    public long Id { get; set; }
    public string GoogleId { get; set; } = default!;
    public string Email { get; set; } = default!;
    public string Name { get; set; } = default!;
    public string? AvatarUrl { get; set; }
    public int? Age { get; set; }
    public int? Grade { get; set; }
    public string? SchoolName { get; set; }

    // Progression
    public int Gold { get; set; } = 0;
    public int Experience { get; set; } = 0;
    public int Level { get; set; } = 1;

    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}