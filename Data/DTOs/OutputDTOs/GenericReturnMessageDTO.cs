namespace LoginService.Data.DTOs.OutputDTOs
{
    public class GenericReturnMessageDTO
    {
        public int StatusCode { get; set; }
        public object Message { get; set; }
    }
}