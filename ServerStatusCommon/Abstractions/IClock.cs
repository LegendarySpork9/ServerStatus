// Copyright © - Unpublished - Toby Hunter
namespace ServerStatusCommon.Abstractions
{
    /// <summary>
    /// Interface for the DateTime object.
    /// </summary>
    public interface IClock
    {
        DateTime UtcNow { get; }
        DateTime DefaultDate { get; }
    }
}
