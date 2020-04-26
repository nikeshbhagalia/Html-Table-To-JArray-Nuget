﻿using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Linq;
using System.Web;

namespace HtmlTableToJArray
{
    public class HtmlTableToJArray
    {
        public JArray GetContentDynamically(string url, string tableXPath, string emptyKeyReplacement = "")
        {
            var document = new HtmlWeb().Load(url);

            var tableRows = document.DocumentNode.SelectNodes($"{tableXPath}//tr");

            var propertyNames = tableRows.First()
                .ChildNodes.Where(cn => cn.Name.StartsWith("t"))
                .Select(th => HttpUtility.HtmlDecode(th.InnerText).Trim())
                .Select(pn => string.IsNullOrEmpty(pn) ? emptyKeyReplacement : pn)
                .ToList();

            var tableData = tableRows
                .Skip(1)
                .Select(r => r.ChildNodes
                    .Where(cn => cn.Name.StartsWith("t"))
                    .ToList());

            var details = new JArray();
            foreach (var data in tableData)
            {
                if (data.Count() < propertyNames.Count)
                {
                    for (var index = 0; index < data.Count(); index++)
                    {
                        var hasColspan = Int32.TryParse(data[index].Attributes.SingleOrDefault(a => a.Name == "colspan")?.Value, out var colspanValue);
                        if (hasColspan && colspanValue > 1)
                        {
                            var htmlNode = data[index].NextSibling.NextSibling.Clone();

                            for (var colspan = 1; colspan < colspanValue; colspan++)
                            {
                                data.Insert(index + colspan, htmlNode);
                            }
                        }
                    }
                }

                var info = new JObject();
                for (var index = 0; index < propertyNames.Count; index++)
                {
                    var propertyName = propertyNames[index];
                    var content = FormatString(data[index].InnerText);

                    JProperty property;
                    if (Int32.TryParse(content.Replace(",", ""), out var number))
                    {
                        property = new JProperty(propertyName, number);
                    }
                    else
                    {
                        property = new JProperty(propertyName, content);
                    }

                    info.Add(property);
                }

                details.Add(info);
            }

            return details;
        }

        private string FormatString(string value)
        {
            return HttpUtility.HtmlDecode(value).Trim().Replace("\t", string.Empty);
        }
    }
}
