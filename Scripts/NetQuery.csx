using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using DnsClient;
using System.Threading.Tasks;
using Telegram.Bot.Types;
using TelegramBot;
using TelegramBot.Interfaces;
using TelegramBot.Types;
using Message = TelegramBot.Types.Message;

public class NetQuery: ScriptCommon,IExtension
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
    public void Handle(Message msg)
    {

    }
    async void DnsQuery(Message msg)
    {
        var lookuper = new LookupClient(new IPEndPoint[] { DefaultNS1, DefaultNS2 });


        
    }
    IPEndPoint[] GetDNS(string s,string splitStr)
    {
        var _s = s.Split(splitStr,StringSplitOptions.RemoveEmptyEntries);

        return _s.Select(IPEndPoint.Parse).ToArray();
    }
    readonly static IPEndPoint DefaultNS1 = IPEndPoint.Parse("192.168.31.11:53");
    readonly static IPEndPoint DefaultNS2 = IPEndPoint.Parse("192.168.31.4:53");
}
public enum QueryType
{
    A = 1,
    AAAA = 28,
    NS = 2,
    CNAME = 5,
    SOA = 6,
    WKS = 11,
    PTR = 12,
    HINFO = 13,
    MX = 15,
    ANY = 255
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
