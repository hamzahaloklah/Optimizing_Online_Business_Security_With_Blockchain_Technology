namespace SportsStore.Infrastructure
{
    public static class Urlextensions
    {
        public static string PathAndQuery(this  HttpRequest request) =>
            request.QueryString.HasValue ?
            $"{request.Path}{request.QueryString}" :
            request.Path.ToString();
    }
}
