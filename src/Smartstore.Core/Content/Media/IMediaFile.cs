﻿using Newtonsoft.Json;

namespace Smartstore.Core.Content.Media
{
    /// <summary>
    /// Represents an entity with a media file.
    /// </summary>
    public interface IMediaFile
    {
        /// <summary>
        /// Gets or sets the media identifier.
        int MediaFileId { get; set; }

        /// <summary>
        /// Gets or sets the media file.
        /// </summary>
        [JsonIgnore]
        MediaFile MediaFile { get; set; }

        /// <summary>
        /// Gets or sets the display order.
        /// </summary>
        int DisplayOrder { get; set; }
    }
}
