using System;
using System.Text;
using System.Text.Json;
using NUnit.Framework;
using RestSharp;
using RestSharp.Authenticators;
using ExamTask.Models;
using System.Net;


namespace ExamTask

{
    [TestFixture]
    public class StoryApiTests
    {
        private RestClient client;
        private static string lastCreatedStoryId;

        private const string BaseUrl = "https://d3s5nxhwblsjbi.cloudfront.net";

        private const string StaticToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiIyNzEyZTE4My1jZjdhLTQxOWYtYmJjNC0wMTViNDZiYzBjNzciLCJpYXQiOiIwNy8wNS8yMDI1IDA3OjQ3OjM3IiwiVXNlcklkIjoiOTQ0NTE1ZjEtOWJkMy00ZjI3LWExYmEtMDhkZGJiODk4Njg2IiwiRW1haWwiOiJJY29AZ21haWwuY29tIiwiVXNlck5hbWUiOiJJY2FrYSIsImV4cCI6MTc1MTcyMzI1NywiaXNzIjoiU3RvcnlTcG9pbF9BcHBfU29mdFVuaSIsImF1ZCI6IlN0b3J5U3BvaWxfV2ViQVBJX1NvZnRVbmkifQ.OswZJw9FQ8MPcXztchsKYHcE7ZUCgCsQ8xOBrFbnqlI\",\"completeSessionValue\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJzdWIiOiJKd3RTZXJ2aWNlQWNjZXNzVG9rZW4iLCJqdGkiOiIyNzEyZTE4My1jZjdhLTQxOWYtYmJjNC0wMTViNDZiYzBjNzciLCJpYXQiOiIwNy8wNS8yMDI1IDA3OjQ3OjM3IiwiVXNlcklkIjoiOTQ0NTE1ZjEtOWJkMy00ZjI3LWExYmEtMDhkZGJiODk4Njg2IiwiRW1haWwiOiJJY29AZ21haWwuY29tIiwiVXNlck5hbWUiOiJJY2FrYSIsImV4cCI6MTc1MTcyMzI1NywiaXNzIjoiU3RvcnlTcG9pbF9BcHBfU29mdFVuaSIsImF1ZCI6IlN0b3J5U3BvaWxfV2ViQVBJX1NvZnRVbmkifQ.OswZJw9FQ8MPcXztchsKYHcE7ZUCgCsQ8xOBrFbnqlI\",\"sessionIndex\":1}]";

        private const string userName = "Icaka";
        private const string password = "ico123123";

        [OneTimeSetUp]
        public void Setup()
        {
            string jwtToken;

            if (!string.IsNullOrWhiteSpace(StaticToken))
            {
                jwtToken = StaticToken;
            }
            else
            {
                jwtToken = GetJwtToken(userName, password);
            }

            var options = new RestClientOptions(BaseUrl)
            {
                Authenticator = new JwtAuthenticator(jwtToken),
            };

            this.client = new RestClient(options);
        }

        private string GetJwtToken(string email, string password)
        {
            var tempCLient = new RestClient(BaseUrl);
            var request = new RestRequest("/User/Login", Method.Post);
            request.AddJsonBody(new { userName, password });

            var response = tempCLient.Execute(request);

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = JsonSerializer.Deserialize<JsonElement>(response.Content);
                var token = content.GetProperty("accessToken").GetString();

                if (string.IsNullOrWhiteSpace(token))
                {
                    throw new InvalidOperationException("Failed to retrieve JWT token from the response.");
                }
                return token;
            }
            else
            {
                throw new InvalidOperationException($"Failed to authenticate. Status code: {response.StatusCode}, Content: {response.Content}");
            }
        }

    // ---------- TESTS ----------

    [Order(1)]
        [Test]
        public void CreateStory_ShouldReturnSuccess()
        {
            
            var storyRequest = new
            {
                Title = "My First Story",
                Description = "This is a test story created via API",
                Status = "Pending"
            };

            
            var request = new RestRequest("/Story/Add", Method.Post);

            
            request.AddHeader("Content-Type", "application/json");
            request.AddJsonBody(storyRequest);

            
            var response = this.client.Execute(request);

            
            Console.WriteLine("Response Status: " + response.StatusCode);
            Console.WriteLine("Response Content: " + response.Content);

            Assert.That(response.Content, Is.Not.Null.And.Not.Empty, "API returned empty content!");

            var createResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(createResponse.Msg, Is.EqualTo("Successfully created!"));
        }

        [Order(2)]
        [Test]
        public void GetAllStories_ShouldReturnListOfStories()
        {
            var request = new RestRequest("Story/All", Method.Get);
            var response = this.client.Execute(request);

            var stories = JsonSerializer.Deserialize<List<StoryDTO>>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(stories, Is.Not.Null);
            Assert.That(stories, Is.Not.Empty);

            lastCreatedStoryId = stories[^1].Id; 
        }

        [Order(3)]
        [Test]
        public void EditExistingStory_ShouldReturnSuccess()
        {
            var editRequest = new
            {
                Title = "Edited Story Title",
                Description = "This story has been updated",
                Status = "InProgress"
            };

            var request = new RestRequest("/Story/Edit", Method.Put);
            request.AddQueryParameter("storyId", lastCreatedStoryId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);

            var editResponse = JsonSerializer.Deserialize<ApiResponseDTO>(response.Content);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(editResponse.Msg, Is.EqualTo("Edited successfully"));
        }

        [Order(4)]
        [Test]
        public void DeleteStory_ShouldReturnSuccess()
        {
            var request = new RestRequest("/Story/Delete", Method.Delete);
            request.AddQueryParameter("storyId", lastCreatedStoryId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(response.Content, Does.Contain("The story is deleted!"));
        }

        [Order(5)]
        [Test]
        public void CreateStory_WithoutRequiredFields_ShouldReturnBadRequest()
        {
            var storyRequest = new
            {
                Title = "",
                Description = ""
            };

            var request = new RestRequest("/Story/Add", Method.Post);
            request.AddJsonBody(storyRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
        }

        [Order(6)]
        [Test]
        public void EditNonExistingStory_ShouldReturnNotFound()
        {
            string nonExistingStoryId = "123";
            var editRequest = new
            {
                Title = "FakeStory",
                Description = "Trying to edit non-existing story"
            };

            var request = new RestRequest("/Story/Edit", Method.Put);
            request.AddQueryParameter("storyId", nonExistingStoryId);
            request.AddJsonBody(editRequest);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such story!"));
        }

        [Order(7)]
        [Test]
        public void DeleteNonExistingStory_ShouldReturnNotFound()
        {
            string nonExistingStoryId = "123";

            var request = new RestRequest("/Story/Delete", Method.Delete);
            request.AddQueryParameter("storyId", nonExistingStoryId);
            var response = this.client.Execute(request);

            Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
            Assert.That(response.Content, Does.Contain("There is no such story!"));
        }

        [OneTimeTearDown]
        public void Teardown()
        {
            this.client?.Dispose();
        }
    }
}