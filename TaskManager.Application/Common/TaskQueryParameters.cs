namespace TaskManager.Application.Common;

public class TaskQueryParameters
{
    private int _pageNumber = 1;
    private int _pageSize = 10;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value < 1 ? 10 : (value > 100 ? 100 : value);
    }

    public int? Status { get; set; }
    public int? Priority { get; set; }
    public Guid? ProjectId { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public bool? IsOverdue { get; set; }
    public string? SearchTerm { get; set; }

    public string SortBy { get; set; } = "CreatedAt";
    public string SortOrder { get; set; } = "desc";

    public bool IsValidSortBy()
    {
        var allowedFields = new[] { "title", "priority", "status", "createdAt", "dueDate", "updatedAt" };
        return allowedFields.Contains(SortBy.ToLower());
    }

    public bool IsValidSortOrder()
    {
        return SortOrder.ToLower() is "asc" or "desc";
    }
}
