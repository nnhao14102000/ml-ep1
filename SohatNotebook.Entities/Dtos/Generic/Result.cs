using SohatNotebook.Entities.Dtos.Errors;
using System;

namespace SohatNotebook.Entities.Dtos.Generic
{
    public class Result<T>
    {
        public T Content { get; set; }
        public Error Error { get; set; }
        public bool IsSuccess => Error == null;
        public DateTime ResponseTime { get; set; } = DateTime.Now;
    }
}
