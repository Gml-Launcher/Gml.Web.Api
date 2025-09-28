public class PermissionDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsSystem { get; set; }
    public string? Description { get; set; }
}
