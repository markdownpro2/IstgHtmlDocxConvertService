using IstgHtmlDocxConvertService.Logging;
using istgOfficeAutomationBrl.RegManager;
using System.Data.SqlClient;


public class TokenValidationService
{
    private readonly SystemEventLogger _eventLogger;
    private readonly string _connectionString;

    public TokenValidationService(IConfiguration configuration, SystemEventLogger eventLogger)
    {
        var registryName = configuration.GetValue<string>("RegistryName");
        _connectionString = new ConnectionStringManager(registryName).GetConnection(IstgDataBase.IstgRef);
        _eventLogger = eventLogger;
    }

    public bool IsTokenValid(string token)
    {
        try
        {
            var prsId = DecryptToken(token);

            using var connection = new SqlConnection(_connectionString);
            connection.Open();
            
            using var command = new SqlCommand("SELECT COUNT(*) FROM ..[dbo].[tbrPersonnel] WHERE prsId = @prsId", connection);
            command.Parameters.AddWithValue("@prsId", prsId);

            var count = (int)command.ExecuteScalar();

            return count > 0;
        }
        catch (Exception ex)
        {
            _eventLogger.Error($"Something went wrong in Token validation: {ex.Message}");
            return false;
        }
    }

    // Placeholder for real decryption logic
    public string DecryptToken(string encryptedToken)
    {
        return encryptedToken;
    }
}
