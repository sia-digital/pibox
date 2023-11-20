namespace PiBox.Extensions.Abstractions
{
    public record PagingRequest(int? Size = 25, int? Page = 0, string Filter = null, string Sort = null);
}
