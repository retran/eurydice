namespace Eurydice.Core
{
    /// <summary>
    ///     File size estimator.
    /// </summary>
    public interface IFileSizeEstimator
    {
        /// <summary>
        ///     Estime file size.
        /// </summary>
        long Estimate(string path);
    }
}