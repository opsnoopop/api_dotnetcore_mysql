namespace Api.Models;

public sealed class User
{
    public long user_id { get; set; }     // BIGINT UNSIGNED -> long (ระวังกรณี overflow ถ้าเกิน long)
    public string username { get; set; } = default!;
    public string email { get; set; } = default!;
}
