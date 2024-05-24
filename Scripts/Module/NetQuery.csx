using System;
using System.Linq;
using System.Net;
using System.Text;
using CSScripting;
using DnsClient;
using DnsClient.Protocol;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using NekoBot;
using Message = NekoBot.Types.Message;
using NekoBot.Interfaces;
using NekoBot.Types;
using Version = NekoBot.Types.Version;
using System.Diagnostics;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
public class NetQuery: Extension, IExtension
{
    public new ExtensionInfo Info { get; } = new ExtensionInfo()
    {
        Name = "NetQuery",
        Version = new Version() { Major = 1, Minor = 0,Revision = 13 },
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
        if (param.IsEmpty() || ipOrdomain is null)
            return;

        if(useTCP)
        {
            var _s = ipOrdomain.Split(":");
            var portStr = _s.LastOrDefault();
            if(_s.Length > 1)
            {
                if(!int.TryParse(portStr, out port))
                    port = 80;
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
            for (int i = 0; i < 4; i++)
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
                }
                else
                {
                    var time = TCPing(address.ToString(), port);
                    if(time  != -1)
                        s += $"\nFrom {address} Seq={i + 1} Time={Math.Round(time / 1000000 ,4)}ms";
                    else
                        s += "\nTimeOut";
                }
                await msg.Edit(sHead + StringHandle(s) + sTail,ParseMode.MarkdownV2);
                await Task.Delay(1000);
            }
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
            return -1;
        }
        finally
        {
            stopwatch.Stop();
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
