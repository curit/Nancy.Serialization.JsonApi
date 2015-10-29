namespace Nancy.Serialization.JsonApi
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Humanizer;
    using Nancy.IO;
    using Newtonsoft.Json;

    public class JsonApiSerializer : ISerializer
    {
        private readonly JsonSerializer serializer;

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonApiSerializer"/> class.
        /// </summary>
        public JsonApiSerializer()
        {
            this.serializer = JsonSerializer.CreateDefault();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="JsonApiSerializer"/> class,
        /// with the provided <paramref name="serializer"/>.
        /// </summary>
        /// <param name="serializer">Json converters used when serializing.</param>
        public JsonApiSerializer(JsonSerializer serializer)
        {
            this.serializer = serializer;
        }

        /// <summary>
        /// Whether the serializer can serialize the content type
        /// </summary>
        /// <param name="contentType">Content type to serialise</param>
        /// <returns>True if supported, false otherwise</returns>
        public bool CanSerialize(string contentType)
        {
            return Helpers.IsJsonType(contentType);
        }

        /// <summary>
        /// Gets the list of extensions that the serializer can handle.
        /// </summary>
        /// <value>An <see cref="IEnumerable{T}"/> of extensions if any are available, otherwise an empty enumerable.</value>
        public IEnumerable<string> Extensions
        {
            get { yield return "jsonapi"; }
        }

        /// <summary>
        /// Serialize the given model with the given contentType
        /// </summary>
        /// <param name="contentType">Content type to serialize into</param>
        /// <param name="model">Model to serialize</param>
        /// <param name="outputStream">Output stream to serialize to</param>
        /// <returns>Serialised object</returns>
        public void Serialize<TModel>(string contentType, TModel model, Stream outputStream)
        {
            using (var writer = new JsonTextWriter(new StreamWriter(new UnclosableStreamWrapper(outputStream))))
            {
                var jsonapidict = new Dictionary<string, object>();

                var modelName = typeof (TModel).Name;

                var properties = typeof (TModel).GetProperties();

                var idProp = model.GetIdPropertyByConvention();

                Dictionary<string, object> attributes =
                    properties
                        .Where(p => p != idProp)
                        .Where(p => (p.PropertyType.IsValueType || 
                                     (p.PropertyType.GetIdPropertyByConvention() == null && p.PropertyType.GetInterfaces().All(i => i.Name != "IEnumerable`1")) ||
                                     (p.PropertyType.GetInterfaces().Any(i => i.Name == "IEnumerable`1") &&
                                      p.PropertyType.GetInterfaces().First(i => i.Name == "IEnumerable`1").GetGenericArguments().First().GetIdPropertyByConvention() == null))
                         )
                        .ToDictionary(p => p.Name.ToKebabCase(), p => p.GetValue(model, null));

                var relationships =
                    properties
                        .Where(p => !attributes.ContainsKey(p.Name.ToKebabCase()))
                        .Where(p => p != idProp)
                        .ToDictionary(p => p.Name.ToKebabCase(), p =>
                        {
                            object data;
                            if (p.PropertyType.GetInterfaces().Any(i => i.Name == "IEnumerable`1"))
                            {
                                var type = p.PropertyType.GetInterfaces().First(i => i.Name == "IEnumerable`1").GetGenericArguments().First();
                                var idProperty = type.GetIdPropertyByConvention();
                                IEnumerable enumerable = (IEnumerable) p.GetValue(model, null);
                                data = from object item in enumerable
                                    select new Dictionary<string, string>
                                    {
                                        {"type", type.Name.Pluralize().ToKebabCase() },
                                        {"id", idProperty.GetValue(item, null).ToString()}
                                    };
                            }
                            else
                            {
                                var type = p.PropertyType;
                                var idProperty = p.PropertyType.GetIdPropertyByConvention();
                                var item = p.GetValue(model, null);
                                data = new Dictionary<string, string>
                                {
                                    { "type", type.Name.Pluralize().ToKebabCase() },
                                    { "id", idProperty.GetValue(item, null).ToString() }
                                };
                            }

                            return new Dictionary<string, object>
                            {
                                {
                                    "data", data
                                }
                            };
                        });


                        
                jsonapidict["type"] = modelName.Pluralize().ToKebabCase();
                jsonapidict["id"] = idProp.GetValue(model, null).ToString();
                jsonapidict["attributes"] = attributes;
                jsonapidict["relationships"] = relationships;

                this.serializer.Serialize(writer, new { data = jsonapidict });
            }
        }
    }
}
