using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using CSScripting;
using DnsClient;
using DnsClient.Protocol;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using TelegramBot;
using TelegramBot.Interfaces;
using TelegramBot.Types;
using Message = TelegramBot.Types.Message;

public class NetQuery: IExtension
{
    public Assembly ExtAssembly { get => Assembly.GetExecutingAssembly(); }
    public BotCommand[] Commands { get; } =
    {
        new BotCommand()
        {
             Command = "nslookup",
             Description = "域名解析"
        }
    };
    public string Name { get; } = "NetQuery";
    public MethodInfo GetMethod(string methodName) => ExtAssembly.GetType().GetMethod(methodName);
    public void Init()
    {

    }
    public void Save()
    {

    }
    public void Destroy()
    {

    }
    public void Handle(Message userMsg)
    {
        var cmd = (Command)userMsg.Command;
        switch(cmd.Prefix)
        {
            case "nslookup":
                DnsQuery(userMsg);
            return;
        }
    }
    async void DnsQuery(Message userMsg)
    {
        // /nslookup [host]
        // /nslookup [protocol] [host] [NS]
        // /nslookup [protocol] [host] [NS] [queryType]
        var param = ((Command)userMsg.Command).Params;
        var lookuper = new LookupClient(new IPEndPoint[] { DefaultNS1, DefaultNS2 });
        string host = "";
        QueryType qType = QueryType.A;
        DnsProtocol protocol = DnsProtocol.Udp;

        if (param.IsEmpty())
        {
            //GetHelpInfo
        }
        else if (param.Length == 1)
            host = param[0];
        else if (param.Length >= 3)
        {
            protocol = param[0].ToLower() switch
            {
                "udp" => DnsProtocol.Udp,
                "tcp" => DnsProtocol.Tcp,
                "tls" => DnsProtocol.Tls,
                "quic" => DnsProtocol.Quic,
                "https" => DnsProtocol.Https,
                _ => DnsProtocol.Unknown
            };
            host = param[1];
            var ns = GetDNS(param[2],",");
            if(ns is null)
            {
                await userMsg.Reply($"Invaild NameServer: \"{param[2]}\"");
                return;
            }
            lookuper = new LookupClient(ns);
        }
        if (param.Length == 4)
            qType = param[3].ToLower() switch
            {
                "a" => QueryType.A,
                "aaaa" => QueryType.AAAA,
                "ptr" => QueryType.PTR,
                "cname" => QueryType.CNAME,
                "any" => QueryType.ANY,
                _ => QueryType.A,
            };
        try
        {
            var question = new DnsQuestion(host, qType);
            var result = await lookuper.QueryAsync(question);

            string nsInfo = $"NameServer: {result.NameServer}\n" +
                            $"Protocol  : {protocol}\n";
            string rspHeader = "```bash\n";
            string rspTailer = "\n```";
            string rsp = "";

            if (result.HasError)
            {
                rsp = $"{rspHeader}" + Program.StringHandle(
                      $"{nsInfo}\n" +
                      $"From \"{result.NameServer}\" message: {result.ErrorMessage}") +
                      $"{rspTailer}";
                await userMsg.Reply(rsp, ParseMode.MarkdownV2);
                return;
            }

            var g = result.Answers.GroupBy(x => x.DomainName);
            foreach (var record in g)
            {

                rsp += $"Domain: {record.Key}\n";
                record.ForEach(r =>
                {
                    if (r is AddressRecord address)
                        rsp += $"{GetRecTypeStr(r.RecordType)}: {address.Address}\n" +
                               $"Ttl: {r.TimeToLive}\n\n";
                    else if (r is CNameRecord cname)
                        rsp += $"{GetRecTypeStr(r.RecordType)}: {cname.CanonicalName}\n" +
                               $"Ttl: {r.TimeToLive}\n\n";
                });
            }
            await userMsg.Reply($"{rspHeader}" + Program.StringHandle(
                                $"{nsInfo}\n" +
                                $"{rsp}") +
                                $"{rspTailer}", ParseMode.MarkdownV2);
        }
        catch(Exception e)
        {
            await userMsg.Reply("```csharp\n" +
                               $"{Program.StringHandle(e.ToString())}\n" +
                               $"```",ParseMode.MarkdownV2);
        }
    }
    string GetRecTypeStr(ResourceRecordType type)
    {
        switch(type)
        {
            case ResourceRecordType.A:
                return "IPv4";
            case ResourceRecordType.AAAA:
                return "IPv6";
            case ResourceRecordType.CNAME:
                return "CNAME";
            case ResourceRecordType.PTR:
                return "PTR";
            default:
                return "Undefined";
        }
    }
    IPEndPoint[]? GetDNS(string s,string splitStr)
    {
        try
        {
            var _s = s.Split(splitStr, StringSplitOptions.RemoveEmptyEntries);

            return _s.Select(IPEndPoint.Parse).ToArray();
        }
        catch
        {
            return null;
        }
    }
    readonly static IPEndPoint DefaultNS1 = IPEndPoint.Parse("192.168.31.11:53");
    readonly static IPEndPoint DefaultNS2 = IPEndPoint.Parse("192.168.31.4:53");
    //readonly static IPEndPoint DefaultNS1 = IPEndPoint.Parse("10.0.0.1:53");
    //readonly static IPEndPoint DefaultNS2 = IPEndPoint.Parse("10.0.0.232:53");
}
public enum DnsProtocol
{
    Udp,
    Tcp,
    Tls,
    Https,
    Quic,
    Unknown
}

public class DnsQuery
{
    public string Host { get; set; }
    public bool Recursion { get; set; }
    public QueryType Type { get; set; }
    public byte[] ToArray()
    {
        var rd = new Random();

        var packet = new byte[512];
        var id = (ushort)rd.Next(0, ushort.MaxValue);

        // Transaction ID
        packet[0] = (byte)(id >> 8);
        packet[1] = (byte)(id & 0xFF);
        // RD Flag
        packet[2] = (byte)(Recursion ? 0x01 : 0x00);
        packet[3] = 0x00;
        // Questions
        packet[4] = 0x00;
        packet[5] = 0x01;
        // Answer RRs
        packet[6] = 0x00;
        packet[7] = 0x00;
        // Authority RRs
        packet[8] = 0x00;
        packet[9] = 0x00;
        // Additional RRs
        packet[10] = 0x00;
        packet[11] = 0x00;

        int pos = 12;
        var labels = Host.Split('.');
        foreach (string label in labels)
        {
            packet[pos++] = (byte)label.Length;
            var b = Encoding.ASCII.GetBytes(label);
            b.CopyTo(packet, pos);
            pos += b.Length;
        }

        // End Flag
        packet[pos++] = 0x00;

        // Query Type
        packet[pos++] = 0x00;
        packet[pos++] = 0x01;

        packet[pos++] = 0x00;
        packet[pos++] = (byte)Type;

        byte[] _packet = new byte[pos];
        Array.Copy(packet, _packet, pos);

        return _packet;
    }
}
public static class DnsHelper
{
    static void MakeQueryPacket()
    {

    }
}
