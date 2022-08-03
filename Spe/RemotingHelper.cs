using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Management.Automation;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Xml;

namespace Spe
{
    internal class RemotingHelper
    {
        private static List<PSObject> DeserializeXml(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return new List<PSObject>();
            }

            var results = new List<PSObject>();

            const BindingFlags commonBindings = BindingFlags.NonPublic | BindingFlags.Instance;
            const BindingFlags methodBindings = BindingFlags.InvokeMethod | commonBindings;
            var type = typeof(PSObject).Assembly.GetType("System.Management.Automation.Deserializer");
            var ctor = type.GetConstructor(commonBindings, null, new[] { typeof(XmlReader) }, null);

            using (var sr = new StringReader(data))
            {
                using (var xr = new XmlTextReader(sr))
                {
                    var deserializer = ctor.Invoke(new object[] { xr });
                    while (!(bool)type.InvokeMember("Done", methodBindings, null, deserializer, new object[] { }))
                    {
                        PSObject deserializedData = null;
                        try
                        {
                            deserializedData = (PSObject)type.InvokeMember("Deserialize", methodBindings, null, deserializer, Array.Empty<object>());
                            results.Add(deserializedData);
                        }
                        catch (Exception ex)
                        {
                            throw;
                            //WriteWarning("Could not deserialize string. Exception: " + ex.Message);
                        }
                    }
                }
            }            

            return results;
        }

        private static string SerializePSObject(PSObject data)
        {
            if (data == null) return null;

            const BindingFlags commonBindings = BindingFlags.NonPublic | BindingFlags.Instance;

            var builder = new StringBuilder();
            var settings = new XmlWriterSettings
            {
                CloseOutput = true,
                Encoding = Encoding.UTF8,
                Indent = false,
                OmitXmlDeclaration = true
            };
            var xmlWriter = XmlWriter.Create(builder, settings);
            var type = typeof(PSObject).Assembly.GetType("System.Management.Automation.Serializer");
            var ctor = type.GetConstructor(commonBindings, null, new[] { typeof(XmlWriter) }, null);
            var serializer = ctor.Invoke(new object[] { xmlWriter });
            var method = type.GetMethod("Serialize", commonBindings, null, new[] { typeof(object) }, null);
            var done = type.GetMethod("Done", commonBindings);

            method.Invoke(serializer, new object[] { data });

            done.Invoke(serializer, new object[] { });            
            xmlWriter.Close();

            return builder.ToString();
        }

        public static Collection<PSObject> InvokeScript(string script, PSObject arguments, bool isRaw = false)
        {
            var results = new Collection<PSObject>();

            var handler = new HttpClientHandler
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            };

            var client = new HttpClient(handler);
            var authBytes = System.Text.Encoding.GetEncoding("iso-8859-1").GetBytes(@"sitecore\admin:b");
            client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(authBytes));

            var uri = new Uri("https://spe.dev.local");
            var sessionId = Guid.NewGuid();
            var persistentSession = false;
            var serviceUrl = "/-/script/script/?";
            serviceUrl += "sessionId=" + sessionId + "&rawOutput=" + isRaw + "&persistentSession=" + persistentSession;

            var url = uri.AbsoluteUri.TrimEnd('/') + serviceUrl;
            var localParams = SerializePSObject(arguments);

            var body = $"{script}<#{sessionId}#>{localParams}";

            var messageBytes = System.Text.Encoding.UTF8.GetBytes(body);
            using (var ms = new MemoryStream())
            using (var gzip = new System.IO.Compression.GZipStream(ms, System.IO.Compression.CompressionLevel.Fastest, true))
            {
                gzip.Write(messageBytes, 0, messageBytes.Length);
                gzip.Close();
                ms.Position = 0;
                var content = new ByteArrayContent(ms.ToArray());
                ms.Close();
                content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
                content.Headers.ContentEncoding.Add("gzip");

                var response = client.PostAsync(url, content);
                var taskResult = response.GetAwaiter().GetResult();
                taskResult.EnsureSuccessStatusCode();
                results.Add(taskResult.Content.ReadAsStringAsync().GetAwaiter().GetResult());
            }

            return results;
        }

        public static List<PSObject> InvokeAndParse(string script, PSObject arguments = null)
        {
            var results = new List<PSObject>();

            var records = InvokeScript(script, arguments, false);
            foreach (var record in records)
            {
                if (record.ImmediateBaseObject is string && string.IsNullOrEmpty(record.ToString())) continue;

                var items = DeserializeXml(record.ToString())[0].ImmediateBaseObject as ArrayList;
                foreach (var item in items)
                {
                    if (item is null) continue;
                    if (item is PSObject)
                    {
                        results.Add((PSObject)item);
                    }
                    else
                    {
                        results.Add(new PSObject(item));
                    }
                }
            }

            return results;
        }
    }
}
