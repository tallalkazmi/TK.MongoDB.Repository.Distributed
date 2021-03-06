﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Options;
using MongoDB.Bson.Serialization.Serializers;
using MongoDB.Driver;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TK.MongoDB.Distributed.Models;
using TK.MongoDB.Distributed.Test.Models;
using TK.MongoDB.Distributed.Test.ViewModels;

namespace TK.MongoDB.Distributed.Test
{
    [TestClass]
    public class MessageUnitTest : BaseTest
    {
        readonly string CollectionId;

        public MessageUnitTest()
        {
            Settings.ConnectionStringSettingName = "MongoDocConnection";
            MasterSettings.AdditionalProperties = new string[] { "CreatedBy" };

            CollectionId = "8ed066adf9c540c7a977a15b5a3da0cb";
        }

        [TestMethod]
        public async Task Find()
        {
            Message result = await MessageRepository.FindAsync(CollectionId, x => x.Text == "xyz" && x.Client == 1);
            Console.WriteLine($"Output:\n{JToken.Parse(JsonConvert.SerializeObject(result)).ToString(Formatting.Indented)}");
        }

        [TestMethod]
        public async Task Get()
        {
            var result = await MessageRepository.GetAsync(CollectionId, 1, 20);
            Console.WriteLine($"Output:\nTotal: {result.Item2}\n{JToken.Parse(JsonConvert.SerializeObject(result.Item1)).ToString(Formatting.Indented)}");
        }

        [TestMethod]
        public async Task Search()
        {
            MessageSearchParameters searchParameters = new MessageSearchParameters()
            {
                Text = "Change",
                Caterer = null,
                Client = null,
                Order = null
            };

            var builder = Builders<Message>.Filter;
            var filter = builder.Empty;
            if (!string.IsNullOrWhiteSpace(searchParameters.Text))
            {
                var criteriaFilter = builder.Regex(x => x.Text, new BsonRegularExpression($".*{searchParameters.Text}.*"));
                filter &= criteriaFilter;
            }

            if (searchParameters.Caterer.HasValue)
            {
                var criteriaFilter = builder.Eq(x => x.Caterer, searchParameters.Caterer.Value);
                filter &= criteriaFilter;
            }

            if (searchParameters.Client.HasValue)
            {
                var criteriaFilter = builder.Eq(x => x.Client, searchParameters.Client.Value);
                filter &= criteriaFilter;
            }

            if (searchParameters.Order.HasValue)
            {
                var criteriaFilter = builder.Eq(x => x.Order, searchParameters.Order.Value);
                filter &= criteriaFilter;
            }

            var result = await MessageRepository.GetAsync(CollectionId, 1, 10, filter);
            Console.WriteLine($"Output:\nTotal: {result.Item2}\n{JToken.Parse(JsonConvert.SerializeObject(result.Item1)).ToString(Formatting.Indented)}");
        }

        //[TestMethod]
        public async Task SearchInArray()
        {
            List<Guid> guids = new List<Guid>()
            {
                Guid.Parse("FC09E7EE-5E78-E811-80C7-000C29DADC00")
            };

            #region Set Serializer & Filter
            BsonClassMap.RegisterClassMap<Message>(cm =>
            {
                cm.AutoMap();
                cm.MapProperty(c => c.Read)
                    .SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<Guid, DateTime>, Guid, DateTime>(DictionaryRepresentation.ArrayOfDocuments));
            });

            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<Message>();
            var criteriaFilter = Builders<Message>.Filter.ElemMatch(x => x.Read, c => !guids.Contains(c.Key));
            var rendered = criteriaFilter.Render(serializer, BsonSerializer.SerializerRegistry);
            #endregion

            var result = await MessageRepository.GetAsync(CollectionId, rendered);
            Console.WriteLine($"Output:\n{JToken.Parse(JsonConvert.SerializeObject(result)).ToString(Formatting.Indented)}");
        }

