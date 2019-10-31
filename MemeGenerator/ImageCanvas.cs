using System;
using System.Collections.Generic;
using System.Linq;
using AppKit;
using CoreGraphics;
using Foundation;

namespace MemeGenerator
{
    [Register("ImageCanvas")]
    public partial class ImageCanvas : NSView, INSTextFieldDelegate, INSDraggingSource
    {
        private static readonly float dragThreshold = 3.0f;
        private static readonly double timeout      = 1.7976931348623157E+308;
        private CGPoint dragOriginOffset            = CGPoint.Empty;
        private CGSize imagePixelSize               = CGSize.Empty;
        private TextField selectedTextField;
        private NSView textfieldoverlay;
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

        private bool IsHighlighted
        {
            get => highlighted;
            set
            {
                highlighted = value;
                NeedsDisplay = true;
            }
        }

        private void Loading(bool value)
        {
            loading = value;
            imageView.Enabled = !loading;
            progressIndicator.Hidden = !loading;
            if (loading)
                progressIndicator.StartAnimation(null);
            else
                progressIndicator.StopAnimation(null);
        }

        public NSImage Image
        {
            set
            {
                imageView.Image = value;
                NSImageRep[] imageReps = value.Representations();
                if(imageReps.Length > 0)
                    imagePixelSize = new CGSize(imageReps[0].PixelsWide, imageReps[0].PixelsHigh);
                Loading(false);
                NeedsLayout = true;
            }
            get
            {
                if(imageView == null)
                    return null;
                return imageView.Image;
            }
        }

        private NSImage DraggingImage
        {
            get
            {
                CGRect targetRect = textfieldoverlay.Frame;
                NSImage image = new NSImage(targetRect.Size);
                if(BitmapImageRepForCachingDisplayInRect(targetRect) is NSBitmapImageRep imageRep)
                {
                    CacheDisplay(targetRect, imageRep);
                    image.AddRepresentation(imageRep);
                }
                return image;
            }
        }

        [Export("awakeFromNib")]
        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            imageView.UnregisterDraggedTypes();
            progressIndicator.Hidden = true; // explicitly hiding the indicator in order not to catch mouse events
            textfieldoverlay = new NSView();
            AddSubview(textfieldoverlay);
        }

        public void AddTextField()
        {
            // Remove any text fields that have no content
            for(int x = textfieldoverlay.Subviews.Length - 1; x > -1; x--)
            {
                if(string.IsNullOrEmpty(((TextField)textfieldoverlay.Subviews[x]).StringValue))
                    textfieldoverlay.Subviews[x].RemoveFromSuperview();
            }
            TextField textField = new TextField()
            {
                Delegate = this
            };
            textfieldoverlay.AddSubview(textField);
            textField.CenterInSuperview();
            textField.Window?.MakeFirstResponder(this);
        }

        partial void Delete(NSMenuItem sender)
        {
            if(selectedTextField != null)
            {
                selectedTextField.RemoveFromSuperview();
                selectedTextField = null;
            }
        }

