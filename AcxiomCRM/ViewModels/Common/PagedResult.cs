namespace AcxiomCRM.ViewModels.Common
{
    public class PagedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / (PageSize > 0 ? PageSize : 10));
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }
}
