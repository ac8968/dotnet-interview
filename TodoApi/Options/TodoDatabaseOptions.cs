namespace TodoApi.Options;

public class TodoDatabaseOptions
{
    public const string SectionName = "TodoDatabase";

    public string ConnectionString { get; set; } = string.Empty;
}
