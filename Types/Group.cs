using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using NekoBot.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Telegram.Bot.Types;

namespace NekoBot.Types;
public class Group : IAccount<Group>
{
    [BsonId]
    [BsonElement("_id")]
    ObjectId _id { get; set; }
    [BsonElement("Id")]
    public long Id { get; set; }
    public string Username { get; set; }
    public string Name { get; set; }
    public BotConfig Setting { get; set; } = new();
    public List<FilterRule> Rules { get; set; } = new();
    public Permission Level { get; set; } = Permission.Common;
    public void SetPermission(Permission targetLevel)
    {
        Level = targetLevel;
    }
    public bool CheckPermission(Permission targetLevel) => Level >= targetLevel;
    public static void Update(Update update)
    {

    }
    public Expression<Func<Group, bool>> GetMatcher() => x => x.Id == Id;
}
