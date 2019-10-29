using System;
using AppKit;
using Foundation;

namespace MemeGenerator
{
    [Register("ImageCanvasController")]
    public partial class ImageCanvasController : NSViewController, INSFilePromiseProviderDelegate
    { 
        private ImageCanvas imageCanvas;

        #region Constructors
        // Called when created from unmanaged code
        public ImageCanvasController(IntPtr handle) : base(handle)
        {
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public ImageCanvasController(NSCoder coder) : base(coder)
        {
        }

        // Call to load from the XIB/NIB file
        public ImageCanvasController()
        {
        }
        #endregion

        /// displays an error
        private void HandleError(NSError error)
        {
            NSOperationQueue.MainQueue.AddOperation( () => {
                NSWindow window = this.View.Window;
                PresentError(error);
            });
        }

        public void UpdateDescription(string ImageDescription, bool hidden)
        {
            placeholderLabel.Hidden = hidden;
            imageLabel.StringValue = ImageDescription;
        }

        #region - NSViewController

        [Export("awakeFromNib")]
        public override void AwakeFromNib()
        {
            imageCanvas = (ImageCanvas)View;
            base.AwakeFromNib();
            imageLabel.StringValue = "";
        }

        /// - Tag: RegisterPromiseReceiver
        public override void ViewDidLoad()
        {
            base.ViewDidLoad();

            View.RegisterForDraggedTypes(NSFilePromiseReceiver.ReadableDraggedTypes);
            View.RegisterForDraggedTypes(new string[] {NSPasteboard.NSPasteboardTypeFileUrl});
        }

        #endregion

        #region Actions

        partial void AddText(NSToolbarItem sender)
        {
            imageCanvas.AddTextField();
        }

        #endregion

        #region NSFilePromiseProviderDelegate

        /// - Tag: ProvideOperationQueue
        /// queue used for reading and writing file promises
        [Export("operationQueueForFilePromiseProvider:")]
        public NSOperationQueue GetOperationQueue(NSFilePromiseProvider filePromiseProvider)
        {
            NSOperationQueue providerQueue = new NSOperationQueue
            {
                QualityOfService = NSQualityOfService.UserInitiated
            };
            return providerQueue;
        }

        /// <summary>
        /// Not on the UI thread
        /// </summary>
        /// <param name="filePromiseProvider"></param>
        /// <param name="url"></param>
        /// <param name="completionHandler"></param>
        [Export("filePromiseProvider:writePromiseToURL:completionHandler:")]
        public void WritePromiseToUrl(NSFilePromiseProvider filePromiseProvider, NSUrl url, Action<NSError> completionHandler)
        {
            InvokeOnMainThread(() =>
            {
                if(filePromiseProvider.UserInfo is SnapshotItem snapshot)
                    snapshot.JpegRepresentation.Save(url, true);
                else
                    throw new Exception(); // TODO: just thow a file not found exception
                completionHandler(null);
            });
        }

        public string GetFileNameForDestination(NSFilePromiseProvider filePromiseProvider, string fileType)
        {
            return "WWDC18.jpg";
        }

        #endregion
    }
}
