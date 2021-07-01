using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BAS.Nop.Plugin.Misc.HybridCache
{
    public class CustomResolver : DefaultContractResolver
    {
        private readonly List<string> _namesOfVirtualPropsToKeep = new List<string>(new String[] { });

        public CustomResolver() { }

        public CustomResolver(IEnumerable<string> namesOfVirtualPropsToKeep)
        {
            this._namesOfVirtualPropsToKeep = namesOfVirtualPropsToKeep.Select(x => x.ToLower()).ToList();
        }

        protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
        {
            JsonProperty prop = base.CreateProperty(member, memberSerialization);
            var propInfo = member as PropertyInfo;
            if (propInfo != null && propInfo.GetMethod.ReturnType != typeof(string) && propInfo.GetMethod.ReturnType.IsClass && propInfo.GetMethod.IsVirtual && !propInfo.GetMethod.IsFinal
                    && !_namesOfVirtualPropsToKeep.Contains(propInfo.Name.ToLower()))
            {
                prop.ShouldSerialize = obj => false;
            }
            return prop;
        }
    }
}
