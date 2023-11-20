namespace PiBox.Extensions.Abstractions
{
    public record PagedList<T>
    {
        public PagedList() { }

        public PagedList(IEnumerable<T> items, int totalElements, int page, int size)
        {
            Items = items.ToList();
            var totalPages = Convert.ToInt32(Math.Ceiling((double)totalElements / size));
            Page = new Page(size, page, totalElements, totalPages);
        }

        public IList<T> Items { get; set; } = null!;
        public Page Page { get; set; } = null!;
    }
}
