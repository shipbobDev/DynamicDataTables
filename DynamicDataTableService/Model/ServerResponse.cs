namespace DynamicDataTableService
{
    public class ServerResponse<T>
    {
        public string Error { get; set; }
        public bool Success { get; set; }
        public T PayLoad { get; set; }
    }
}
