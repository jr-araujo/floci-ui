namespace FlociDashboard.Services;

public class RegionService(IHttpContextAccessor httpContextAccessor, IConfiguration config)
{
    private const string SessionKey = "SelectedRegion";

    public string CurrentRegion
    {
        get
        {
            var session = httpContextAccessor.HttpContext?.Session;
            if (session != null)
            {
                var val = session.GetString(SessionKey);
                if (!string.IsNullOrWhiteSpace(val)) return val;
            }
            return config["Floci:Region"] ?? "us-east-1";
        }
        set
        {
            httpContextAccessor.HttpContext?.Session.SetString(SessionKey, value);
        }
    }

    public List<string> AvailableRegions =>
        config.GetSection("Floci:AvailableRegions").Get<List<string>>() ?? ["us-east-1"];

    public string AccountId => "000000000000";

    public string BuildArn(string service, string resourceType, string resourceId)
        => $"arn:aws:{service}:{CurrentRegion}:{AccountId}:{resourceType}/{resourceId}";
}
