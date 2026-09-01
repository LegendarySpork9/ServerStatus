// Copyright © - Unpublished - Toby Hunter
using ServerStatusCommon.Functions;

namespace ServerStatus.UnitTests.Common.Functions
{
    [TestClass]
    public class URLBuilderFunctionTest
    {
        /// <summary>
        /// Checks whether the BuildURL method returns the base URL and endpoint.
        /// </summary>
        [TestMethod]
        public void TestBuildURLBasic()
        {
            string expected = "https://api.example.com/custom";
            string actual = URLBuilderFunction.BuildURL("https://api.example.com", "/custom");

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the BuildURL method appends the entity ID.
        /// </summary>
        [TestMethod]
        public void TestBuildURLWithEntityId()
        {
            string expected = "https://api.example.com/custom/42";
            string actual = URLBuilderFunction.BuildURL("https://api.example.com", "/custom", 42);

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the BuildURL method appends query parameters to an endpoint with no default query.
        /// </summary>
        [TestMethod]
        public void TestBuildURLWithQueryParameters()
        {
            string expected = "https://api.example.com/custom?page=1&limit=50";

            List<KeyValuePair<string, object>> queryParameters =
            [
                new("page", 1),
                new("limit", 50)
            ];

            string actual = URLBuilderFunction.BuildURL("https://api.example.com", "/custom", queryParameters: queryParameters);

            Assert.AreEqual(
                expected,
                actual);
        }

        /// <summary>
        /// Checks whether the BuildURL method appends query parameters to an endpoint with a default query.
        /// </summary>
        [TestMethod]
        public void TestBuildURLWithDefaultQueryAndParameters()
        {
            List<KeyValuePair<string, object>> queryParameters =
            [
                new("username", "tester")
            ];

            string actual = URLBuilderFunction.BuildURL("https://api.example.com", "/user", queryParameters: queryParameters);

            Assert.IsTrue(actual.Contains("includeDeleted=false"));
            Assert.IsTrue(actual.Contains("username=tester"));
        }

        /// <summary>
        /// Checks whether the BuildURL method ignores the query when ignoreQuery is true.
        /// </summary>
        [TestMethod]
        public void TestBuildURLIgnoreQuery()
        {
            string expected = "https://api.example.com/issues";
            string actual = URLBuilderFunction.BuildURL("https://api.example.com", "/issues", ignoreQuery: true);

            Assert.AreEqual(
                expected,
                actual);
            Assert.IsFalse(actual.Contains("?"));
        }

        /// <summary>
        /// Checks whether the BuildURL method combines entity ID and query parameters.
        /// </summary>
        [TestMethod]
        public void TestBuildURLWithEntityIdAndParameters()
        {
            List<KeyValuePair<string, object>> queryParameters =
            [
                new("detail", "full")
            ];

            string actual = URLBuilderFunction.BuildURL("https://api.example.com", "/custom", 7, queryParameters);

            Assert.IsTrue(actual.Contains("/custom/7"));
            Assert.IsTrue(actual.Contains("detail=full"));
        }
    }
}
