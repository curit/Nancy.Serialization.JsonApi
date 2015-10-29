namespace Nancy.Serialization.JsonApi.Tests
{
    using System;
    using System.IO;
    using System.Text;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Serialization;
    using Xunit;

    public class Fiets
    {
        public int Id { get; set; }
        public string Naam { get; set; }
    }

    public class Thing
    {
        public int Id { get; set; }
        public string SomeString { get; set; }
        public Guid SomeGuid { get; set; }
        public Uri NullValue { get; set; }
        public Bert Bert { get; set; }
        public Ernie Ernie { get; set; }
        public Fiets[] Fietsen { get; set; }
    }

    public class Bert
    {
        public int Id { get; set; }
        public string Banaan { get; set; }
    }

    public class Ernie
    {
        public string Banaan { get; set; }
        public string Apple { get; set; }
    }

    public class JsonApiSerializerFixture
    {
        [Fact]
        public void when_serializing()
        {
            // Given
            JsonConvert.DefaultSettings = GetJsonSerializerSettings;

            var data = new Thing {
                Id = 5,
                SomeString = "some string value",
                SomeGuid = new Guid("77f8195e-ac2e-4c5f-9d0a-f7663ca24435"),
                NullValue = default(Uri),
                Bert = new Bert
                    {
                        Id = 45,
                        Banaan = "fiets"
                    },
                Ernie = new Ernie
                {
                    Banaan = "peer",
                    Apple = "fiets"
                },
                Fietsen = new []
                    {
                        new Fiets { Id = 6, Naam = "batavia" },
                        new Fiets { Id = 7, Naam = "gazelle" },
                        new Fiets { Id = 8, Naam = "canondale" }
                    }
            };

            string expected = "{\"data\":{\"type\":\"things\",\"id\":\"5\",\"attributes\":{\"some-string\":\"some string value\",\"some-guid\":\"77f8195e-ac2e-4c5f-9d0a-f7663ca24435\",\"null-value\":null,\"ernie\":{\"banaan\":\"peer\",\"apple\":\"fiets\"}},\"relationships\":{\"bert\":{\"data\":{\"type\":\"berts\",\"id\":\"45\"}},\"fietsen\":{\"data\":[{\"type\":\"fiets\",\"id\":\"6\"},{\"type\":\"fiets\",\"id\":\"7\"},{\"type\":\"fiets\",\"id\":\"8\"}]}}}}";

            // When
            string actual;
            using (var stream = new MemoryStream())
            {
                ISerializer sut = new JsonApiSerializer();
                sut.Serialize("application/vnd.api+json", data, stream);
                actual = Encoding.UTF8.GetString(stream.ToArray());
            }

            // Then
            Assert.Equal(expected, actual);
        }

        public static JsonSerializerSettings GetJsonSerializerSettings()
        {
            return new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            };
        }
    }
}
