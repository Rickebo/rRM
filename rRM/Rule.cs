using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Json;
using Newtonsoft.Json;

namespace rRM;

public class Rule
{
    public string Name { get; set; }
    public string Application { get; set; }
    public string Destination { get; set; }
    public string Interface { get; set; }

    public static List<Rule> LoadRules()
    {
        var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "rRM");
        
        if (!Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        var file = Path.Combine(dir, "rules.json");

        RuleCollection rc;

        if (!File.Exists(file))
        {
            rc = new RuleCollection()
            {
                Rules = new List<Rule>()
                {
                    new Rule()
                    {
                        Name = "Sample rule",
                        Application = "discord",
                        Interface = "Network adapter 'Apple' on local host"
                    },
                    new Rule()
                    {
                        Name = "Another sample rule",
                        Destination = "1.1.1.0",
                        Interface = "Network adapter 'Apple' on local host"
                    }
                }
            };

            var json = JsonConvert.SerializeObject(rc, Formatting.Indented);
            File.WriteAllText(file, json);
        }
        else
        {
            var json = File.ReadAllText(file);
            rc = JsonConvert.DeserializeObject<RuleCollection>(json);
        }

        return rc.Rules.ToList();
    }

    private class RuleCollection
    {
        public List<Rule> Rules { get; set; } 
    }
}