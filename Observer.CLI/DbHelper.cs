using LeaderAnalytics.AdaptiveClient.EntityFrameworkCore;

namespace LeaderAnalytics.Observer.CLI;

public class DbHelper
{
    // Nuget package LeaderAnalytics.Vyntix.Fred.StagingDb must be referenced directly
    // by this app in order for stored procs to be copied to the db when it is created.

    private IDatabaseUtilities utilities;
    private IEndPointConfiguration endPoint;
    public IEndPointConfiguration EndPoint => endPoint;

    public DbHelper(IDatabaseUtilities utilities, IEndPointConfiguration endPoint)
    {
        this.utilities = utilities;
        this.endPoint = endPoint;
    }

    public async Task<DatabaseStatus> VerifyDatabase() => await utilities.GetDatabaseStatus(endPoint);

    public async Task UpdateDatabase() => await utilities.CreateOrUpdateDatabase(endPoint);
}
