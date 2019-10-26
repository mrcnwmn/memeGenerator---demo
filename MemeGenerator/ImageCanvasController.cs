using System;
using System.Linq;
using AppKit;
using Foundation;

namespace MemeGenerator
{
    [Register("ImageCanvasController")]
    public partial class ImageCanvasController : NSViewController, INSFilePromiseProviderDelegate, ImageCanvasDelegate
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

        public string GetFileNameForDestination(NSFilePromiseProvider filePromiseProvider, string fileType)
        {
            return "WWDC18.jpg";
        }

        /// queue used for reading and writing file promises
        private NSOperationQueue workQueue
        {
            get
            {
                NSOperationQueue providerQueue = new NSOperationQueue();
                providerQueue.QualityOfService = NSQualityOfService.UserInitiated;
                return providerQueue;
            }
        }

        /// directory URL used for accepting file promises
        private NSUrl destinationURL
        {
            get
            {
                NSFileManager fm = new NSFileManager();
                NSUrl destURL = fm.GetTemporaryDirectory();
                destURL = destURL.Append("Drops",true);
                NSFileManager.DefaultManager.CreateDirectory(destURL.Path, true, null);
                return destURL;
            }
        }

        /// updates the canvas with a given image
        private void handleImage(NSImage image)
        {
            imageCanvas.image = image;
            placeholderLabel.Hidden = (image != null);
            imageLabel.StringValue = imageCanvas.imageDescription;
        }

        /// updates the canvas with a given image file
        private void handleFile(NSUrl url)
        {
            NSImage image = new NSImage(url);
            NSOperationQueue.MainQueue.AddOperation(() => {
                this.handleImage(image);
            });
        }

        /// displays an error
        private void handleError(NSError error)
        {
            NSOperationQueue.MainQueue.AddOperation( () => {
                NSWindow window = this.View.Window;
                PresentError(error);
            });
        }

        /// displays a progress indicator
        private void prepareForUpdate()
        {
            imageCanvas.isLoading = true;
            placeholderLabel.Hidden = true;
        }

        // MARK: - NSViewController

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

        // MARK: - Actions

        //    @IBAction func addText(_ sender: AnyObject)
        //    {
        //        imageCanvas.addTextField()
        //    }

        // MARK: - ImageCanvasDelegate

        public NSDragOperation draggingEntered(ImageCanvas imageCanvas, NSDraggingInfo sender)
        {
            // TODO: Check that this works. Has to change behavior for C#
            if(sender.DraggingSourceOperationMask.HasFlag(NSDragOperation.Copy))
                return NSDragOperation.Copy;
            //return sender.DraggingSourceOperationMask.intersection([.copy])
            return new NSDragOperation();
        }

        public bool performDragOperation(ImageCanvas imageCanvas, NSDraggingInfo sender)
        {
            //Type[] supportedClasses = { typeof(NSFilePromiseReceiver), typeof(NSUrl) };
            //NSDictionary searchOptions = new NSDictionary("urlReadingContentsConformToTypes", true,
            //                                        "NSPasteboardURLReadingContentsConformToTypesKey", MobileCoreServices.UTType.Image);

            NSPasteboard pasteBoard = sender.GetDraggingPasteboard();
            foreach(NSPasteboardItem pbitem in pasteBoard.PasteboardItems)
            {
                if(pbitem.Types.Contains(NSPasteboard.NSPasteboardTypeFileUrl))
                {
                    handleFile(NSUrl.FromString(pbitem.GetStringForType(NSPasteboard.NSPasteboardTypeFileUrl)));
                }
            }

            //NSFilePromiseReceiver hi = new NSFilePromiseReceiver();
            //GCHandle handle1 = GCHandle.Alloc(hi);
            //IntPtr supportedClasses = (IntPtr)handle1;
            //NSDictionary searchOptions = new NSDictionary<NSPasteboard, NSObject>();

            ///// - Tag: HandleFilePromises
            //sender.EnumerateDraggingItems(
            //NSDraggingItemEnumerationOptions.Concurrent,
            //View,
            //supportedClasses,
            //searchOptions,
            //(NSDraggingItem draggingItem, nint idx, ref bool stop) =>
            //{
            //    switch(draggingItem.Item.GetType().ToString())
            //    {
            //        case "NSFilePromiseReceiver":
            //            NSFilePromiseReceiver filePromiseReceiver = (NSFilePromiseReceiver)draggingItem.Item;
            //            this.prepareForUpdate();
            //            filePromiseReceiver.ReceivePromisedFiles(
            //                    this.destinationURL,
            //                    new NSDictionary(),
            //                    workQueue,
            //                    (NSUrl fileURL, NSError error) =>
            //                    {
            //                        if(error != null)
            //                            handleError(error);
            //                        else
            //                            handleFile(fileURL);
            //                    });
            //            break;
            //        case "NSUrl":
            //            handleFile((NSUrl)draggingItem.Item);
            //            break;
            //        default: break;
            //    }
            //});
            return true;
        }

        public NSFilePromiseProvider pasteboardWriter(ImageCanvas imageCanvas)
        {
            NSFilePromiseProvider provider = new NSFilePromiseProvider(MobileCoreServices.UTType.JPEG, this);
            provider.UserInfo = imageCanvas.snapshotItem;
            return provider;
        }

        // MARK: - NSFilePromiseProviderDelegate

        /// - Tag: ProvideFileName
        //public String filePromiseProvider(NSFilePromiseProvider filePromiseProvider, String fileType)
        //{
        //    return "WWDC18.jpg";
        //}

        /// - Tag: ProvideOperationQueue
        [Export("operationQueueForFilePromiseProvider:")]
        public NSOperationQueue GetOperationQueue(NSFilePromiseProvider filePromiseProvider)
        {
            return workQueue;
        }
        
        /// - Tag: PerformFileWriting
        ///
        //public void filePromiseProvider(NSFilePromiseProvider filePromiseProvider, NSUrl url, Action<NSError> completionHandler)
        //{
        //    ImageCanvas.SnapshotItem snapshot = filePromiseProvider.UserInfo as ImageCanvas.SnapshotItem;
        //    if(snapshot != null)
        //        snapshot.jpegRepresentation.Save(url, true);
        //    else
        //        throw new Exception(); // TODO: just thow a file not found exception
        //    completionHandler(null);
        //}

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
                ImageCanvas.SnapshotItem snapshot = filePromiseProvider.UserInfo as ImageCanvas.SnapshotItem;
                if(snapshot != null)
                    snapshot.jpegRepresentation.Save(url, true);
                else
                    throw new Exception(); // TODO: just thow a file not found exception
                completionHandler(null);
            });
        }
    }
}
