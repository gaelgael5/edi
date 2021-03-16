namespace Bb.Configurations
{

    /// <summary>
    /// Service to configure
    /// </summary>
    public interface IConfiguring
    {

        /// <summary>
        /// Configures the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        void Configure(IConfigurator configuration);

    }

    /// <summary>
    /// Service to configure
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConfiguring<T> : IConfiguring
        where T : IConfigurator<T>
    {

    }

}
