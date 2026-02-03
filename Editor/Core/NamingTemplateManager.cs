using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace UnityTools.ObjectGrouper.Core
{
    public static class NamingTemplateManager
    {
        public static string GenerateName(string template, int count, GameObject reference = null)
        {
            string name = template;
            
            // Basic variables
            name = name.Replace("{count}", count.ToString("D2"));
            name = name.Replace("{date}", DateTime.Now.ToString("yyyyMMdd"));
            name = name.Replace("{time}", DateTime.Now.ToString("HHmmss"));

            if (reference != null)
            {
                name = name.Replace("{type}", reference.GetType().Name);
                name = name.Replace("{tag}", reference.tag);
                name = name.Replace("{scene}", reference.scene.name);
                name = name.Replace("{name}", reference.name);
            }
            else
            {
                name = name.Replace("{type}", "Object");
                name = name.Replace("{tag}", "Untagged");
                name = name.Replace("{scene}", "Global");
                name = name.Replace("{name}", "Group");
            }

            return SanitizeName(name);
        }

        public static string SanitizeName(string name)
        {
            // Remove invalid characters for Unity naming / general OS safety
            return Regex.Replace(name, @"[^\w\-. ]", "_");
        }
    }
}
