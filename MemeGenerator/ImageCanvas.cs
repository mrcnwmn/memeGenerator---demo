using System;
using System.Collections.Generic;
using System.Linq;
using AppKit;
using CoreGraphics;
using Foundation;

namespace MemeGenerator
{
    /// Delegate for handling dragging events
    //[Protocol(Name = "ImageCanvasDelegate")] //, WrapperType = typeof(NSTextFieldDelegateWrapper))]
    //public interface ImageCanvasDelegate
    //{ 
    //    NSDragOperation draggingEntered(ImageCanvas imageCanvas, NSDraggingInfo sender);
    //    bool performDragOperation(ImageCanvas imageCanvas, NSDraggingInfo sender);
    //    NSFilePromiseProvider pasteboardWriter(ImageCanvas imageCanvas);
    //}

    [Register("ImageCanvas")]
    public partial class ImageCanvas : NSView, INSTextFieldDelegate, INSDraggingSource
    {
        private readonly float dragThreshold = 3.0f;
        private readonly List<TextField> textFields = new List<TextField>();
        private CGPoint dragOriginOffset = CGPoint.Empty;
        private CGSize imagePixelSize = CGSize.Empty;
        private NSView overlay;

        private bool highlighted;
        private bool loading;

        #region Constructors
        // Called when created from unmanaged code
        public ImageCanvas(IntPtr handle) : base(handle)
        {
        }

        // Called when created directly from a XIB file
        [Export("initWithCoder:")]
        public ImageCanvas(NSCoder coder) : base(coder)
        {
        }

        // Call to load from the XIB/NIB file
        public ImageCanvas()
        {
        }
        #endregion


        /// Used to represent the content of the canvas and render a flattened image
        public class SnapshotItem : NSObject
        {
            NSImage baseImage;
            CGSize pixelSize;
            TextField.DrawingItem drawingItems;
            nfloat drawingScale;

            public SnapshotItem(NSImage baseImage, CGSize pixelSize, TextField.DrawingItem drawingItems, nfloat drawingScale)
            {
                this.baseImage = baseImage;
                this.pixelSize = pixelSize;
                this.drawingItems = drawingItems;
                this.drawingScale = drawingScale;
            }

            private bool DrawingHandler(CGRect dstRect)
            {
                baseImage.Draw(dstRect);
                NSAffineTransform transform = new NSAffineTransform();
                transform.Scale(this.drawingScale);
                transform.Concat();
                drawingItems.draw();
                return true;
            }

            public NSData JpegRepresentation
            {
                get
                {
                    NSImage outputImage = NSImage.ImageWithSize(pixelSize, false, DrawingHandler);
                    NSData tiffData = outputImage.AsTiff();
                    NSBitmapImageRep bitmapImageRep = new NSBitmapImageRep(tiffData);
                    return bitmapImageRep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Jpeg, new NSDictionary());
                }
            }
        }

        private bool IsHighlighted
        {
            get => highlighted;
            set
            {
                highlighted = value;
                NeedsDisplay = true;
            }
        }

        public bool isLoading
        {
            get => loading;
            set
            {
                loading = value;
                imageView.Enabled = !loading;
                progressIndicator.Hidden = !loading;
                if (loading)
                    progressIndicator.StartAnimation(null);
                else
                    progressIndicator.StopAnimation(null);
            }
        }

        public NSImage Image
        {
            set
            {
                imageView.Image = value;
                NSImageRep[] imageReps = value.Representations();
                if(imageReps.Length > 0)
                    imagePixelSize = new CGSize(imageReps[0].PixelsWide, imageReps[0].PixelsHigh);
                isLoading = false;
                NeedsLayout = true;
            }
            get
            {
                if(imageView == null)
                    return null;
                return imageView.Image;
            }
        }

        public string ImageDescription
        {
            get
            {
                return (Image != null) ? ((int)imagePixelSize.Width).ToString() + " × " + ((int)imagePixelSize.Height).ToString() : "...";
            }
        }

        private NSImage DraggingImage
        {
            get
            {
                CGRect targetRect = overlay.Frame;
                Image = new NSImage(targetRect.Size);
                if(BitmapImageRepForCachingDisplayInRect(targetRect) is NSBitmapImageRep imageRep)
                {
                    CacheDisplay(targetRect, imageRep);
                    Image.AddRepresentation(imageRep);
                }
                return Image;
            }
        }

        public SnapshotItem snapshotItem
        {
            get
            {
                NSImage newimage = this.Image;
                if(newimage == null) return null;
                TextField.DrawingItem drawingItems = (textFields.Count>0)? textFields[0].drawingItem() : new TextField.DrawingItem();
                nfloat drawingScale = imagePixelSize.Width / overlay.Frame.Width;

                return new SnapshotItem(Image, imagePixelSize, drawingItems, drawingScale);
            }
        }