        private CGRect RectForDrawingImage(CGSize imageSize, NSImageScale scaling)
        {
            CGRect drawingRect = new CGRect(CGPoint.Empty, imageSize);
            CGRect containerRect = Bounds;

            if(imageSize.Width <= 0 || imageSize.Height <= 0)
            {
                return drawingRect;
            }

            CGSize scaledSizeToFitFrame()
            {
                CGSize scaledSize       = CGSize.Empty;
                nfloat horizontalScale  = containerRect.Width / imageSize.Width;
                nfloat verticalScale    = containerRect.Height / imageSize.Height;
                float minimumScale      = Math.Min((float)horizontalScale, (float)verticalScale);
                scaledSize.Width        = imageSize.Width * minimumScale;
                scaledSize.Height       = imageSize.Height * minimumScale;
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

            if (center.X > textfieldoverlay.Bounds.Width) 
                result.X = Bounds.Width - (rect.Width * 0.5f);

            if (center.Y > textfieldoverlay.Bounds.Height) 
                result.Y = Bounds.Height - (rect.Height * 0.5f);
            
            return BackingAlignedRect(result, NSAlignmentOptions.AllEdgesNearest);
        }

        #region NSView

        public override NSView HitTest(CGPoint aPoint)
        {
            NSView ahitView = base.HitTest(aPoint);

            // Catch all mouse events except when editing text
            if (ahitView != Window?.FieldEditor(false, null))
                ahitView = this;

            return ahitView;
        }

        private TextField GetTextField(NSView view)
        {
            foreach(TextField fld in textfieldoverlay.Subviews)
            {
                if(fld.Equals(view))
                    return fld;
            }
            return null;
        }

        public override void MouseDown(NSEvent theEvent)
        {
            CGPoint location        = ConvertPointFromView(theEvent.LocationInWindow, null);
            NSView hitView          = base.HitTest(ConvertPointFromView(location, Superview));
            NSEventMask eventMask   = NSEventMask.LeftMouseUp | NSEventMask.LeftMouseDragged;
            selectedTextField       = GetTextField(hitView);

            if(selectedTextField != null)
            {
                selectedTextField.IsSelected = true;
                // drag the text field
                CGRect textFrame = selectedTextField.Frame;
                dragOriginOffset = new CGPoint(location.X - textFrame.GetMinX(), location.Y - textFrame.GetMinY());

                if(theEvent.ClickCount == 2)
                {
                    selectedTextField.IsSelected = false;
                    Window?.MakeFirstResponder(selectedTextField);
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
                        CGPoint movedLocation   = ConvertPointFromView(evt.LocationInWindow, null);
                        CGPoint MovedOrigin     = new CGPoint(movedLocation.X - dragOriginOffset.X, movedLocation.Y - dragOriginOffset.Y);
                        selectedTextField.Frame = ConstrainRectCenterToBounds(new CGRect(MovedOrigin, textFrame.Size));
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
                        NSFilePromiseProvider provider = new NSFilePromiseProvider(MobileCoreServices.UTType.JPEG, CanvasDelegate)
                        {
                            UserInfo = new SnapshotItem(Image, textfieldoverlay.Subviews, imagePixelSize, imagePixelSize.Width / textfieldoverlay.Frame.Width)
                        };
                        NSDraggingItem[] draggingItems = { new NSDraggingItem(provider) };
                        draggingItems[0].SetDraggingFrame(textfieldoverlay.Frame, DraggingImage);
                        NSDraggingSession ds = BeginDraggingSession(draggingItems, evt, this);
                    }
                });
            }
        }

        public override void Layout()
        {
            base.Layout();
            CGSize imageSize = Image?.Size ?? CGSize.Empty;
            textfieldoverlay.Frame = RectForDrawingImage(imageSize, imageView.ImageScaling);
        }

        [Export("drawRect:")]
        public override void DrawRect(CGRect dirtyRect)
        {
            base.DrawRect(dirtyRect);

            if(IsHighlighted)
            {
                NSGraphicsContext current = NSGraphicsContext.CurrentContext;
                current.SaveGraphicsState();
                NSGraphics.SetFocusRingStyle(NSFocusRingPlacement.RingOnly);
                current.CGContext.FillRect(Bounds.Inset(2, 2));
                current.RestoreGraphicsState();
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

        /// updates the canvas with a given image file
        private void HandleFile(NSUrl url)
        {
            NSOperationQueue.MainQueue.AddOperation(() => {
                Image = new NSImage(url);

                string desc = (Image != null) ? ((int)imagePixelSize.Width).ToString() + " × " + ((int)imagePixelSize.Height).ToString() : "...";
                CanvasDelegate.UpdateDescription(desc, Image != null);
            });
        }

        /// directory URL used for accepting file promises
        private NSUrl DestinationURL
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

        #region NSDraggingDestination

        [Export("draggingEntered:")]
        public override NSDragOperation DraggingEntered(NSDraggingInfo sender)
        {
            IsHighlighted = true;

            if(sender.DraggingSourceOperationMask.HasFlag(NSDragOperation.Copy))
                return NSDragOperation.Copy;

            return NSDragOperation.None;
        }

        [Export("draggingUpdated:")]
        public override NSDragOperation DraggingUpdated(NSDraggingInfo sender)
        {
            if(sender.DraggingSource == this)
                return NSDragOperation.None;
            return NSDragOperation.Copy;
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
            //                    this.DestinationURL,
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
