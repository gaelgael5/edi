namespace Bb.Configurations
{

    /// <summary>
    /// Class used to configurea service
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConfigurator
    {

        bool AcceptConfigure(object objectToConfigure);

        void Configure(object objectToConfigure);

    }


    /// <summary>
    /// Class use to configure a service
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IConfigurator<T> : IConfigurator
    {

        void ConfigureService(T objectToConfigure);

    }

}
