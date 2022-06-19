﻿using Smartstore.IO;

namespace Smartstore.Net.Mail
{
    public enum TransferEncoding
    {
        Default = 0,
        SevenBit = 1,
        EightBit = 2,
        Binary = 3,
        Base64 = 4,
        QuotedPrintable = 5,
        UUEncode = 6
    }

    public class MailAttachment : Disposable
    {
        public MailAttachment(IFile file)
        {
            Guard.NotNull(file, nameof(file));

            ContentStream = file.OpenRead();
            Name = file.Name;
            ContentType = MimeTypes.MapNameToMimeType(Name);
            ModificationDate = file.LastModified;
            ReadDate = file.LastModified;
        }

        public MailAttachment(FileInfo file)
        {
            Guard.NotNull(file, nameof(file));

            ContentStream = file.OpenRead();
            Name = file.Name;
            ContentType = MimeTypes.MapNameToMimeType(Name);
            CreationDate = file.CreationTimeUtc;
            ModificationDate = file.LastWriteTimeUtc;
            ReadDate = file.LastAccessTimeUtc;
        }

        public MailAttachment(Stream contentStream, string name, string contentType = null)
        {
            Guard.NotNull(contentStream, nameof(contentStream));

            ContentStream = contentStream;
            Name = name;

            if (contentType is null && name.HasValue())
            {
                ContentType = MimeTypes.MapNameToMimeType(name);
            }
        }

        public Stream ContentStream { get; init; }
        public string Name { get; init; }
        public string ContentType { get; init; }

        public TransferEncoding TransferEncoding { get; set; } = TransferEncoding.Default;
        public DateTimeOffset? CreationDate { get; set; }
        public DateTimeOffset? ModificationDate { get; set; }
        public DateTimeOffset? ReadDate { get; set; }

        protected override void OnDispose(bool disposing)
        {
            if (disposing)
            {
                ContentStream?.Close();
            }
        }
    }
}