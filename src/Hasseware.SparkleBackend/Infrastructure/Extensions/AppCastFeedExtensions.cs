using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml.Linq;

namespace Hasseware.SparkleService
{
    internal static class AppCastFeedExtensions
    {
        static readonly XNamespace SparkleNamespace = XNamespace.Get("http://www.andymatuschak.org/xml-namespaces/sparkle");
        static readonly XNamespace DcNamespace = XNamespace.Get("http://purl.org/dc/elements/1.1/");

        public static void Load(this Models.AppCastFeed @this, Stream stream)
        {
            var xdoc = XDocument.Load(stream);
            @this.Clear();

            foreach (var xitem in xdoc.Root.Element("channel").Elements())
            {
                switch (xitem.Name.LocalName)
                {
                    case "title":
                        @this.Title = xitem.Value;
                        break;
                    case "link":
                        @this.Link = xitem.Value;
                        break;
                    case "description":
                        @this.Description = xitem.Value;
                        break;
                    case "language":
                        @this.Language = CultureInfo.GetCultureInfo(xitem.Value);
                        break;
                    case "item":
                        var item = new Models.AppCastItem();
                        RetrieveItem(item, xitem);
                        @this.Add(item);
                        break;
                }
            }
        }

        public static void Save(this Models.AppCastFeed @this, Stream stream)
        {
            var xdoc = new XDocument(new XDeclaration("1.0", Encoding.UTF8.WebName, "yes"),
                new XElement("rss", new XAttribute("version", "2.0"),
                    new XAttribute(XNamespace.Xmlns.GetName("sparkle"), SparkleNamespace),
                    new XAttribute(XNamespace.Xmlns.GetName("dc"), DcNamespace)));

            var xchannel = new XElement("channel", new XElement("title", @this.Title), new XElement("link", @this.Link),
                new XElement("description", @this.Description), new XElement("language", @this.Language));
            xdoc.Root.Add(xchannel);

            foreach (var item in @this)
            {
                StoreItem(item, xchannel);
            }
            xdoc.Save(stream);
        }

        private static void RetrieveItem(Models.AppCastItem item, XElement root)
        {
            foreach (var xchild in root.Elements())
            {
                switch (xchild.Name.LocalName)
                {
                    case "title":
                        item.Title = xchild.Value;
                        break;
                    case "releaseNotesLink":
                        item.NotesLink = xchild.Value;
                        break;
                    case "pubDate":
                        item.Published = DateTime.Parse(xchild.Value);
                        break;
                    case "enclosure":
                        item.Enclosure = new Models.AppCastEnclosure();
                        RetrieveEnclosure(item.Enclosure, xchild);
                        break;
                    case "deltas":
                        foreach (var xdelta in root.Elements())
                        {
                            var delta = new Models.AppCastDelta();
                            RetrieveEnclosure(delta, xdelta);
                            item.Add(delta);
                        }
                        break;
                }
            }
        }

        private static void RetrieveEnclosure(Models.AppCastEnclosure item, XElement root)
        {
            foreach (var attrib in root.Attributes())
            {
                switch (attrib.Name.LocalName)
                {
                    case "version":
                        item.Version = Version.Parse(attrib.Value);
                        break;
                    case "url":
                        item.ContentLink = attrib.Value;
                        break;
                    case "type":
                        item.ContentType = attrib.Value;
                        break;
                    case "length":
                        if (attrib.Value != null)
                            item.ContentLength = Int32.Parse(attrib.Value);
                        break;
                    case "dsaSignature":
                        item.Signature = attrib.Value;
                        break;
                    case "deltaFrom":
                        if (item is Models.AppCastDelta)
                            ((Models.AppCastDelta)item).DeltaFrom = Version.Parse(attrib.Value);
                        break;
                }
            }
        }

        private static void StoreItem(Models.AppCastItem item, XElement root)
        {
            var xitem = new XElement("item", new XElement("title", item.Title),
                new XElement(SparkleNamespace.GetName("releaseNotesLink"), item.NotesLink),
                new XElement("pubDate", item.Published));
            root.Add(xitem);

            StoreEnclosure(item.Enclosure, xitem);
        }

        private static void StoreEnclosure(Models.AppCastEnclosure item, XElement root)
        {
            var xenclosure = new XElement("enclosure",
                new XAttribute(SparkleNamespace.GetName("version"), item.Version));
            root.Add(xenclosure);

            if (item is Models.AppCastDelta)
            {
                xenclosure.Add(new XAttribute(SparkleNamespace.GetName("deltaFrom"),
                    ((Models.AppCastDelta)item).DeltaFrom));
            }
            if (item.ContentLink != null)
                xenclosure.Add(new XAttribute("url", item.ContentLink));
            if (item.ContentType != null)
                xenclosure.Add(new XAttribute("type", item.ContentType));
            if (item.ContentType != null)
                xenclosure.Add(new XAttribute("length", item.ContentLength));
            if (item.Signature != null)
                xenclosure.Add(new XAttribute(SparkleNamespace.GetName("dsaSignature"), item.Signature));
        }
    }
}