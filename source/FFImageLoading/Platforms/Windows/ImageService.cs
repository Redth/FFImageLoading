﻿using System;
using System.IO;
using FFImageLoading.Config;
using FFImageLoading.Cache;
using FFImageLoading.Helpers;
using FFImageLoading.Work;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using FFImageLoading.DataResolvers;
using System.Linq;
using System.Runtime.CompilerServices;

namespace FFImageLoading
{
    /// <summary>
    /// FFImageLoading by Daniel Luberda
    /// </summary>
    [Preserve(AllMembers = true)]
    public class ImageService : ImageServiceBase<BitmapSource>
    {
		public ImageService(
			IConfiguration configuration,
			ImageCache imageCache,
			IMD5Helper md5Helper,
			IMiniLogger miniLogger,
			IPlatformPerformance platformPerformance,
			IMainThreadDispatcher mainThreadDispatcher,
			IDataResolverFactory dataResolverFactory)
			: base(configuration, md5Helper, miniLogger, platformPerformance, mainThreadDispatcher, dataResolverFactory)
		{
			ImageCache = imageCache;
		}

		static ConditionalWeakTable<object, IImageLoaderTask> _viewsReferences = new ConditionalWeakTable<object, IImageLoaderTask>();


		protected readonly ImageCache ImageCache;

        protected override void PlatformSpecificConfiguration(Config.Configuration configuration)
        {
            base.PlatformSpecificConfiguration(configuration);

            configuration.ClearMemoryCacheOnOutOfMemory = false;
            configuration.ExecuteCallbacksOnUIThread = true;
        }

        protected override IMemoryCache<BitmapSource> MemoryCache => this.ImageCache;

        public IImageLoaderTask CreateTask<TImageView>(TaskParameter parameters, ITarget<BitmapSource, TImageView> target) where TImageView : class
			=> new PlatformImageLoaderTask<TImageView>(this, MemoryCache, target, parameters);

		public override IImageLoaderTask CreateTask(TaskParameter parameters)
			=> new PlatformImageLoaderTask<object>(this, MemoryCache, null, parameters);

        protected override void SetTaskForTarget(IImageLoaderTask currentTask)
        {
            var targetView = currentTask?.Target?.TargetControl;

            if (!(targetView is Microsoft.UI.Xaml.Controls.Image))
                return;

            lock (_viewsReferences)
            {
                if (_viewsReferences.TryGetValue(targetView, out var existingTask))
                {
                    try
                    {
                        if (existingTask != null && !existingTask.IsCancelled && !existingTask.IsCompleted)
                        {
                            existingTask.Cancel();
                        }
                    }
                    catch (ObjectDisposedException) { }

                    _viewsReferences.Remove(targetView);
                }

                _viewsReferences.Add(targetView, currentTask);
            }
        }

        public override void CancelWorkForView(object view)
        {
            lock (_viewsReferences)
            {
                if (_viewsReferences.TryGetValue(view, out var existingTask))
                {
                    try
                    {
                        if (existingTask != null && !existingTask.IsCancelled && !existingTask.IsCompleted)
                        {
                            existingTask.Cancel();
                        }
                    }
                    catch (ObjectDisposedException) { }
                }
            }
        }

        public override int DpToPixels(double dp)
        {
            return (int)Math.Floor(dp * ScaleHelper.Scale);
        }

        public override double PixelsToDp(double px)
        {
            if (Math.Abs(px) < double.Epsilon)
                return 0d;

            return Math.Floor(px / ScaleHelper.Scale);
        }
    }
}
