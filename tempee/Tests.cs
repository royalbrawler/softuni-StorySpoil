using System.Net;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using storyy.Models;

namespace storyy
{
    [TestFixture]
    public class StoryyTests
    {
        private RestClient _client;
        private static string __createdStoryId;
        private const string BaseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        [OneTimeSetUp]
        public void Setup()
        {
            string token = GetJWTtoker("gorillazfeelgood321321", "gorillazfeelgood321321");
            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(token),
            };
            _client = new RestClient(options);
        }

        private string GetJWTtoker(string username, string password)
        {
            var loginclient = new RestClient(BaseUrl);
            var request = new RestRequest("/api/User/Authentication", Method.Post);
            request.AddJsonBody(new { userName = username, password });
            var response = loginclient.Execute(request);
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                return content.GetProperty("accessToken").GetString();
            }
            throw new Exception(
                $"Failed to get JWT token: {response.StatusCode} {response.Content}"
            );
        }

        [Test, Order(1)]
        public void CreateStory_shouldreturnCreated()
        {
            var food = new
            {
                title = "TestStory312312",
                description = "TestDescription",
                url = "",
            };
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(food);
            var response = _client.Execute(request);

            Assert.That(
                response.StatusCode,
                Is.EqualTo(HttpStatusCode.Created),
                "Expected status code 201 Created, but got: " + response.StatusCode
            );

            var jsonResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(
                jsonResponse.StoryId,
                Is.Not.Null.Or.Empty,
                "Expected story ID to be returned, but it was null or empty."
            );
            Assert.That(jsonResponse.msg, Is.EqualTo("Successfully created!"));

            __createdStoryId = jsonResponse.StoryId;
        }

        [Test, Order(2)]
        public void EditStoryTitle_shouldReturnOk()
        {
            if (string.IsNullOrEmpty(__createdStoryId))
            {
                Assert.Fail("No story ID available for editing.");
            }
            var updatedStory = new
            {
                title = "TestStory312312_Updated",
                description = "pew",
                url = "",
            };
            var request = new RestRequest($"/api/Story/Edit/{__createdStoryId}", Method.Put);
            request.AddJsonBody(updatedStory);
            var response = _client.Execute(request);
            Assert.That(
                response.StatusCode,
                Is.EqualTo(HttpStatusCode.OK),
                "Expected status code 200 OK, but got: " + response.StatusCode
            );

            var jsonResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(jsonResponse.msg, Is.EqualTo("Successfully edited"));
        }

        [Test, Order(3)]
        public void GetAllStorySpoilers_shouldReturnOk()
        {
            var request = new RestRequest("/api/Story/All", Method.Get);
            var response = _client.Execute(request);
            Assert.That(
                response.StatusCode,
                Is.EqualTo(HttpStatusCode.OK),
                "Expected status code 200 OK, but got: " + response.StatusCode
            );
            var stories = JsonSerializer.Deserialize<List<StoryDTO>>(response.Content);

            Assert.That(stories, Is.Not.Null, "Response should not be null");
            Assert.That(stories.Count, Is.GreaterThan(0), "Stories array should not be empty");
        }

        [Test, Order(4)]
        public void DeleteStory_shouldReturnOk()
        {
            if (string.IsNullOrEmpty(__createdStoryId))
            {
                Assert.Fail("No story ID available for deletion.");
            }
            var request = new RestRequest($"/api/Story/Delete/{__createdStoryId}", Method.Delete);
            var response = _client.Execute(request);
            Assert.That(
                response.StatusCode,
                Is.EqualTo(HttpStatusCode.OK),
                "Expected status code 200 OK, but got: " + response.StatusCode
            );
            var jsonResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(
                jsonResponse.msg,
                Is.EqualTo("Deleted successfully!"),
                "Expected success message for deletion."
            );
        }

        [Test, Order(5)]
        public void TryCreateStoryWithoutRequiredFields_shouldReturnBadRequest()
        {
            var invalidStory = new
            {
                title = "",
                description = "",
                url = "",
            };
            var request = new RestRequest("/api/Story/Create", Method.Post);
            request.AddJsonBody(invalidStory);
            var response = _client.Execute(request);
            Assert.That(
                response.StatusCode,
                Is.EqualTo(HttpStatusCode.BadRequest),
                "Expected status code 400 Bad Request, but got: " + response.StatusCode
            );
        }

        [Test, Order(6)]
        public void EditNonExistentStory_shouldReturnNotFound()
        {
            var updatedStory = new
            {
                title = "NonExistentStoryTitle",
                description = "NonExistentStoryDescription",
                url = "",
            };
            var request = new RestRequest("/api/Story/Edit/NonExistentStoryIdASDASD", Method.Put);
            request.AddJsonBody(updatedStory);
            var response = _client.Execute(request);
            Assert.That(
                response.StatusCode,
                Is.EqualTo(HttpStatusCode.NotFound),
                "Expected status code 404 Not Found, but got: " + response.StatusCode
            );
            var jsonResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(
                jsonResponse.msg,
                Is.EqualTo("No spoilers..."),
                "Expected message for Non Existent Story."
            );
        }

        [Test, Order(7)]
        public void DeleteNonExistentStory_shouldReturnBadRequest()
        {
            var request = new RestRequest(
                "/api/Story/Delete/NonExistentStoryIdASDASD",
                Method.Delete
            );
            var response = _client.Execute(request);
            Assert.That(
                response.StatusCode,
                Is.EqualTo(HttpStatusCode.BadRequest),
                "Expected status code 400 Bad Request, but got: " + response.StatusCode
            );
            var jsonResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);
            Assert.That(
                jsonResponse.msg,
                Is.EqualTo("Unable to delete this story spoiler!"),
                "Expected message for deleting a Existent Story."
            );
        }

        [OneTimeTearDown]
        public void Cleanup()
        {
            _client?.Dispose();
        }
    }
}
