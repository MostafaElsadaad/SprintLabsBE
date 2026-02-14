namespace Shared.Responses
{
    public class PagedResponse<TData>
    {
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
        public int TotalRecords { get; set; }
        public IEnumerable<TData> Data { get; set; }

        public PagedResponse(IEnumerable<TData> data, int pageNumber, int pageSize, int totalRecords)
        {
            Data = data;
            PageNumber = pageNumber;
            PageSize = pageSize;
            TotalRecords = totalRecords;
            TotalPages = (int)Math.Ceiling(totalRecords / (double)pageSize);

            // explaination : the total pages is the total records divided by the page size
            // if the total records is 10 and the page size is 5, the total pages will be 2
            // if the total records is 11 and the page size is 5, the total pages will be 3
            // if the total records is 12 and the page size is 5, the total pages will be 3
            // Math.Ceiling is used to round up the result to the nearest whole number
            // the cast to double is to ensure that the division result is a decimal number
            // the cast to int is to ensure that the result is a whole number
            // why pageSize is casted to double 
            // because the division result should be a decimal number
            // example : 10 / 5 = 2
            // example : 11 / 5 = 2.2
            // example : 12 / 5 = 2.4
            // so the 2.4 should be rounded up to 3
            // so the total pages should be 3
        }
    }
}