        [TestMethod]
        public async Task Insert()
        {
            Dictionary<Guid, DateTime> Read = new Dictionary<Guid, DateTime>()
            {
                { Guid.Parse("6B9F4B43-5F78-E811-80C7-000C29DADC00"), DateTime.UtcNow }
            };

            MasterSettings.SetProperties(new Dictionary<string, object>() { { "CreatedBy", Guid.Parse("FC09E7EE-5E78-E811-80C7-000C29DADC00") } }, MasterSettings.Triggers.BeforeInsert);
            //MasterSettings.SetProperties(new Dictionary<string, object>() { { "CreatedBy", Guid.Parse("6B9F4B43-5F78-E811-80C7-000C29DADC00") } }, MasterSettings.Triggers.AfterInsert);
            Message message = new Message()
            {
                Text = $"Test message # {DateTime.UtcNow.ToShortTimeString()}",
                Client = 1,
                Caterer = 1,
                Read = Read
            };

            InsertResult<Message> result = await MessageRepository.InsertAsync(message);
            Console.WriteLine($"Success:{result.Success}\nCollectionId:{result.CollectionId}\nInserted:\n{JToken.Parse(JsonConvert.SerializeObject(result.Result)).ToString(Formatting.Indented)}");

            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(Message));
        }

        [TestMethod]
        public void Insert2()
        {
            Dictionary<Guid, DateTime> Read = new Dictionary<Guid, DateTime>()
            {
                { Guid.Parse("6B9F4B43-5F78-E811-80C7-000C29DADC00"), DateTime.UtcNow }
            };

            MasterSettings.SetProperties(new Dictionary<string, object>() { { "CreatedBy", Guid.Parse("FC09E7EE-5E78-E811-80C7-000C29DADC00") } }, MasterSettings.Triggers.BeforeInsert);
            //MasterSettings.SetProperties(new Dictionary<string, object>() { { "CreatedBy", Guid.Parse("6B9F4B43-5F78-E811-80C7-000C29DADC00") } }, MasterSettings.Triggers.AfterInsert);
            Message message = new Message()
            {
                Text = $"Test message # {DateTime.UtcNow.ToShortTimeString()}",
                Client = 1,
                Caterer = 1,
                Read = Read
            };

            InsertResult<Message> result = MessageRepository.Insert(message);
            Console.WriteLine($"Success:{result.Success}\nCollectionId:{result.CollectionId}\nInserted:\n{JToken.Parse(JsonConvert.SerializeObject(result.Result)).ToString(Formatting.Indented)}");

            Assert.IsNotNull(result.Result);
            Assert.IsInstanceOfType(result.Result, typeof(Message));
        }

        //[TestMethod]
        public async Task Update()
        {
            Message message = new Message()
            {
                Id = new ObjectId("5ec50c0098d2c12c2c4960d5"),
                Text = $"Changed @ {DateTime.UtcNow.ToShortTimeString()}"
            };

            UpdateResult<Message> result = await MessageRepository.UpdateAsync(CollectionId, message);
            Console.WriteLine($"Success:{result.Success}\nUpdated:\n{JToken.Parse(JsonConvert.SerializeObject(result.Result)).ToString(Formatting.Indented)}");
        }

        //[TestMethod]
        public async Task BulkUpdate()
        {
            List<Guid> guids = new List<Guid>()
            {
                Guid.Parse("FC09E7EE-5E78-E811-80C7-000C29DADC00")
            };

            #region Set Serializer & Filter
            BsonClassMap.RegisterClassMap<Message>(cm =>
            {
                cm.AutoMap();
                cm.MapProperty(c => c.Read)
                    .SetSerializer(new DictionaryInterfaceImplementerSerializer<Dictionary<Guid, DateTime>, Guid, DateTime>(DictionaryRepresentation.ArrayOfDocuments));
            });

            var serializer = BsonSerializer.SerializerRegistry.GetSerializer<Message>();
            var criteriaFilter = Builders<Message>.Filter.ElemMatch(x => x.Read, c => !guids.Contains(c.Key));
            var rendered = criteriaFilter.Render(serializer, BsonSerializer.SerializerRegistry);
            #endregion

            var result = await MessageRepository.GetAsync(CollectionId, rendered);

            result.ToList().ForEach(x => x.Read.Add(Guid.Parse("FC09E7EE-5E78-E811-80C7-000C29DADC00"), DateTime.UtcNow));
            long count = await MessageRepository.BulkUpdateAsync(CollectionId, result);
            Console.WriteLine($"Output:{count}");
        }

        //[TestMethod]
        public async Task Delete()
        {
            bool result = await MessageRepository.DeleteAsync(CollectionId, new ObjectId("5ec50c0098d2c12c2c4960d5"));
            Console.WriteLine($"Deleted: {result}");
        }

        [TestMethod]
        public async Task Count()
        {
            long result = await MessageRepository.CountAsync(CollectionId);
            Console.WriteLine($"Count: {result}");
        }

        [TestMethod]
        public async Task Exists()
        {
            bool result = await MessageRepository.ExistsAsync(CollectionId, x => x.Text == "abc");
            Console.WriteLine($"Exists: {result}");
        }
    }
}
