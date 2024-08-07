public class BaseResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
}

public class DataResponse<T> : BaseResponse
{
    public T? Data { get; set; }
}