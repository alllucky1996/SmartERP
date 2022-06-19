﻿using System.ServiceModel.Syndication;
using System.Xml;
using System.Xml.Linq;
using Smartstore.IO;

namespace Smartstore.Net
{
    public class SmartSyndicationFeed : SyndicationFeed
    {
        public SmartSyndicationFeed(Uri feedAlternateLink, string title, string description = null)
            : base(title, description ?? title, feedAlternateLink, null, DateTime.UtcNow)
        {
        }

        public static string UrlAtom => "http://www.w3.org/2005/Atom";
        public static string UrlPurlContent => "http://purl.org/rss/1.0/modules/content/";

        public void AddNamespaces(bool purlContent)
        {
            AttributeExtensions.Add(new XmlQualifiedName("atom", XNamespace.Xmlns.NamespaceName), UrlAtom);

            if (purlContent)
            {
                AttributeExtensions.Add(new XmlQualifiedName("content", XNamespace.Xmlns.ToString()), UrlPurlContent);
            }
        }

        public void Init(string selfLink, string languageCulture = null)
        {
            ElementExtensions.Add(
                new XElement(((XNamespace)UrlAtom) + "link",
                    new XAttribute("href", selfLink),
                    new XAttribute("rel", "self"),
                    new XAttribute("type", "application/rss+xml")));

            if (languageCulture.HasValue())
            {
                Language = languageCulture;
            }
        }

        public SyndicationItem CreateItem(string title, string synopsis, string url, DateTimeOffset published, string contentEncoded = null)
        {
            var item = new SyndicationItem(
                title.RemoveInvalidXmlChars().EmptyNull(),
                synopsis.RemoveInvalidXmlChars().EmptyNull(),
                new Uri(url),
                url,
                published);

            if (contentEncoded != null)
            {
                item.ElementExtensions.Add("encoded", UrlPurlContent, contentEncoded.RemoveInvalidXmlChars().EmptyNull());
            }

            return item;
        }

        public bool AddEnclosure(SyndicationItem item, IFile file, string fileUrl)
        {
            Guard.NotNull(item, nameof(item));
            Guard.NotNull(file, nameof(file));

            if (file != null && fileUrl.HasValue())
            {
                // We do not have the size behind fileUrl but the original file size should be fine.
                long fileLength = file.Length;
                if (fileLength <= 0)
                {
                    // 0 omits the length attribute but that invalidates the feed!
                    fileLength = 10000;
                }

                var enclosure = SyndicationLink.CreateMediaEnclosureLink(
                    new Uri(fileUrl),
                    MimeTypes.MapNameToMimeType(file.Name).EmptyNull(),
                    fileLength);

                item.Links.Add(enclosure);

                return true;
            }

            return false;
        }
    }
}