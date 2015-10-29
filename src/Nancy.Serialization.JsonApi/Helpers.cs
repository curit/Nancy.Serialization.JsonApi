namespace Nancy.Serialization.JsonApi
{
    using System;
    using System.Linq;
    using System.Reflection;
    using Humanizer;

    internal static class Helpers
    {
        /// <summary>
        /// Attempts to detect if the content type is JSON.
        /// Supports:
        ///   application/vnd.api+json
        /// Matches are case insentitive to try and be as "accepting" as possible.
        /// </summary>
        /// <param name="contentType">Request content type</param>
        /// <returns>True if content type is JSON, false otherwise</returns>
        public static bool IsJsonType(string contentType)
        {
            if (string.IsNullOrEmpty(contentType))
            {
                return false;
            }

            var contentMimeType = contentType.Split(';')[0];

            return contentMimeType.Equals("application/vnd.api+json", StringComparison.InvariantCultureIgnoreCase);
        }

        public static string ToKebabCase(this string str)
        {
            return str.Humanize(LetterCasing.LowerCase).Replace(' ', '-');
        }

        public static PropertyInfo GetIdPropertyByConvention<TModel>(this TModel model)
        {
            return GetIdPropertyByConvention((object) model);
        }

        public static PropertyInfo GetIdPropertyByConvention(this object model)
        {
            var type = model.GetType();
            return GetIdPropertyByConvention(type);

        }

        public static PropertyInfo GetIdPropertyByConvention(this Type type)
        {
            var modelName = type.FullName.Split('.').Last();
            var properties = type.GetProperties();
            return properties.FirstOrDefault(p => p.Name == "Id" || p.Name == modelName + "Id");
        }
    }
}
