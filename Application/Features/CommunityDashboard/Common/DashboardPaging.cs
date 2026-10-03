using Shared.Responses;

namespace Application.Features.CommunityDashboard.Common;

public static class DashboardPaging
{
    public static void Validate(int pageNumber, int pageSize)
    {
        if (pageNumber < 1 || pageSize < 1 || pageSize > 100 ||
            ((long)pageNumber - 1) * pageSize > int.MaxValue) throw DashboardAuthorization.Invalid();
    }

    public static PagedResponse<T> Page<T>(IEnumerable<T> items, int pageNumber, int pageSize)
    {
        Validate(pageNumber, pageSize);
        var list = items.ToList();
        return new PagedResponse<T>(list.Skip((pageNumber - 1) * pageSize).Take(pageSize), pageNumber, pageSize, list.Count);
    }

    public static bool Descending(string? sort, params string[] fields)
    {
        var parts = (sort ?? "name:asc").Split(':');
        if (parts.Length != 2 || !fields.Contains(parts[0]) || (parts[1] != "asc" && parts[1] != "desc"))
            throw DashboardAuthorization.Invalid();
        return parts[1] == "desc";
    }
}
