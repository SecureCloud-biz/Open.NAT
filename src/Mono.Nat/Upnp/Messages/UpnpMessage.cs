//
// Authors:
//   Alan McGovern alan.mcgovern@gmail.com
//
// Copyright (C) 2006 Alan McGovern
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System;
using System.Diagnostics;
using System.Xml;
using System.Net;
using System.IO;
using System.Text;
using System.Globalization;

namespace Mono.Nat.Upnp
{
    internal abstract class MessageBase
    {
        internal static readonly CultureInfo Culture = CultureInfo.InvariantCulture;
        protected UpnpNatDevice device;

        protected MessageBase(UpnpNatDevice device)
        {
            this.device = device;
        }

        protected WebRequest CreateRequest(string upnpMethod, string methodParameters, string webrequestMethod)
        {
            Uri location = new Uri("http://" + this.device.HostEndPoint.ToString() + this.device.ControlUrl);

            HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(location);
            req.KeepAlive = false;
            req.Method = webrequestMethod;
            req.ContentType = "text/xml; charset=\"utf-8\"";
            req.Headers.Add("SOAPACTION", "\"urn:schemas-upnp-org:service:WANIPConnection:1#" + upnpMethod + "\"");

            string body = "<s:Envelope "
               + "xmlns:s=\"http://schemas.xmlsoap.org/soap/envelope/\" "
               + "s:encodingStyle=\"http://schemas.xmlsoap.org/soap/encoding/\">"
               + "<s:Body>"
               + "<u:" + upnpMethod + " "
               + "xmlns:u=\"urn:schemas-upnp-org:service:WANIPConnection:1\">"
               + methodParameters
               + "</u:" + upnpMethod + ">"
               + "</s:Body>"
               + "</s:Envelope>\r\n\r\n";

            req.ContentLength = System.Text.Encoding.UTF8.GetByteCount(body);
            Stream s = req.GetRequestStream();

            s.Write(System.Text.Encoding.UTF8.GetBytes(body), 0, (int)req.ContentLength);
            return req;
        }

        public static MessageBase Decode(string message)
        {
            XmlNode node = null;
            System.Xml.XmlDocument doc = new System.Xml.XmlDocument();
            doc.LoadXml(message);

            XmlNamespaceManager nsm = new XmlNamespaceManager(doc.NameTable);

            // Error messages should be found under this namespace
            nsm.AddNamespace("errorNs", "urn:schemas-upnp-org:control-1-0");
            nsm.AddNamespace("responseNs", "urn:schemas-upnp-org:service:WANIPConnection:1");

            // Check to see if we have a fault code message.
            if ((node = doc.SelectSingleNode("//errorNs:UPnPError", nsm)) != null)
                return new ErrorMessage(Convert.ToInt32(node["errorCode"].InnerText, System.Globalization.CultureInfo.InvariantCulture),
                                                        node["errorDescription"].InnerText);

            if ((node = doc.SelectSingleNode("//responseNs:AddPortMappingResponse", nsm)) != null)
                return new CreatePortMappingResponseMessage();

            if ((node = doc.SelectSingleNode("//responseNs:DeletePortMappingResponse", nsm)) != null)
                return new DeletePortMapResponseMessage();

            if ((node = doc.SelectSingleNode("//responseNs:GetExternalIPAddressResponse", nsm)) != null)
                return new GetExternalIPAddressResponseMessage(node["NewExternalIPAddress"].InnerText);

            if ((node = doc.SelectSingleNode("//responseNs:GetGenericPortMappingEntryResponse", nsm)) != null)
                return new GetGenericPortMappingEntryResponseMessage(node, true);

            if ((node = doc.SelectSingleNode("//responseNs:GetSpecificPortMappingEntryResponse", nsm)) != null)
                return new GetGenericPortMappingEntryResponseMessage(node, false);

            Console.WriteLine("Unknown message returned. Please send me back the following XML:");
            Console.WriteLine(message);
            return null;
        }

        public abstract WebRequest Encode();

        internal static void WriteFullElement(XmlWriter writer, string element, string value)
        {
            writer.WriteStartElement(element);
            writer.WriteString(value);
            writer.WriteEndElement();
        }

        internal static XmlWriter CreateWriter(StringBuilder sb)
        {
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.ConformanceLevel = ConformanceLevel.Fragment;
            return XmlWriter.Create(sb, settings);
        }
    }
}