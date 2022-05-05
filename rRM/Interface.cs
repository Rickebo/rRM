using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Management.Automation;
using PcapDotNet.Core;

namespace rRM;

public class Interface
{
    public string Name { get; set; }
    public string Address { get; set; }
    public int Metric { get; set; }
    public int Index { get; set; }
    public IpAddress[] Addresses { get; set; }
    public LivePacketDevice Device { get; set; }

    private Interface()
    {
        
    }

    public static bool TryParse(
        LivePacketDevice device, 
        Dictionary<IpAddress, AdapterInfo> windowsAdapters, 
        out Interface resultingInterface)

    {
        var addresses = device.Addresses
            .Where(address => address != null)
            .Select(address =>
            {
                return address.Address switch
                {
                    IpV4SocketAddress ipv4 => new IpAddress
                    {
                        Version = 4,
                        Address = ipv4.Address.ToString()
                    },
                    IpV6SocketAddress ipv6 => new IpAddress
                    {
                        Version = 6,
                        Address = ipv6.Address.ToString()
                    },
                    _ => null
                };
            })
            .Where(address => address != null)
            .ToArray();
        
        var foundAddress = false;
        var metric = 0;
        var index = 0;
        
        foreach (var address in addresses)
        {
            if (!windowsAdapters.TryGetValue(address, out var windowsInfo))
                continue;

            metric = windowsInfo.Metric;
            index = windowsInfo.Index;

            foundAddress = true;
        }

        if (!foundAddress)
        {
            resultingInterface = null;
            return false;
        }

        resultingInterface = new Interface()
        {
            Name = device.Description,
            Metric = metric,
            Index = index,
            Device = device,
            Address = string.Join(", ", device.Addresses
                .Select(address => ConvertAddress(address.Address))),
            Addresses = addresses
        };

        return true;
    }
    
    
    private static string ConvertAddress(SocketAddress address) =>
        address switch
        {
            IpV4SocketAddress ipv4 => "IPv4 " + ipv4.Address.ToString(),
            IpV6SocketAddress ipv6 => "IPv6 " + ipv6.Address.ToString(),
            _ => address.ToString()
        };

    public static Dictionary<IpAddress, AdapterInfo> GetWindowsInterfaces()
    {
        var ps = PowerShell.Create();

        ps.AddCommand("Set-ExecutionPolicy")
            .AddArgument("RemoteSigned");

        ps.Invoke();
        ps.Commands.Clear();

        ps.AddCommand("Import-Module")
            .AddArgument("NetTCPIP");

        ps.Invoke();

        ps.Commands.Clear();
        ps.Commands.AddCommand("Get-NetIPInterface");

        var psObjects = ps.Invoke();

        var dict = new Dictionary<IpAddress, AdapterInfo>();

        var seenIndices = new HashSet<int>();

        foreach (var psObject in psObjects)
        {
            int? metric = null;
            int? index = null;

            foreach (var propInfo in psObject.Properties)
            {
                if (propInfo is PSAdaptedProperty adapted)
                {
                    if (propInfo.Name == "InterfaceMetric")
                        metric = Convert.ToInt32(propInfo.Value);

                    if (propInfo.Name == "InterfaceIndex")
                        index = Convert.ToInt32(propInfo.Value);
                }

                // extract values...
            }

            if (!index.HasValue || !metric.HasValue)
                continue;

            if (seenIndices.Contains(index.Value))
                continue;

            seenIndices.Add(index.Value);
            
            ps.Commands.Clear();

            ps.Commands
                .AddCommand("Get-NetIPAddress")
                .AddParameter("-InterfaceIndex ", index.Value);

            var response = ps.Invoke();

            string ipv4Address = null;
            string ipv6Address = null;

            foreach (var addressObject in response)
            {
                foreach (var property in addressObject.Properties)
                {
                    if (!(property is PSAdaptedProperty adapted))
                        continue;

                    if (property.Name == "IPv4Address" && property.Value != null)
                        ipv4Address = (string)property.Value;

                    if (property.Name == "IPv6Address" && property.Value != null)
                        ipv6Address = (string)property.Value;
                }
            }

            var info = new AdapterInfo()
            {
                Metric = metric.Value,
                Index = index.Value
            };
            
            if (ipv4Address != null)
                dict.Add(new IpAddress
                {
                    Version = 4,
                    Address = ipv4Address
                }, info);
            
            if (ipv6Address != null)
                dict.Add(new IpAddress
                {
                    Version = 6,
                    Address = ipv6Address
                }, info);
        }

        return dict;
    }

    public record IpAddress
    {
        public byte Version { get; init; }
        public string Address { get; init; }
    }

    public record AdapterInfo
    {
        public int Index { get; init; }
        public int Metric { get; init; }
    }
}