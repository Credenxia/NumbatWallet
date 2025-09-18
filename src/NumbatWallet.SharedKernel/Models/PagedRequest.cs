namespace NumbatWallet.SharedKernel.Models;

public class PagedRequest
{
    private const int MaxPageSize = 100;
    private int _pageSize = 10;
    private int _pageNumber = 1;

    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > MaxPageSize ? MaxPageSize : (value < 1 ? 10 : value);
    }

    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public string? SearchTerm { get; set; }
    public Dictionary<string, object> Filters { get; set; } = new();

    public int Skip => (PageNumber - 1) * PageSize;
    public int Take => PageSize;
}