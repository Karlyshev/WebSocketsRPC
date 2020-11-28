using Newtonsoft.Json.Linq;
using System.Reflection;

namespace WebSocketsRPC 
{
    public static class Helpers
    {
        public static void SetMethodsArgs(ref object[] methodArgs, JArray jsonArgs, ParameterInfo[] parameters)
        {
            for (int i = 0; i < methodArgs.Length; i++)
            {
                try
                {
                    methodArgs[i] = i < jsonArgs.Count ? jsonArgs[i].ToObject(parameters[i].ParameterType) : (parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null);
                }
                catch
                {
                    try
                    {
                        methodArgs[i] = i < jsonArgs.Count ? JObject.Parse(jsonArgs[i].ToString()).ToObject(parameters[i].ParameterType) : (parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null);
                    }
                    catch
                    {
                        methodArgs[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : null;
                    }
                }
            }
        }
    }
}