        [Export("awakeFromNib")]
        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            imageView.UnregisterDraggedTypes();
            progressIndicator.Hidden = true; // explicitly hiding the indicator in order not to catch mouse events
            overlay = new NSView();
            AddSubview(overlay);
        }

        public void AddTextField()
        {
            TextField textField = new TextField();
            textFields.Add(textField);
            overlay.AddSubview(textField);
            textField.Delegate = this;
            textField.centerInSuperview();

            textField.Window?.MakeFirstResponder(this);
        }

        partial void delete(NSMenuItem sender)
        {
            //textField = selectedTextField;
            //index = textFields.index(textField);
            //if(textField != null && index != null)
            //{
            //    textFields.remove(index);
            //    selectedTextField?.removeFromSuperview();
            //    selectedTextField = null;
            //}
        }

        private CGRect RectForDrawingImage(CGSize imageSize, NSImageScale scaling)
        {
            CGRect drawingRect = new CGRect(CGPoint.Empty, imageSize);
            CGRect containerRect = Bounds;

            if(imageSize.Width > 0 && imageSize.Height > 0)
            {
                return drawingRect;
            }

            CGSize scaledSizeToFitFrame()
            {
                CGSize scaledSize = CGSize.Empty;
                nfloat horizontalScale = containerRect.Width / imageSize.Width;
                nfloat verticalScale = containerRect.Height / imageSize.Height;
                float minimumScale = Math.Min((float)horizontalScale, (float)verticalScale);
                scaledSize.Width = imageSize.Width * minimumScale;
                scaledSize.Height = imageSize.Height * minimumScale;
                return scaledSize;
            }

            switch (scaling)
            {
                case NSImageScale.ProportionallyDown:
                    if (imageSize.Width > containerRect.Width || imageSize.Height > containerRect.Height)
                        drawingRect.Size = scaledSizeToFitFrame();
                    break;
                case NSImageScale.AxesIndependently:
                    drawingRect.Size = containerRect.Size;
                        break;
                case NSImageScale.ProportionallyUpOrDown:
                    if(imageSize.Width > 0.0 && imageSize.Height > 0.0)
                        drawingRect.Size = scaledSizeToFitFrame();
                    break;
                case NSImageScale.None:
                    break;
            }

            drawingRect.X = containerRect.GetMinX() + (containerRect.Width - drawingRect.Width) * 0.5f;
            drawingRect.Y = containerRect.GetMinY() + (containerRect.Height - drawingRect.Height) * 0.5f;

            return drawingRect;
        }

        private CGRect ConstrainRectCenterToBounds(CGRect rect) {
            CGRect result = rect;
            CGPoint center = new CGPoint(rect.GetMidX(), rect.GetMidY());
            if (center.X < 0.0)
                result.X = -(rect.Width * 0.5f);

            if(center.Y < 0.0)
                result.Y = -(rect.Height * 0.5f);

            if (center.X > overlay.Bounds.Width) 
                result.X = Bounds.Width - (rect.Width * 0.5f);

            if (center.Y > overlay.Bounds.Height) 
                result.Y = Bounds.Height - (rect.Height * 0.5f);
            
            return BackingAlignedRect(result, NSAlignmentOptions.AllEdgesNearest);
        }

        #region NSView

        public override NSView HitTest(CGPoint aPoint)
        {
            NSView ahitView = base.HitTest(aPoint);
            // catching all mouse events except when editing text
            if (ahitView != Window?.FieldEditor(false, null))
                ahitView = this;

            return ahitView;
        }

        public override void MouseDown(NSEvent theEvent)
        {
            CGPoint location = ConvertPointFromView(theEvent.LocationInWindow, null);
            CGPoint hitPoint = ConvertPointFromView(location, Superview);
            NSView hitView = base.HitTest(hitPoint);
            foreach (TextField atextField in textFields)
            {
                if(hitView == atextField)
                {
                    atextField.IsSelected = true;
                    break;
                }
            }

            NSEventMask eventMask = NSEventMask.LeftMouseUp | NSEventMask.LeftMouseDragged;
            double timeout = 1.7976931348623157E+308;
            if(hitView is TextField textField)
            {
                // drag the text field
                CGRect textFrame = textField.Frame;
                dragOriginOffset = new CGPoint(location.X - textFrame.GetMinX(), location.Y - textFrame.GetMinY());

                if(theEvent.ClickCount == 2)
                {
                    textField.IsSelected = false;
                    Window?.MakeFirstResponder(textField);
                }
                else
                {
                    Window?.TrackEventsMatching(eventMask, timeout, "NSEventTrackingRunLoopMode", (NSEvent evt, ref bool stop) =>
                    {
                        if(evt == null || evt.Type == NSEventType.LeftMouseUp)
                        {
                            stop = true;
                            return;
                        }
                        CGPoint movedLocation = ConvertPointFromView(evt.LocationInWindow, null);
                        CGPoint MovedOrigin = new CGPoint(movedLocation.X - dragOriginOffset.X, movedLocation.Y - dragOriginOffset.Y);
                        textField.Frame = ConstrainRectCenterToBounds(new CGRect(MovedOrigin, textFrame.Size));
                    });
                }
            }
            else if(Image != null)
            {
                // drag the flattened image
                Window?.TrackEventsMatching(eventMask, timeout, "NSEventTrackingRunLoopMode", (NSEvent evt, ref bool stop) =>
                {
                    if(evt == null || evt.Type == NSEventType.LeftMouseUp)
                    {
                        stop = true;
                        return;
                    }
                    CGPoint movedLocation = ConvertPointFromView(evt.LocationInWindow, null);
                    if(Math.Abs(movedLocation.X - location.Y) > dragThreshold || Math.Abs(movedLocation.Y - location.Y) > dragThreshold)
                    {
                        stop = true;
                        if(CanvasDelegate is ImageCanvasController cdelegate)
                        {
                            NSFilePromiseProvider provider = new NSFilePromiseProvider(MobileCoreServices.UTType.JPEG, cdelegate)
                            {
                                UserInfo = snapshotItem
                            };
                            NSDraggingItem[] draggingItems = { new NSDraggingItem(provider) };
                            draggingItems[0].SetDraggingFrame(overlay.Frame, DraggingImage);
                            BeginDraggingSession(draggingItems, evt, this);
                        }
                    }
                });
            }
        }

        public override void Layout()
        {
            base.Layout();
            CGSize imageSize = Image?.Size ?? CGSize.Empty;
            overlay.Frame = RectForDrawingImage(imageSize, imageView.ImageScaling);
        }

        [Export("drawRect:")]
        public override void DrawRect(CGRect dirtyRect)
        {
            base.DrawRect(dirtyRect);

            if(IsHighlighted)
            {
                NSGraphicsContext.GlobalSaveGraphicsState();
                NSGraphics.SetFocusRingStyle(NSFocusRingPlacement.RingOnly);
                Bounds.Inset(2, 2); //.Fill();
                
                NSGraphicsContext.GlobalRestoreGraphicsState();
            }
        }

        public override bool AcceptsFirstResponder()
        {
            return true;
        }

        #endregion

        #region NSTextViewDelegate

        [Export("controlTextDidEndEditing:")]
        public void EditingEnded(NSNotification notification)
        {
            Window?.MakeFirstResponder(this);
        }

        #endregion

        #region NSDraggingSource

        // BUGBUG: This method doesn't exist in Xamarin:
        public NSDragOperation DraggingSession(NSDraggingSession session, NSDraggingContext context)
        {
            return (context == NSDraggingContext.OutsideApplication) ? NSDragOperation.Copy : NSDragOperation.None;
        }
        #endregion

        /// updates the canvas with a given image
        private void HandleImage(NSImage image)
        {
            Image = image;
            if(CanvasDelegate is ImageCanvasController localdelegate)
            {
                localdelegate.UpdateDescription(ImageDescription, image != null);
            }
        }

        /// updates the canvas with a given image file
        private void HandleFile(NSUrl url)
        {
            NSImage image = new NSImage(url);
            NSOperationQueue.MainQueue.AddOperation(() => {
                this.HandleImage(image);
            });
        }

        #region NSDraggingDestination

        [Export("draggingEntered:")]
        public override NSDragOperation DraggingEntered(NSDraggingInfo sender)
        {
            if(CanvasDelegate is ImageCanvasController localdelegate)
            {
                IsHighlighted = true;

                // TODO: Check that this works. Has to change behavior for C#
                if(sender.DraggingSourceOperationMask.HasFlag(NSDragOperation.Copy))
                    return NSDragOperation.Copy;
                //return sender.DraggingSourceOperationMask.intersection([.copy])
                return new NSDragOperation();
            }
            return new NSDragOperation();
        }

        /// directory URL used for accepting file promises
        private NSUrl destinationURL
        {
            get
            {
                NSFileManager fm = new NSFileManager();
                NSUrl destURL = fm.GetTemporaryDirectory();
                destURL = destURL.Append("Drops", true);
                NSFileManager.DefaultManager.CreateDirectory(destURL.Path, true, null);
                return destURL;
            }
        }

        [Export("performDragOperation:")]
        public override bool PerformDragOperation(NSDraggingInfo sender)
        {
            //return CanvasDelegate?.performDragOperation(this, sender) ?? true;
            //Type[] supportedClasses = { typeof(NSFilePromiseReceiver), typeof(NSUrl) };
            //NSDictionary searchOptions = new NSDictionary("urlReadingContentsConformToTypes", true,
            //                                        "NSPasteboardURLReadingContentsConformToTypesKey", MobileCoreServices.UTType.Image);

            NSPasteboard pasteBoard = sender.GetDraggingPasteboard();
            foreach(NSPasteboardItem pbitem in pasteBoard.PasteboardItems)
            {
                if(pbitem.Types.Contains(NSPasteboard.NSPasteboardTypeFileUrl))
                {
                    HandleFile(NSUrl.FromString(pbitem.GetStringForType(NSPasteboard.NSPasteboardTypeFileUrl)));
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

        [Export("draggingExited:")]
        public override void DraggingExited(NSDraggingInfo sender)
        {
            IsHighlighted = false;
        }

        [Export("draggingEnded:")]
        public override void DraggingEnded(NSDraggingInfo sender)
        {
            IsHighlighted = false;
        }

        #endregion
    }
}
