using Bb.ComponentModel.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Bb.ComponentModel
{
    public static class TypeReferentialExtension
    {





        /// <summary>
        /// Gets the custom attribute attributes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self">The self.</param>
        /// <returns></returns>
        public static T[] GetAttributes<T>(this Type self) where T : Attribute
        {

            return TypeDescriptor.GetAttributes(self).OfType<T>().ToArray();
        }

        /// <summary>
        /// Gets the custom attribute attributes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self">The self.</param>
        /// <returns></returns>
        public static T[] GetAttributes<T>(this MethodInfo self) where T : Attribute
        {
            return TypeDescriptor.GetAttributes(self).OfType<T>().ToArray();
        }

        /// <summary>
        /// Gets the custom attribute attributes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self">The self.</param>
        /// <returns></returns>
        public static T[] GetAttributes<T>(this PropertyInfo self) where T : Attribute
        {
            return TypeDescriptor.GetAttributes(self).OfType<T>().ToArray();
        }

        /// <summary>
        /// Gets the custom attribute attributes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self">The self.</param>
        /// <returns></returns>
        public static T[] GetAttributes<T>(this FieldInfo self) where T : Attribute
        {
            return TypeDescriptor.GetAttributes(self).OfType<T>().ToArray();
        }

        /// <summary>
        /// Gets the custom attribute attributes.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="self">The self.</param>
        /// <returns></returns>
        public static T[] GetAttributes<T>(this EventInfo self) where T : Attribute
        {
            return TypeDescriptor.GetAttributes(self).OfType<T>().ToArray();
        }

        /// <summary>
        /// looking for all loaded assemblies and return types where class contains <see cref="ExposeClassAttribute" />. with context equals specified context
        /// </summary>
        /// <param name="self">The self is  <see cref="ITypeReferential" /></param>
        /// <param name="context">The context that want to search in context attribute</param>
        /// <param name="typeBase">The type base if the list of type must inherit of specific type.</param>
        /// <returns>
        /// the list of types found, the key is the display name of <see cref="ExposeClassAttribute" />
        /// </returns>
        public static KeyValuePair<string, Type>[] GetTypesWithAttributeExposeClass(this ITypeReferential self, string context, Type typeBase = null)
        {
            return self
                .GetTypesWithAttributes<Attributes.ExposeClassAttribute>(typeBase ?? typeof(object), attribute => attribute.Context == context)
                .Select(c => new KeyValuePair<string, Type>(TypeDescriptor.GetAttributes(c).OfType<ExposeClassAttribute>().FirstOrDefault().Name, c))
                .ToArray();
        }

        /// <summary>
        /// return attributes <see cref="ExposeClassAttribute" />. with context equals specified context. If context is null or empty, no filter is apply
        /// </summary>
        /// <param name="self">The self is  <see cref="ITypeReferential" /></param>
        /// <param name="context">The context that want to search in context attribute</param>
        /// <returns>
        /// List of <see cref="ExposeClassAttribute" />
        /// </returns>
        public static ExposeClassAttribute[] GetExposedAttributes(this Type self, string context = null)
        {
            var result = self.GetAttributes<ExposeClassAttribute>().ToArray();
            if (string.IsNullOrEmpty(context))
                return result;

            return result.Where(c => c.Context == context).ToArray();

        }


    }

}