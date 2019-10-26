using System;
using System.Collections.Generic;
using AppKit;
using CoreGraphics;
using Foundation;

namespace MemeGenerator
{
    /// Delegate for handling dragging events
    [Protocol(Name = "ImageCanvasDelegate")] //, WrapperType = typeof(NSTextFieldDelegateWrapper))]
    public interface ImageCanvasDelegate
    { 
        NSDragOperation draggingEntered(ImageCanvas imageCanvas, NSDraggingInfo sender);
        bool performDragOperation(ImageCanvas imageCanvas, NSDraggingInfo sender);
        NSFilePromiseProvider pasteboardWriter(ImageCanvas imageCanvas);
    }

    [Register("ImageCanvas")]
    public partial class ImageCanvas : NSView, INSTextFieldDelegate, INSDraggingSource
    {
        private float dragThreshold = 3.0f;
        private CGPoint dragOriginOffset = CGPoint.Empty;
        private CGSize imagePixelSize = CGSize.Empty;
        private NSView overlay;
        private List <TextField> textFields = new List<TextField>();
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

            private bool drawingHandler(CGRect dstRect)
            {
                baseImage.Draw(dstRect);
                NSAffineTransform transform = new NSAffineTransform();
                transform.Scale(this.drawingScale);
                transform.Concat();
                drawingItems.draw();
                return true;
            }

            public NSData jpegRepresentation
            {
                get
                {
                    NSImage outputImage = NSImage.ImageWithSize(pixelSize, false, drawingHandler);
                    NSData tiffData = outputImage.AsTiff();
                    NSBitmapImageRep bitmapImageRep = new NSBitmapImageRep(tiffData);
                    return bitmapImageRep.RepresentationUsingTypeProperties(NSBitmapImageFileType.Jpeg, new NSDictionary());
                }
            }
        }

        public bool isHighlighted
        {
            get
            {
                return highlighted;
            }
            set
            {
                highlighted = value;
                NeedsDisplay = true;
            }
        }

        public bool isLoading
        {
            get
            {
                return loading;
            }
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

        public NSImage image
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

        public string imageDescription
        {
            get
            {
                return (image != null) ? ((int)imagePixelSize.Width).ToString() + " × " + ((int)imagePixelSize.Height).ToString() : "...";
            }
        }

        public NSImage draggingImage
        {
            get
            {
                CGRect targetRect = overlay.Frame;
                image = new NSImage(targetRect.Size);
                NSBitmapImageRep imageRep = BitmapImageRepForCachingDisplayInRect(targetRect);
                if(imageRep != null)
                {
                    CacheDisplay(targetRect, imageRep);
                    image.AddRepresentation(imageRep);
                }
                return image;
            }
        }

        public SnapshotItem snapshotItem
        {
            get
            {
                NSImage newimage = this.image;
                if(newimage == null) return null;
                TextField.DrawingItem drawingItems = (textFields.Count>0)? textFields[0].drawingItem() : new TextField.DrawingItem();
                nfloat drawingScale = imagePixelSize.Width / overlay.Frame.Width;

                return new SnapshotItem(image, imagePixelSize, drawingItems, drawingScale);
            }
        }

        //    @IBOutlet weak var delegate: ImageCanvasDelegate?

        [Export("awakeFromNib")]
        public override void AwakeFromNib()
        {
            base.AwakeFromNib();
            imageView.UnregisterDraggedTypes();
            progressIndicator.Hidden = true; // explicitly hiding the indicator in order not to catch mouse events
            overlay = new NSView();
            AddSubview(overlay);
        }

        public void addTextField()
        {
            TextField textField = new TextField();
            textFields.Add(textField);
            overlay.AddSubview(textField);
            textField.Delegate = this;
            textField.centerInSuperview();
            textField.makeFirstResponder();
        }

        //@IBAction func delete(_ sender: Any?)
        //{
        //    if let textField = selectedTextField, let index = textFields.index(of: textField) {
        //        textFields.remove(at: index)
        //            selectedTextField?.removeFromSuperview()
        //            selectedTextField = nil
        //        }
        //}

        private CGRect rectForDrawingImage(CGSize imageSize, NSImageScale scaling)
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

        private CGRect constrainRectCenterToBounds(CGRect rect) {
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

        // MARK: - NSView

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
                    atextField.isSelected = true;
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
                    textField.isSelected = false;
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
                        textField.Frame = constrainRectCenterToBounds(new CGRect(MovedOrigin, textFrame.Size));
                    });
                }
            }
            else if(image != null)
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
                        ImageCanvasController cdelegate = CanvasDelegate;
                        if(cdelegate != null)
                        {
                            NSDraggingItem[] draggingItems = { new NSDraggingItem(cdelegate.pasteboardWriter(this)) };
                            draggingItems[0].SetDraggingFrame(overlay.Frame, draggingImage);
                            BeginDraggingSession(draggingItems, evt, this);
                        }
                    }
                });
            }
        }

        public override void Layout()
        {
            base.Layout();
            CGSize imageSize = image?.Size ?? CGSize.Empty;
            overlay.Frame = rectForDrawingImage(imageSize, imageView.ImageScaling);
        }

        [Export("drawRect:")]
        public override void DrawRect(CGRect dirtyRect)
        {
            base.DrawRect(dirtyRect);

            if(isHighlighted)
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

        // MARK: - NSTextViewDelegate

        [Export("controlTextDidEndEditing:")]
        public void EditingEnded(NSNotification notification)
        {
            Window?.MakeFirstResponder(this);
        }

        // MARK: - NSDraggingSource

            // BUGBUG: This method doesn't exist in Xamarin:
        public NSDragOperation draggingSession(NSDraggingSession session, NSDraggingContext context)
        {
            return (context == NSDraggingContext.OutsideApplication) ? NSDragOperation.Copy : NSDragOperation.None;
        }

        // MARK: - NSDraggingDestination

        [Export("draggingEntered:")]
        public override NSDragOperation DraggingEntered(NSDraggingInfo sender)
        {
            ImageCanvasController localdelegate = CanvasDelegate;
            if(localdelegate != null)
            {
                isHighlighted = true;
                return localdelegate.draggingEntered(this, sender);
            }
            return new NSDragOperation();
        }

        [Export("performDragOperation:")]
        public override bool PerformDragOperation(NSDraggingInfo sender)
        {
            return CanvasDelegate?.performDragOperation(this, sender) ?? true;
        }

        [Export("draggingExited:")]
        public override void DraggingExited(NSDraggingInfo sender)
        {
            isHighlighted = false;
        }

        [Export("draggingEnded:")]
        public override void DraggingEnded(NSDraggingInfo sender)
        {
            isHighlighted = false;
        }
    }
}
