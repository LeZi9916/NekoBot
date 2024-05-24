using System;
using System.Linq;
using System.Net;
using System.Text;
using CSScripting;
using DnsClient;
using DnsClient.Protocol;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Message = NekoBot.Types.Message;
using NekoBot.Interfaces;
using NekoBot.Types;
using Version = NekoBot.Types.Version;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Collections.Generic;
public class NetQuery: Extension, IExtension
{
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "NetQuery",
        Version = new Version() { Major = 1, Minor = 0,Revision = 15 },
        Type = ExtensionType.Module,
        Commands = new BotCommand[]
        {
            new BotCommand()
            {
                Command = "nslookup",
                Description = "域名解析"
            },
            new BotCommand()
            {
                Command = "ping",
                Description = "对目标主机发送ICMP Echo"
            },
            new BotCommand()
            {
                Command = "tcping",
                Description = "对目标主机发送TCP SYN"
            }
        },
        SupportUpdate = new UpdateType[]
        {
            UpdateType.Message,
            UpdateType.EditedMessage
        }
    };
    public override void Handle(Message userMsg)
    {
        var cmd = (Command)userMsg.Command!;
        switch(cmd.Prefix)
        {
            case "nslookup":
                DnsQuery(userMsg);
                break;
            case "ping":
                PingRequest(userMsg);
                break;
            case "tcping":
                PingRequest(userMsg,true);
                break;
        }
    }
    async void PingRequest(Message userMsg, bool useTCP = false)
    {
        var param = ((Command)userMsg.Command!).Params;
        var ipOrdomain = param.FirstOrDefault();
        IPAddress? address;
        int port = 80;
        int count = 4;
        if (param.IsEmpty() || ipOrdomain is null)
            return;

        if(useTCP)
        {
            var _s = ipOrdomain.Split(":");
            var portStr = _s.LastOrDefault();
            if(_s.Length > 1)
            {
                if(!int.TryParse(portStr, out port))
                {
                    port = 80;
                    ipOrdomain = string.Join(":", _s);
                }
                else
                    ipOrdomain = string.Join(":", _s.SkipLast(1));
            }                
            else
                ipOrdomain = _s.FirstOrDefault();

        }

        if(IPAddress.TryParse(ipOrdomain, out address))
        {
            if (address is null)
                return;
        }
        else
        {
            var lookuper = new LookupClient(new IPEndPoint[] { DefaultNS1, DefaultNS2 });
            var question = new DnsQuestion(ipOrdomain, QueryType.A);
            var result = await lookuper.QueryAsync(question);
            address = result.Answers.AaaaRecords()?.FirstOrDefault()?.Address ?? result.Answers?.ARecords()?.FirstOrDefault()?.Address;
            if(address is null)
            {
                await userMsg.Reply("Unknown address or host");
                return;
            }
        }
        string sHead = "```log\n";
        string sTail = "\n```";
        string s = useTCP ? $"TCPING {ipOrdomain} ({address}) via port {port}"  : $"PING {ipOrdomain} ({address})";
        var msg = await userMsg.Reply(sHead + StringHandle(s) + sTail, ParseMode.MarkdownV2);
        if (msg is null)
            return;
        try
        {
            int error = 0;
            List<double> rtt = new();
            for (int i = 0; i < count; i++)
            {
                if(!useTCP)
                {
                    var ping = new Ping();
                    var reply = await ping.SendPingAsync(address);
                    switch (reply.Status)
                    {
                        case IPStatus.TimedOut:
                            s += "\nTimeOut";
                            break;
                        case IPStatus.Success:
                            s += $"\nFrom {address} Seq={i + 1} Ttl={(reply.Options is null ? "Null" : reply.Options.Ttl)} Time={reply.RoundtripTime}ms";
                            break;
                        case IPStatus.DestinationNetworkUnreachable:
                        case IPStatus.DestinationProtocolUnreachable:
                        case IPStatus.DestinationPortUnreachable:
                        case IPStatus.DestinationHostUnreachable:
                            s += $"\nUnreachable";
                            break;
                    }
                    if (reply.Status != IPStatus.Success)
                        error++;
                    rtt.Add(reply.RoundtripTime);
                }
                else
                {
                    var time = TCPing(address.ToString(), port);
                    if(time  >= 0)
                        s += $"\nFrom {address} Seq={i + 1} Time={Math.Round(time / 1000000 ,4)}ms";
                    else
                    {
                        s += "\nTimeOut";
                        error++;
                    }
                    rtt.Add(Math.Abs(Math.Round(time / 1000000, 4)));
                }
                await msg.Edit(sHead + StringHandle(s) + sTail,ParseMode.MarkdownV2);
                await Task.Delay(1000);
            }
            s += $"""

                  --- {ipOrdomain} {(useTCP ? "TCPing" : "Ping")} statistics ---
                     {Math.Round(error / (double)count * 100,2)}% packet loss, time {rtt.Sum()}ms
                  rtt min/avg/max = {rtt.Min()}/{Math.Round(rtt.Sum() / count,4)}/{rtt.Max()} ms
                  """;
            await msg.Edit(sHead + StringHandle(s) + sTail, ParseMode.MarkdownV2);
        }
        catch
        {
            s += "\nOperation Canceled";
            await msg.Edit(sHead + StringHandle(s) + sTail, ParseMode.MarkdownV2);
        }
    }
    async void DnsQuery(Message userMsg)
    {
        // /nslookup [host]
        // /nslookup [protocol] [host] [NS]
        // /nslookup [protocol] [host] [NS] [queryType]
        var param = ((Command)userMsg.Command!).Params;
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
                rsp = $"{rspHeader}" + StringHandle(
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
            await userMsg.Reply($"{rspHeader}" + StringHandle(
                                $"{nsInfo}\n" +
                                $"{rsp}") +
                                $"{rspTailer}", ParseMode.MarkdownV2);
        }
        catch(Exception e)
        {
            await userMsg.Reply("```csharp\n" +
                               $"{StringHandle(e.ToString())}\n" +
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
    public double TCPing(string host, int port)
    {
        Stopwatch stopwatch = new();
        stopwatch.Start();
        try
        {
            TcpClient client = new TcpClient();
            client.SendTimeout = 2000;
            client.ReceiveTimeout = 2000;
            client.Connect(host, port);
            client.Close();
            stopwatch.Stop();
            return stopwatch.Elapsed.TotalNanoseconds;

        }
        catch
        {
            stopwatch.Stop();
            return -stopwatch.Elapsed.TotalNanoseconds;
        }

    }
    RouteInfo IcmpTrace(IPAddress address, int timeout = 2000, int maxHops = 30)
    {
        Ping sender = new();
        PingOptions opt = new();
        byte[] buffer = Enumerable.Repeat<byte>(1, 32).ToArray();
        List<Router?> routers = new();

        for (int i = 1; i <= maxHops; i++)
        {
            opt.Ttl = i;
            var result = sender.Send(address, timeout, buffer, opt);
            if (result.Status is IPStatus.Success || result.Status is IPStatus.TtlExpired)
            {
                var router = Router.Parse(result);
                routers.Add(router);
                if ((bool)router?.Address.Equals(address))
                    break;
            }
            else
                routers.Add(Router.Empty());
        }
        return new RouteInfo(address, routers.ToArray());
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
public class RouteInfo
{
    public IPAddress Address { get; }
    public long RoundtripTime { get; }
    public Router?[] Routers { get; }
    public bool IsAchieved()
    {
        var lastRouter = Routers.Last();
        if (lastRouter.IsUnreachable())
            return false;
        else
            return lastRouter.Address == Address;
    }
    public RouteInfo(IPAddress address, Router?[] routers)
    {
        Address = address;
        Routers = routers;
        var lastRouter = routers.Last();

        if (lastRouter.IsUnreachable() || lastRouter.Address != Address)
            RoundtripTime = -1;
        else
            RoundtripTime = lastRouter.RoundtripTime;
    }
}
public class Router
{
    public IPAddress? Address { get; }
    public long RoundtripTime { get; }
    public bool IsUnreachable() => Address is null;
    public Router(IPAddress? address, long rrt)
    {
        Address = address;
        RoundtripTime = rrt;
    }
    public static Router? Parse(PingReply? pingReply)
    {
        if (pingReply == null) return null;

        return new Router(pingReply.Address, pingReply.RoundtripTime);
    }
    public static Router Empty() => new Router(null, -1);
}
