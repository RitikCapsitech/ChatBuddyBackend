using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatbotFAQApi.Models
{
    public class FaqItem
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("query")]
        [BsonRequired]
        public string Query { get; set; }

        [BsonElement("response")]
        [BsonRequired]
        public string Response { get; set; }

        [BsonElement("options")]
        [BsonIgnoreIfNull]
        public List<FaqOption> Options { get; set; } = new List<FaqOption>();
    }

    public class FaqOption
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }

        [BsonElement("optionText")]
        [BsonRequired]
        public string OptionText { get; set; }

        [BsonElement("response")]
        [BsonRequired]
        public string Response { get; set; }


        [BsonElement("options")]
        [BsonIgnoreIfNull]
        public List<FaqOption>? Options { get; set; } = new List<FaqOption>();
    }

    public class FaqBulkRequest
    {
        public List<FaqItem> Items { get; set; }
    }
}
