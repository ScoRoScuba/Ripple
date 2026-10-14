using DbUp.Engine.Output;
using Serilog;

namespace Ripple.Migrations.Logging;

/// <summary>
/// Routes DbUp's string.Format-style output through Serilog.
/// </summary>
public sealed class SerilogUpgradeLog(ILogger logger) : IUpgradeLog
{
    public void WriteInformation(string format, params object[] args) =>
        logger.Information("{DbUpMessage:l}", string.Format(format, args));

    public void WriteWarning(string format, params object[] args) =>
        logger.Warning("{DbUpMessage:l}", string.Format(format, args));

    public void WriteError(string format, params object[] args) =>
        logger.Error("{DbUpMessage:l}", string.Format(format, args));
}
