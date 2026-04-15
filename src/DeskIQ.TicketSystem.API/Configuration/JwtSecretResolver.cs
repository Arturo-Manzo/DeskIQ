namespace DeskIQ.TicketSystem.API.Configuration;

public static class JwtSecretResolver
{
    public const string SecretEnvironmentVariableName = "JWT_KEY";

    public static (string Secret, bool UsedAppSettingsFallback) Resolve(IConfiguration configuration)
    {
        // First check environment variable
        var envSecret = Environment.GetEnvironmentVariable(SecretEnvironmentVariableName);
        if (!string.IsNullOrWhiteSpace(envSecret))
        {
            return (envSecret, false);
        }

        // Fall back to appsettings
        var configSecret = configuration["Jwt:Key"];
        if (!string.IsNullOrWhiteSpace(configSecret))
        {
            return (configSecret, true);
        }

        throw new InvalidOperationException(
            $"JWT secret not found. Either set the {SecretEnvironmentVariableName} environment variable " +
            "or configure Jwt:Key in appsettings.json");
    }
}

public static class SecurityStartupValidator
{
    public static void ValidateJwtSecretSource(
        bool usedAppSettingsFallback,
        bool isProduction,
        string environmentVariableName)
    {
        // Only validate in production environment
        if (isProduction && usedAppSettingsFallback)
        {
            throw new InvalidOperationException(
                $"In production, JWT secret must be set via the {environmentVariableName} environment variable, " +
                "not in appsettings.json");
        }
    }
}
