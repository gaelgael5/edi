using System;

namespace Bb.ComponentModel.Attributes
{

    /// <summary>
    /// specify this class contains method to expose
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class ExposeModel : Attribute
    {

        public ExposeModel(string domain, string version, string key, string usage)
        {
            this.Domain = domain;
            this.Version = version;
            this.Key = key;
            this.Usage = usage;
        }

        /// <summary>
        /// Gets the domain.
        /// </summary>
        /// <value>
        /// The domain.
        /// </value>
        public string Domain { get; }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <value>
        /// The version.
        /// </value>
        public string Version { get; }

        /// <summary>
        /// Gets the model key for matching the model.
        /// </summary>
        /// <value>
        /// The message key.
        /// </value>
        public string Key { get; }

        /// <summary>
        /// Gets the message key for matching the incoming message.
        /// </summary>
        /// <value>
        /// The message key.
        /// </value>
        public string Usage { get; }

    }

}