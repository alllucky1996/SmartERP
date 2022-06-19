﻿using Smartstore.Core.Content.Media.Imaging;
using Smartstore.Core.Localization;
using Smartstore.Threading;

namespace Smartstore.Core.Content.Media
{
    public abstract class ImageHandlerBase : IMediaHandler
    {
        protected ImageHandlerBase(IImageCache imageCache, MediaExceptionFactory exceptionFactory)
        {
            ImageCache = imageCache;
            ExceptionFactory = exceptionFactory;
        }

        public ILogger Logger { get; set; } = NullLogger.Instance;
        public IImageCache ImageCache { get; set; }
        public MediaExceptionFactory ExceptionFactory { get; set; }
        public Localizer T { get; set; } = NullLocalizer.Instance;

        public virtual int Order => -100;

        public async Task ExecuteAsync(MediaHandlerContext context)
        {
            if (!IsProcessable(context))
            {
                return;
            }

            var query = context.ImageQuery;
            var pathData = context.PathData;

            var cachedImage = await ImageCache.GetAsync(context.MediaFileId, pathData, query);

            if (!pathData.Extension.EqualsNoCase(cachedImage.Extension))
            {
                // The query requests another format. 
                // Adjust extension and mime type fo proper ETag creation.
                pathData.Extension = cachedImage.Extension;
                pathData.MimeType = cachedImage.MimeType;
            }

            var exists = cachedImage.Exists;

            if (exists && cachedImage.FileSize == 0)
            {
                // Empty file means: thumb extraction failed before and will most likely fail again.
                // Don't bother proceeding.
                context.Exception = ExceptionFactory.ExtractThumbnail(cachedImage.FileName);
                context.Executed = true;
                return;
            }

            if (!exists)
            {
                // Lock concurrent requests to same resource
                using (await AsyncLock.KeyedAsync("ImageHandlerBase.Execute." + cachedImage.Path))
                {
                    await ImageCache.RefreshInfoAsync(cachedImage);

                    // File could have been processed by another request in the meantime, check again.
                    if (!cachedImage.Exists)
                    {
                        // Call inner function
                        var sourceFile = await context.GetSourceFileAsync();
                        if (sourceFile == null || sourceFile.Length == 0)
                        {
                            context.Executed = true;
                            return;
                        }

                        var inputStream = await sourceFile.OpenReadAsync();
                        if (inputStream == null)
                        {
                            context.Exception = ExceptionFactory.ExtractThumbnail(sourceFile.SubPath, T("Admin.Media.Exception.NullInputStream"));
                            context.Executed = true;
                            return;
                        }

                        try
                        {
                            await ProcessImageAsync(context, cachedImage, inputStream);
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex);

                            if (ex is ExtractThumbnailException)
                            {
                                // Thumbnail extraction failed and we must assume that it always will fail.
                                // Therefore we create an empty file to prevent repetitive processing.
                                using (var memStream = new MemoryStream())
                                {
                                    await ImageCache.PutAsync(cachedImage, memStream);
                                }
                            }

                            context.Exception = ex;
                            context.Executed = true;
                            return;
                        }

                        if (context.ResultImage != null)
                        {
                            await ImageCache.PutAsync(cachedImage, context.ResultImage);
                            context.ResultFile = cachedImage.File;
                        }

                        context.Executed = true;
                        return;

                    }
                }
            }

            // Cached image existed already
            context.ResultFile = cachedImage.File;
            context.Executed = true;
        }

        protected abstract bool IsProcessable(MediaHandlerContext context);

        /// <summary>
        /// The handler implementation. <see cref="inputStream"/> should be closed by implementor.
        /// </summary>
        protected abstract Task ProcessImageAsync(
            MediaHandlerContext context, 
            CachedImage cachedImage, 
            Stream inputStream);
    }
}
