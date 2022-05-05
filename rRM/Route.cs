using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Automation;

namespace rRM;

public class Route
{
    public string Destination { get; set; }
    public string Gateway { get; set; }
    public int Mask { get; set; }
    public int InterfaceIndex { get; set; }
    public int Metric { get; set; }
    public int InterfaceMetric { get; set; }
    public int EffectiveMetric => Metric + InterfaceMetric;

    public static Route[] Read(Dictionary<int, Interface> interfaces)
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
        ps.Commands.AddCommand("Get-NetRoute");

        var psObjects = ps.Invoke();
        var routes = new List<Route>();

        foreach (var psObject in psObjects)
        {
            string destination = null;
            string gateway = null;
            int? mask = null;
            int? index = null;
            int? metric = null;
            int? interfaceMetric = null;

            foreach (var propInfo in psObject.Properties)
            {
                if (propInfo is PSAdaptedProperty adapted)
                {
                    switch (propInfo.Name)
                    {
                        case "InterfaceIndex":
                            index = Convert.ToInt32(propInfo.Value);
                            break;

                        case "RouteMetric":
                            metric = Convert.ToInt32(propInfo.Value);
                            break;
                        
                        case "InterfaceMetric":
                            interfaceMetric = Convert.ToInt32(propInfo.Value);
                            break;

                        case "DestinationPrefix":
                            destination = (string)propInfo.Value;

                            if (destination.Contains('/'))
                            {
                                var splitDestination = destination.Split('/');
                                destination = splitDestination[0];
                                mask = splitDestination.Length > 1 &&
                                       int.TryParse(splitDestination[1], out var parsedMask)
                                    ? parsedMask
                                    : null;
                            }

                            break;
                    }
                }
            }

            if (destination == null || mask == null || index == null || metric == null || interfaceMetric == null)
                continue;

            gateway = interfaces.TryGetValue(index.Value, out var @interface)
                ? @interface.Address
                : null;

            if (gateway == null)
                continue;

            routes.Add(new Route()
            {
                Destination = destination,
                Gateway = gateway,
                Mask = mask.Value,
                Metric = metric.Value,
                InterfaceIndex = index.Value,
                InterfaceMetric = interfaceMetric.Value
            });
        }
        
        return routes.OrderBy(route => route.Metric).ToArray();
    }
}