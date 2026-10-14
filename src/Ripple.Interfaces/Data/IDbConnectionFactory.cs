using System.Data;

namespace Ripple.Data;

public interface IDbConnectionFactory
{
    /// <summary>
    /// Returns an already-open connection. Callers own it and must dispose it; do not call Open() on it.
    /// </summary>
    IDbConnection CreateConnection();
}
