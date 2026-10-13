using System.Data;

namespace Ripple.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
