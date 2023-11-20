namespace PiBox.Extensions.Abstractions
{
    public record Page
    {
        private int _pageNumber;

        public Page()
        {
        }

        public Page(int? size, int? current, int totalElements, int totalPages)
        {
            TotalElements = totalElements;
            TotalPages = totalPages;
            Current = current ?? 0;
            Size = size ?? int.MaxValue;
        }

        [Obsolete("Use Current instead of number. Number is deprecated.")]
        public int? Number
        {
            get => _pageNumber;
            set => _pageNumber = value ?? 0;
        }

        public int? Current
        {
            get => _pageNumber;
            set => _pageNumber = value ?? 0;
        }

        public int? Size { get; set; }
        public int TotalElements { get; set; }
        public int TotalPages { get; set; }
    }
}
