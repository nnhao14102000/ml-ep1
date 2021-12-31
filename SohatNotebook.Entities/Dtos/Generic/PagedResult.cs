using System.Collections.Generic;

namespace SohatNotebook.Entities.Dtos.Generic
{
    public class PagedResult<T> : Result<List<T>>
    {
        public int Page { get; set; }
        public int ResultCount { get; set; }
        public int ResultPerPage { get; set; }
    }
}
