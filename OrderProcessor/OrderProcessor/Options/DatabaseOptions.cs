namespace RefactoringExercise.Options;

public enum DatabaseProvider
{
    SqlServer,
    Sqlite
}

public class DatabaseOptions
{
    public const string SectionName = "Database";

    public DatabaseProvider Provider { get; set; } = DatabaseProvider.SqlServer;
}
