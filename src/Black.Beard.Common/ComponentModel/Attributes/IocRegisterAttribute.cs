using System;

namespace Bb.ComponentModel.Attributes
{

    public enum IocScopeEnum
    {

        /// <summary>
        /// Specifies that a new instance of the service will be created every time it is requested.
        /// </summary>
        Transient = 0,

        /// <summary>
        /// Specifies that a single instance of the service will be created.
        /// </summary>
        Singleton = 1,

        /// <summary>
        /// The scopedSpecifies that a new instance of the service will be created for each scope.
        /// </summary>
        /// <remarks>In ASP.NET Core applications a scope is created around each server request.</remarks>
        Scoped = 2,

    }

    [System.AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class IocRegisterAttribute : Attribute
    {

        // This is a positional argument
        public IocRegisterAttribute(Type exposedType, IocScopeEnum scope = IocScopeEnum.Transient)
        {
            ExposedType = exposedType;
            Scope = scope;
        }

        public Type ExposedType { get; }

        public IocScopeEnum Scope { get; }

    }

}
