using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;
using System;
using System.Text.Json.Serialization;

namespace NekoBot.Types;

public class MaiAccount
{
    [JsonIgnore]
    [BsonId]
    [BsonElement("_id")]
    ObjectId _id;
    public string userName { get; set; }
    public long playerRating { get; set; }
    public int userId { get; set; }
    public string lastGameId { get; set; }
    public string lastDataVersion { get; set; }
    public string lastRomVersion { get; set; }
    public int? banState { get; set; }
    public DateTime lastUpdate { get; set; }

}