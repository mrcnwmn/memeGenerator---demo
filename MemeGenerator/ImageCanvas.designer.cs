// WARNING
//
// This file has been generated automatically by Visual Studio to store outlets and
// actions made in the UI designer. If it is removed, they will be lost.
// Manual changes to this file may not be handled correctly.
//
using Foundation;
using System.CodeDom.Compiler;

namespace MemeGenerator
{ 
	partial class ImageCanvas
	{
		[Outlet]
		MemeGenerator.ImageCanvasController CanvasDelegate { get; set; }

		[Outlet]
		AppKit.NSImageView imageView { get; set; }

		[Outlet]
		AppKit.NSProgressIndicator progressIndicator { get; set; }

        [Action("Delete:")]
        partial void Delete(AppKit.NSMenuItem sender);

        void ReleaseDesignerOutlets ()
		{
			if (imageView != null) {
				imageView.Dispose ();
				imageView = null;
			}

			if (progressIndicator != null) {
				progressIndicator.Dispose ();
				progressIndicator = null;
			}

			if (CanvasDelegate != null) {
				CanvasDelegate.Dispose ();
				CanvasDelegate = null;
			}
		}
	}
}
